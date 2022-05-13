// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

internal sealed class CircuitOptionsJavaScriptInitializersConfiguration : IConfigureOptions<CircuitOptions>
{
    private readonly IWebHostEnvironment _environment;

    public CircuitOptionsJavaScriptInitializersConfiguration(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public void Configure(CircuitOptions options)
    {
        var file = _environment.WebRootFileProvider.GetFileInfo($"{_environment.ApplicationName}.modules.json");
        if (file.Exists)
        {
            var initializers = JsonSerializer.Deserialize<string[]>(file.CreateReadStream());
            for (var i = 0; i < initializers.Length; i++)
            {
                var initializer = initializers[i];
                options.JavaScriptInitializers.Add(initializer);
            }
        }
    }
}
