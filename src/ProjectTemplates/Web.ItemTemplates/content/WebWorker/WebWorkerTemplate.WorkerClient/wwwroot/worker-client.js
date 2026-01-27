// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

class WorkerClient {
    #pendingRequests = {};
    #requestId = 0;
    #workerError = null;
    #worker;

    constructor(worker) {
        this.#worker = worker;
    }

    static async create() {
        const worker = new Worker('_content/WebWorkerTemplate.WorkerClient/worker.js', { type: "module" });
        const { promise, resolve, reject } = Promise.withResolvers();

        worker.addEventListener('error', (e) => {
            const errorMessage = e.message || 'Worker encountered an unhandled error';
            console.error("Worker error:", errorMessage);
            reject(new Error(errorMessage));
        });

        worker.addEventListener('message', (e) => {
            if (e.data.type === "ready") {
                e.data.error ? reject(new Error(e.data.error)) : resolve();
            }
        });

        await promise;

        const client = new WorkerClient(worker);

        worker.addEventListener('error', (e) => {
            const errorMessage = e.message || 'Worker encountered an unhandled error';
            client.#workerError = errorMessage;
            client.#rejectAllPending(`Worker error: ${errorMessage}`);
        });

        worker.addEventListener('message', (e) => {
            if (e.data.type === "result") {
                const request = client.#pendingRequests[e.data.requestId];
                if (!request) return;
                delete client.#pendingRequests[e.data.requestId];
                e.data.error ? request.reject(new Error(e.data.error)) : request.resolve(e.data.result);
            }
        });

        return client;
    }

    greet = (name) => this.#invoke("WebWorkerTemplate.Worker.GreetWorker.Greet", [name]);

    async #invoke(method, args) {
        if (this.#workerError) {
            throw new Error(`Worker failed to initialize: ${this.#workerError}`);
        }

        const currentRequestId = ++this.#requestId;
        const { promise, resolve, reject } = Promise.withResolvers();
        this.#pendingRequests[currentRequestId] = { resolve, reject };
        this.#worker.postMessage({ method, args, requestId: currentRequestId });
        return promise;
    }

    terminate() {
        this.#rejectAllPending("Worker was terminated");
        this.#worker?.terminate();
    }

    #rejectAllPending(errorMessage) {
        for (const id in this.#pendingRequests) {
            this.#pendingRequests[id].reject(new Error(errorMessage));
            delete this.#pendingRequests[id];
        }
    }
}

export const createWorker = () => WorkerClient.create();
