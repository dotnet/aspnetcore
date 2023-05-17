// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using BlazorUnitedApp.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// HACK: Making the trimmer include these types.
_ = typeof(CounterButtonWasm).FullName; 
_ = typeof(CounterButtonServer).FullName;

await builder.Build().RunAsync();
