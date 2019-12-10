/** minibench - https://github.com/SteveSanderson/minibench */

import { groups, BenchmarkStatus } from './minibench.js';

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
    this.runButton.setAttribute('id', 'runAll');
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
    this.stopButton.style.display = areAllIdle ? 'none' : 'block';;
  }

  get globalRunOptions() {
    return { verifyOnly: this.verifyCheckbox.checked };
  }
}

/**
 * minibench
 * https://github.com/SteveSanderson/minibench
 */

export { HtmlUI };
