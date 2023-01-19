// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { rendererAttached } from "../Rendering/WebRendererInteropMethods";
export class JSInitializer {
    constructor() {
        this.afterStartedCallbacks = [];
    }
    async importInitializersAsync(initializerFiles, initializerArguments) {
        await Promise.all(initializerFiles.map(f => importAndInvokeInitializer(this, f)));
        function adjustPath(path) {
            // This is the same we do in JS interop with the import callback
            const base = document.baseURI;
            path = base.endsWith('/') ? `${base}${path}` : `${base}/${path}`;
            return path;
        }
        async function importAndInvokeInitializer(jsInitializer, path) {
            const adjustedPath = adjustPath(path);
            const initializer = await import(/* webpackIgnore: true */ adjustedPath);
            if (initializer === undefined) {
                return;
            }
            const { beforeStart: beforeStart, afterStarted: afterStarted } = initializer;
            if (afterStarted) {
                jsInitializer.afterStartedCallbacks.push(afterStarted);
            }
            if (beforeStart) {
                return beforeStart(...initializerArguments);
            }
        }
    }
    async invokeAfterStartedCallbacks(blazor) {
        await rendererAttached;
        await Promise.all(this.afterStartedCallbacks.map(callback => callback(blazor)));
    }
}
//# sourceMappingURL=JSInitializers.js.map