function withTimeout(promise, timeoutMs, timeoutMessage) {
    const timeout = new Promise((_, reject) =>
        setTimeout(() => reject(new Error(timeoutMessage)), timeoutMs));
    return Promise.race([promise, timeout]);
}

class DotnetWebWorkerClient {
    #worker;
    #pendingRequests = {};
    #requestId = 0;

    constructor(worker) {
        this.#worker = worker;
    }

    static create(initTimeoutMs, options = {}) {
        const worker = new Worker('_content/Company.WebWorker1/dotnet-web-worker.js', { type: "module" });

        const initWorker = new Promise((resolve, reject) => {
            worker.addEventListener('error', (e) =>
                reject(new Error(e.message || 'Worker encountered an error')));
            worker.addEventListener('message', function onMessage(e) {
                if (e.data.type === "ready") {
                    worker.removeEventListener('message', onMessage);
                    e.data.error ? reject(new Error(e.data.error)) : resolve();
                }
            });
        });

        const dotnetJsUrl = DotnetWebWorkerClient.#resolveDotnetJsUrl();
        const assemblyName = options?.assemblyName ?? null;
        worker.postMessage({ type: 'init', dotnetJsUrl, assemblyName });

        return withTimeout(initWorker, initTimeoutMs, 'Worker initialization timed out').then(() => {
            const client = new DotnetWebWorkerClient(worker);
            client.#setupMessageHandler();
            return client;
        }, err => {
            worker.terminate();
            throw err;
        });
    }

    static #resolveDotnetJsUrl() {
        // Resolve using the browser's import map (handles fingerprinted URLs in published apps).
        // Workers don't inherit the page's import map, so we resolve on the main thread and pass the URL.
        const dotnetJsUrl = new URL('_framework/dotnet.js', document.baseURI).href;
        return import.meta.resolve?.(dotnetJsUrl) ?? dotnetJsUrl;
    }

    invoke(method, args, timeoutMs) {
        const invoke = new Promise((resolve, reject) => {
            const id = ++this.#requestId;
            this.#pendingRequests[id] = { resolve, reject };
            this.#worker.postMessage({ method, args, requestId: id });
        });

        return withTimeout(invoke, timeoutMs, `Worker method '${method}' timed out`).catch(err => {
            const id = this.#requestId;
            if (this.#pendingRequests[id]) {
                delete this.#pendingRequests[id];
            }
            throw err;
        });
    }

    terminate() {
        this.#rejectAllPending("Worker terminated");
        this.#worker?.terminate();
        this.#worker = null;
    }

    #setupMessageHandler() {
        this.#worker.addEventListener('message', (e) => {
            if (e.data.type === "result") {
                const request = this.#pendingRequests[e.data.requestId];
                if (request) {
                    delete this.#pendingRequests[e.data.requestId];
                    if (e.data.error) {
                        request.reject(new Error(e.data.error));
                    } else {
                        request.resolve(e.data.result);
                    }
                }
            }
        });

        this.#worker.addEventListener('error', (e) => {
            this.#rejectAllPending(e.message || 'Worker error');
        });
    }

    #rejectAllPending(errorMessage) {
        for (const id in this.#pendingRequests) {
            this.#pendingRequests[id].reject(new Error(errorMessage));
            delete this.#pendingRequests[id];
        }
    }
}

export function create(initTimeoutMs, options) {
    return DotnetWebWorkerClient.create(initTimeoutMs, options);
}
