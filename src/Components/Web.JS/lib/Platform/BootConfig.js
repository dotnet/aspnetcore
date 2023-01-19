// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
export class BootConfigResult {
    constructor(bootConfig, applicationEnvironment) {
        this.bootConfig = bootConfig;
        this.applicationEnvironment = applicationEnvironment;
    }
    static async initAsync(loadBootResource, environment) {
        const loaderResponse = loadBootResource !== undefined ?
            loadBootResource('manifest', 'blazor.boot.json', '_framework/blazor.boot.json', '') :
            defaultLoadBlazorBootJson('_framework/blazor.boot.json');
        let bootConfigResponse;
        if (!loaderResponse) {
            bootConfigResponse = await defaultLoadBlazorBootJson('_framework/blazor.boot.json');
        }
        else if (typeof loaderResponse === 'string') {
            bootConfigResponse = await defaultLoadBlazorBootJson(loaderResponse);
        }
        else {
            bootConfigResponse = await loaderResponse;
        }
        // While we can expect an ASP.NET Core hosted application to include the environment, other
        // hosts may not. Assume 'Production' in the absence of any specified value.
        const applicationEnvironment = environment || bootConfigResponse.headers.get('Blazor-Environment') || 'Production';
        const bootConfig = await bootConfigResponse.json();
        bootConfig.modifiableAssemblies = bootConfigResponse.headers.get('DOTNET-MODIFIABLE-ASSEMBLIES');
        bootConfig.aspnetCoreBrowserTools = bootConfigResponse.headers.get('ASPNETCORE-BROWSER-TOOLS');
        return new BootConfigResult(bootConfig, applicationEnvironment);
        function defaultLoadBlazorBootJson(url) {
            return fetch(url, {
                method: 'GET',
                credentials: 'include',
                cache: 'no-cache',
            });
        }
    }
}
export var ICUDataMode;
(function (ICUDataMode) {
    ICUDataMode[ICUDataMode["Sharded"] = 0] = "Sharded";
    ICUDataMode[ICUDataMode["All"] = 1] = "All";
    ICUDataMode[ICUDataMode["Invariant"] = 2] = "Invariant";
})(ICUDataMode || (ICUDataMode = {}));
//# sourceMappingURL=BootConfig.js.map