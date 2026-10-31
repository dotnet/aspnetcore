// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ENDPOINT_BASE_URL } from "./Common";

describe("WebWorkers", () => {
    it("can use SignalR client", (done) => {
        if (typeof window !== "undefined" && (window as any).Worker) {
            const workerSrc = `
                var connection = null;

                onmessage = function (e) {
                    if (connection === null) {
                        postMessage('initialized');

                        importScripts(e.data + '/lib/signalr-webworker/signalr.js');

                        connection = new signalR.HubConnectionBuilder()
                            .withUrl(e.data + '/testhub')
                            .build();

                        connection.on('message', function (message) {
                            postMessage('Received message: ' + message);
                        });

                        connection.start().then(function () {
                            postMessage('connected');
                        });
                    } else if (connection.state == signalR.HubConnectionState.Connected) {
                        connection.invoke('invokeWithString', e.data);
                    } else {
                        postMessage('Attempted to send message while disconnected.')
                    }
                }`;

            // Load worker from a blob since workers MUST come from the same origin despite CORS configuration.
            // https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API/Using_web_workers#Spawning_subworkers
            const blob = new Blob([workerSrc], { type: "application/javascript" });
            const worker = new Worker(URL.createObjectURL(blob));

            const testMessage = "Hello World!";

            const initWorkerTimeout = setTimeout(() => {
                console.log("Web workers are supported by this browser, but don't work!?");
                worker.terminate();
                done();
            }, jasmine.DEFAULT_TIMEOUT_INTERVAL / 2);

            worker.postMessage(ENDPOINT_BASE_URL);

            worker.onmessage = (e) => {
                if (e.data === "initialized") {
                    clearTimeout(initWorkerTimeout);
                } else if (e.data === "connected") {
                    worker.postMessage(testMessage);
                } else {
                    expect(e.data).toBe(`Received message: ${testMessage}`);
                    worker.terminate();
                    done();
                }
            };
        } else {
            console.log("Web workers are not supported by this browser!");
            done();
        }
    });
});
