// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Wasm.Authentication.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Services.AddApiAuthorization();

            // B2C hosted
            //builder.Services.AddMsalAuthentication(options =>
            //{
            //    var authentication = options.ProviderOptions.Authentication;
            //    authentication.Authority = "https://jacalvarb2c.b2clogin.com/jacalvarb2c.onmicrosoft.com/b2c_1_siupin";
            //    authentication.ClientId = "e6b0c1e2-bb93-4cc5-996e-e175c7bd8f1a";
            //    authentication.ValidateAuthority = false;
            //    options.ProviderOptions.DefaultAccessTokenScopes.Add("https://jacalvarb2c.onmicrosoft.com/BlazorWasmAadB2CServerAPI/All");
            //});

            // B2C standalone
            //builder.Services.AddMsalAuthentication(options =>
            //{
            //    var authentication = options.ProviderOptions.Authentication;
            //    authentication.Authority = "https://jacalvarb2c.b2clogin.com/jacalvarb2c.onmicrosoft.com/b2c_1_siupin";
            //    authentication.ClientId = "e6b0c1e2-bb93-4cc5-996e-e175c7bd8f1a";
            //    authentication.ValidateAuthority = false;
            //});

            // AAD hosted
            //builder.Services.AddMsalAuthentication(options =>
            //{
            //    var authentication = options.ProviderOptions.Authentication;
            //    authentication.Authority = "https://login.microsoftonline.com/7e511586-66ec-4108-bc9c-a68dee0dc2aa";
            //    authentication.ClientId = "ae13253e-6630-48c2-9f99-52ec618d9c4c";
            //    options.ProviderOptions.DefaultAccessTokenScopes.Add("api://BlazorWasmAadAPI/All");
            //});

            // AAD standalone
            //builder.Services.AddMsalAuthentication(options =>
            //{
            //    var authentication = options.ProviderOptions.Authentication;
            //    authentication.Authority = "https://login.microsoftonline.com/7e511586-66ec-4108-bc9c-a68dee0dc2aa";
            //    authentication.ClientId = "ae13253e-6630-48c2-9f99-52ec618d9c4c";
            //});

            builder.RootComponents.Add<App>("app");

            await builder.Build().RunAsync();
        }
    }
}
