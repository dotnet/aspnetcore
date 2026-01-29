// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

class DotnetWebWorkerClient {
    #worker;
    #pendingRequests = {};
    #requestId = 0;

    constructor(worker) {
        this.#worker = worker;
    }

    static create() {
        return new Promise((resolve, reject) => {
            const worker = new Worker('_content/Company.WebWorker1/dotnet-web-worker.js', { type: "module" });

            worker.addEventListener('error', (e) => {
                reject(new Error(e.message || 'Worker encountered an error'));
            });

            worker.addEventListener('message', function onMessage(e) {
                if (e.data.type === "ready") {
                    worker.removeEventListener('message', onMessage);
                    if (e.data.error) {
                        reject(new Error(e.data.error));
                    } else {
                        const client = new DotnetWebWorkerClient(worker);
                        client.#setupMessageHandler();
                        resolve(client);
                    }
                }
            });
        });
    }

    invoke(method, args) {
        return new Promise((resolve, reject) => {
            const id = ++this.#requestId;
            this.#pendingRequests[id] = { resolve: r => resolve(this.#parseIfJson(r)), reject };
            this.#worker.postMessage({ method, args, requestId: id });
        });
    }

    #parseIfJson(value) {
        if (typeof value === 'string' && (value[0] === '{' || value[0] === '[')) {
            try { return JSON.parse(value); } catch { /* not JSON */ }
        }
        return value;
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

export function create() {
    return DotnetWebWorkerClient.create();
}
