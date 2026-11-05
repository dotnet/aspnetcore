// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function initialize(target) {
    const controllers = new Map();
    window.cancellationTests = {
        start(id, behavior, alreadyAborted = false) {
            const controller = new AbortController();
            controllers.set(id, controller);
            const signal = controller.signal;
            let listeners = 0;
            const add = signal.addEventListener.bind(signal);
            const remove = signal.removeEventListener.bind(signal);
            signal.addEventListener = (...args) => {
                listeners++;
                document.getElementById(`${id}-listeners`).textContent = listeners;
                add(...args);
            };
            signal.removeEventListener = (...args) => {
                listeners--;
                document.getElementById(`${id}-listeners`).textContent = listeners;
                remove(...args);
            };
            document.getElementById(`${id}-listeners`).textContent = listeners;
            if (alreadyAborted) {
                controller.abort();
            }
            document.getElementById(`${id}-js`).textContent = "pending";
            try {
                target.invokeMethodAsyncWithCancellation("Run", signal, id, behavior).then(
                    value => document.getElementById(`${id}-js`).textContent = value,
                    error => {
                        document.getElementById(`${id}-js`).textContent = error === signal.reason && error.name !== "AbortError"
                            ? "custom reason preserved"
                            : error.name === "AbortError" ? "AbortError" : error.message;
                    });
            } catch (error) {
                document.getElementById(`${id}-js`).textContent = error.message;
            }
        },
        abort(id) {
            controllers.get(id).abort();
        },
        abortWithReason(id) {
            const reason = {};
            reason.self = reason;
            controllers.get(id).abort(reason);
        },
        ordinary() {
            target.invokeMethodAsync("Ordinary").then(value => {
                document.getElementById("ordinary-js").textContent = value;
            });
        },
        staticInvocation() {
            DotNet.invokeMethodAsyncWithCancellation("BasicTestApp", "StaticToken", new AbortController().signal).then(
                value => document.getElementById("static-js").textContent = value,
                error => document.getElementById("static-js").textContent = error.message);
        }
    };
}
