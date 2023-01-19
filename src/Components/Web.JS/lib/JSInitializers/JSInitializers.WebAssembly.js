// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { JSInitializer } from './JSInitializers';
export async function fetchAndInvokeInitializers(bootConfig, options) {
    const initializers = bootConfig.resources.libraryInitializers;
    const jsInitializer = new JSInitializer();
    if (initializers) {
        await jsInitializer.importInitializersAsync(Object.keys(initializers), [options, bootConfig.resources.extensions]);
    }
    return jsInitializer;
}
//# sourceMappingURL=JSInitializers.WebAssembly.js.map