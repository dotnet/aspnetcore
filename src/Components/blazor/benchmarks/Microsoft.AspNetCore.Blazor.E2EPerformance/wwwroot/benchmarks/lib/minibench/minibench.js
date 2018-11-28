/** minibench - https://github.com/SteveSanderson/minibench */
class EventEmitter {
    constructor() {
        this.eventListeners = {};
    }

    on(eventName, callback, options) {
        const listeners = this.eventListeners[eventName] = this.eventListeners[eventName] || [];
        const handler = argsArray => {
            if (options && options.once) {
                const thisIndex = listeners.indexOf(handler);
                listeners.splice(thisIndex, 1);
            }

            callback.apply(null, argsArray);
        };

        listeners.push(handler);
    }

    once(eventName, callback) {
        this.on(eventName, callback, { once: true });
    }

    _emit(eventName, ...args) {
        const listeners = this.eventListeners[eventName];
        listeners && listeners.forEach(l => l.call(null, args));
    }
}

let currentPromise = new Promise(resolve => resolve());

function addToWorkQueue(fn) {
    const cancelHandle = new CancelHandle();
    currentPromise = currentPromise.then(() => cancelHandle.isCancelled || fn());
    return cancelHandle;
}

class CancelHandle {
    cancel() {
        this.isCancelled = true;
    }
}

const queue = [];
const messageIdentifier = 'nextTick-' + Math.random();

function nextTick(callback) {
    queue.push(callback);
    window.postMessage(messageIdentifier, '*');
}

function nextTickPromise() {
    return new Promise(resolve => nextTick(resolve));
}

window.addEventListener('message', evt => {
    if (evt.data === messageIdentifier) {
        evt.stopPropagation();
        const callback = queue.shift();
        callback && callback();
    }
});

/*
    To work around browsers' current nonsupport for high-resolution timers
    (since Spectre etc.), the approach used here is to group executions into
    blocks of roughly fixed duration.
    
    - In each block, we execute the test code as many times as we can until
      the end of the block duration, without even yielding the thread if
      it's a synchronous call. We count how many executions completed. It
      will always be at least 1, even if the single call duration is longer
      than the intended block duration.
    - Since each block is of a significant duration (e.g., 0.5 sec), the low
      resolution of the timer doesn't matter. We can divide the measured block
      duration by the measured number of executions to estimate the per-call
      duration.
    - Each block will give us a different estimate. We want to return the *best*
      timing, not the mean or median. That's the most accurate predictor of the
      true execution cost, as hopefully there will have been at least one block
      during which there was no unrelated GC cycle or other background contention.
    - We keep running blocks until some larger timeout occurs *and* we've done
      at least some minimum number of executions.
    
    Note that this approach does *not* allow for per-execution setup/teardown
    logic whose timing is separated from the code under test. Because of the
    low timer precision, there would be no way to separate the setup duration
    from the test code duration if they were interleaved too quickly (e.g.,
    if the test code was < 1ms). We do support per-benchmark setup/teardown,
    but not per-execution.
*/

const totalDurationMs = 6000;
const blockDurationMs = 400;
const minExecutions = 10;

class ExecutionTimer {
    constructor(fn) {
        this._fn = fn;
    }

    async run(progressCallback, runOptions) {
        this._isAborted = false;
        this.numExecutions = 0;
        this.bestExecutionsPerMs = null;

        // 'verify only' means just do a single execution to check it doesn't error
        const targetBlockDuration = runOptions.verifyOnly ? 1 : blockDurationMs;
        const targetMinExecutions = runOptions.verifyOnly ? 1 : minExecutions;
        const targetTotalDuration = runOptions.verifyOnly ? 0 : totalDurationMs;

        const endTime = performance.now() + targetTotalDuration;
        while (performance.now() < endTime || this.numExecutions < targetMinExecutions) {
            if (this._isAborted) {
                this.numExecutions = 0;
                this.bestExecutionsPerMs = null;
                break;
            }

            const { blockDuration, blockExecutions } = await this._runBlock(targetBlockDuration);
            this.numExecutions += blockExecutions;

            const blockExecutionsPerMs = blockExecutions / blockDuration;
            if (blockExecutionsPerMs > this.bestExecutionsPerMs) {
                this.bestExecutionsPerMs = blockExecutionsPerMs;
            }

            progressCallback && progressCallback();
        }
    }

    abort() {
        this._isAborted = true;
    }

    async _runBlock(targetBlockDuration) {
        await nextTickPromise();

        const blockStartTime = performance.now();
        const blockEndTime = blockStartTime + targetBlockDuration;
        let executions = 0;

        while ((performance.now() < blockEndTime) && !this._isAborted) {
            const syncResult = this._fn();

            // Only yield the thread if we really have to
            if (syncResult instanceof Promise) {
                await syncResult;
            }

            executions++;
        }

        return {
            blockDuration: performance.now() - blockStartTime,
            blockExecutions: executions
        };
    }
}

class Benchmark extends EventEmitter {
    constructor(group, name, fn, options) {
        super();
        this._group = group;
        this.name = name;
        this._fn = fn;
        this._options = options;
        this._state = { status: BenchmarkStatus.idle };
    }

    get state() {
        return this._state;
    }

    run(runOptions) {
        this._currentRunWasAborted = false;
        if (this._state.status === BenchmarkStatus.idle) {
            this._updateState({ status: BenchmarkStatus.queued });
            this.workQueueCancelHandle = addToWorkQueue(async () => {
                try {
                    if (!(runOptions && runOptions.skipGroupSetup)) {
                        await this._group.runSetup();
                    }

                    this._updateState({ status: BenchmarkStatus.running });
                    this._options && this._options.setup && await this._options.setup();
                    await this._measureTimings(runOptions);

                    this._options && this._options.teardown && await this._options.teardown();
                    if (this._currentRunWasAborted || !(runOptions && runOptions.skipGroupTeardown)) {
                        await this._group.runTeardown();
                    }

                    this._updateState({ status: BenchmarkStatus.idle });
                } catch (ex) {
                    this._updateState({ status: BenchmarkStatus.error });
                    console.error(ex);
                }
            });
        }
    }

    stop() {
        this._currentRunWasAborted = true;
        this.timer && this.timer.abort();
        this.workQueueCancelHandle && this.workQueueCancelHandle.cancel();
        this._updateState({ status: BenchmarkStatus.idle });
    }

    async _measureTimings(runOptions) {
        this._updateState({ numExecutions: 0, estimatedExecutionDurationMs: null });

        this.timer = new ExecutionTimer(this._fn);
        const updateTimingsDisplay = () => {
            this._updateState({
                numExecutions: this.timer.numExecutions,
                estimatedExecutionDurationMs: this.timer.bestExecutionsPerMs ? 1 / this.timer.bestExecutionsPerMs : null
            });
        };

        await this.timer.run(updateTimingsDisplay, { verifyOnly: runOptions.verifyOnly });
        updateTimingsDisplay();
        this.timer = null;
    }

    _updateState(newState) {
        Object.assign(this._state, newState);
        this._emit('changed', this._state);
    }
}

const BenchmarkStatus = {
    idle: 0,
    queued: 1,
    running: 2,
    error: 3,
};

class Group extends EventEmitter {
    constructor(name) {
        super();
        this.name = name;
        this.benchmarks = [];
    }

    add(benchmark) {
        this.benchmarks.push(benchmark);
        benchmark.on('changed', () => this._emit('changed'));
    }

    runAll(runOptions) {
        this.benchmarks.forEach((benchmark, index) => {
            benchmark.run(Object.assign({
                skipGroupSetup: index > 0,
                skipGroupTeardown: index < this.benchmarks.length - 1,
            }, runOptions));
        });
    }

    stopAll() {
        this.benchmarks.forEach(b => b.stop());
    }

    async runSetup() {
        this.setup && await this.setup();
    }

    async runTeardown() {
        this.teardown && await this.teardown();
    }

    get status() {
        return this.benchmarks.reduce(
            (prev, next) => Math.max(prev, next.state.status),
            BenchmarkStatus.idle
        );
    }
}

const groups = [];

function group(name, configure) {
    groups.push(new Group(name));
    configure && configure();
}

function benchmark(name, fn, options) {
    const group = groups[groups.length - 1];
    group.add(new Benchmark(group, name, fn, options));
}

function setup(fn) {
    groups[groups.length - 1].setup = fn;
}

function teardown(fn) {
    groups[groups.length - 1].teardown = fn;
}

class BenchmarkDisplay {
    constructor(htmlUi, benchmark) {
        this.benchmark = benchmark;
        this.elem = document.createElement('tr');
        
        const headerCol = this.elem.appendChild(document.createElement('th'));
        headerCol.className = 'pl-4';
        headerCol.textContent = benchmark.name;
        headerCol.setAttribute('scope', 'row');

        const progressCol = this.elem.appendChild(document.createElement('td'));
        this.numExecutionsText = progressCol.appendChild(document.createTextNode(''));

        const timingCol = this.elem.appendChild(document.createElement('td'));
        this.executionDurationText = timingCol.appendChild(document.createElement('span'));
        
        const runCol = this.elem.appendChild(document.createElement('td'));
        runCol.className = 'pr-4';
        runCol.setAttribute('align', 'right');
        this.runButton = document.createElement('a');
        this.runButton.className = 'run-button';
        runCol.appendChild(this.runButton);
        this.runButton.textContent = 'Run';
        this.runButton.onclick = evt => {
            evt.preventDefault();
            this.benchmark.run(htmlUi.globalRunOptions);
        };

        benchmark.on('changed', state => this.updateDisplay(state));
        this.updateDisplay(this.benchmark.state);
    }

    updateDisplay(state) {
        const benchmark = this.benchmark;
        this.elem.className = rowClass(state.status);
        this.runButton.textContent = runButtonText(state.status);
        this.numExecutionsText.textContent = state.numExecutions
            ? `Executions: ${state.numExecutions}` : '';
        this.executionDurationText.innerHTML = state.estimatedExecutionDurationMs
            ? `Duration: <b>${parseFloat(state.estimatedExecutionDurationMs.toPrecision(3))}ms</b>` : '';
        if (state.status === BenchmarkStatus.idle) {
            this.runButton.setAttribute('href', '');
        } else {
            this.runButton.removeAttribute('href');
            if (state.status === BenchmarkStatus.error) {
                this.numExecutionsText.textContent = 'Error - see console';
            }
        }
    }
}

function runButtonText(status) {
    switch (status) {
        case BenchmarkStatus.idle:
        case BenchmarkStatus.error:
            return 'Run';
        case BenchmarkStatus.queued:
            return 'Waiting...';
        case BenchmarkStatus.running:
            return 'Running...';
        default:
            throw new Error(`Unknown status: ${status}`);
    }
}

function rowClass(status) {
    switch (status) {
        case BenchmarkStatus.idle:
            return 'benchmark-idle';
        case BenchmarkStatus.queued:
            return 'benchmark-waiting';
        case BenchmarkStatus.running:
            return 'benchmark-running';
        case BenchmarkStatus.error:
            return 'benchmark-error';
        default:
            throw new Error(`Unknown status: ${status}`);
    }
}

class GroupDisplay {
    constructor(htmlUi, group) {
        this.group = group;

        this.elem = document.createElement('div');
        this.elem.className = 'my-3 py-2 bg-white rounded shadow-sm';
        
        const headerContainer = this.elem.appendChild(document.createElement('div'));
        headerContainer.className = 'd-flex align-items-baseline px-4';
        const header = headerContainer.appendChild(document.createElement('h5'));
        header.className = 'py-2';
        header.textContent = group.name;

        this.runButton = document.createElement('a');
        this.runButton.className = 'ml-auto run-button';
        this.runButton.setAttribute('href', '');
        headerContainer.appendChild(this.runButton);
        this.runButton.textContent = 'Run all';
        this.runButton.onclick = evt => {
            evt.preventDefault();
            group.runAll(htmlUi.globalRunOptions);
        };

        const table = this.elem.appendChild(document.createElement('table'));
        table.className = 'table mb-0 benchmarks';
        const tbody = table.appendChild(document.createElement('tbody'));

        group.benchmarks.forEach(benchmark => {
            const benchmarkDisplay = new BenchmarkDisplay(htmlUi, benchmark);
            tbody.appendChild(benchmarkDisplay.elem);
        });

        group.on('changed', () => this.updateDisplay());
        this.updateDisplay();
    }

    updateDisplay() {
        const canRun = this.group.status === BenchmarkStatus.idle;
        this.runButton.style.display = canRun ? 'block' : 'none';
    }
}

class HtmlUI {
    constructor(title, selector) {
        this.containerElement = document.querySelector(selector);

        const headerDiv = this.containerElement.appendChild(document.createElement('div'));
        headerDiv.className = 'd-flex align-items-center';

        const header = headerDiv.appendChild(document.createElement('h2'));
        header.className = 'mx-3 flex-grow-1';
        header.textContent = title;

        const verifyCheckboxLabel = document.createElement('label');
        verifyCheckboxLabel.className = 'ml-auto mr-5';
        headerDiv.appendChild(verifyCheckboxLabel);
        this.verifyCheckbox = verifyCheckboxLabel.appendChild(document.createElement('input'));
        this.verifyCheckbox.type = 'checkbox';
        this.verifyCheckbox.className = 'mr-2';
        verifyCheckboxLabel.appendChild(document.createTextNode('Verify only'));

        this.runButton = document.createElement('button');
        this.runButton.className = 'btn btn-success ml-auto px-4 run-button';
        headerDiv.appendChild(this.runButton);
        this.runButton.textContent = 'Run all';
        this.runButton.onclick = () => {
            groups.forEach(g => g.runAll(this.globalRunOptions));
        };

        this.stopButton = document.createElement('button');
        this.stopButton.className = 'btn btn-danger ml-auto px-4 stop-button';
        headerDiv.appendChild(this.stopButton);
        this.stopButton.textContent = 'Stop';
        this.stopButton.onclick = () => {
            groups.forEach(g => g.stopAll());
        };

        groups.forEach(group$$1 => {
            const groupDisplay = new GroupDisplay(this, group$$1);
            this.containerElement.appendChild(groupDisplay.elem);
            group$$1.on('changed', () => this.updateDisplay());
        });

        this.updateDisplay();
    }

    updateDisplay() {
        const areAllIdle = groups.reduce(
            (prev, next) => prev && next.status === BenchmarkStatus.idle,
            true
        );
        this.runButton.style.display = areAllIdle ? 'block' : 'none';
        this.stopButton.style.display = areAllIdle ? 'none' : 'block';
    }

    get globalRunOptions() {
        return { verifyOnly: this.verifyCheckbox.checked };
    }
}

/**
 * minibench
 * https://github.com/SteveSanderson/minibench
 */

export { group, benchmark, setup, teardown, HtmlUI };
