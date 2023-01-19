// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { JSInitializer } from './JSInitializers';
export async function fetchAndInvokeInitializers(options) {
    const jsInitializersResponse = await fetch('_blazor/initializers', {
        method: 'GET',
        credentials: 'include',
        cache: 'no-cache',
    });
    const initializers = await jsInitializersResponse.json();
    const jsInitializer = new JSInitializer();
    await jsInitializer.importInitializersAsync(initializers, [options]);
    return jsInitializer;
}
//# sourceMappingURL=JSInitializers.Server.js.map