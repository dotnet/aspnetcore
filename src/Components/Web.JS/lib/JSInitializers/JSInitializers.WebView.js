// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import { JSInitializer } from './JSInitializers';
export async function fetchAndInvokeInitializers() {
    const jsInitializersResponse = await fetch('_framework/blazor.modules.json', {
        method: 'GET',
        credentials: 'include',
        cache: 'no-cache',
    });
    const initializers = await jsInitializersResponse.json();
    const jsInitializer = new JSInitializer();
    await jsInitializer.importInitializersAsync(initializers, []);
    return jsInitializer;
}
//# sourceMappingURL=JSInitializers.WebView.js.map