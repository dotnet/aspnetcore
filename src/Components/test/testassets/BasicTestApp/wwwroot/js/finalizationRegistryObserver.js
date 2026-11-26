// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Loaded before blazor.server.js, so that tests can wait for the cleanup callback of a registry the framework creates.
(() => {
    const cleanups = new WeakMap();

    window.FinalizationRegistry = class extends FinalizationRegistry {
        constructor(callback) {
            const resolvers = new Map();
            super(heldValue => {
                try {
                    callback(heldValue);
                } finally {
                    resolvers.get(heldValue)?.();
                    resolvers.delete(heldValue);
                }
            });
            this.resolvers = resolvers;
        }

        register(target, heldValue, unregisterToken) {
            // The test pages' string held values can repeat, so only object held values are tracked.
            if (typeof heldValue === "object" && heldValue !== null) {
                cleanups.set(target, new Promise(resolve => this.resolvers.set(heldValue, resolve)));
            }
            super.register(target, heldValue, unregisterToken);
        }
    };

    window.finalizationRegistryObserver = {
        // Resolves after the cleanup callback for target has run. Call it before dropping target.
        cleanupOf: target => cleanups.get(target),
    };
})();
