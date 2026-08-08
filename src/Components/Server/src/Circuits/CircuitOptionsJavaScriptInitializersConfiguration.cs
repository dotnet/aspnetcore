// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

internal sealed partial class CircuitOptionsJavaScriptInitializersConfiguration : IConfigureOptions<CircuitOptions>
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
            // The contract is generated rather than reflected over so that startup keeps working in an
            // application that disabled reflection-based serialization, which Native AOT does by default.
            var initializers = JsonSerializer.Deserialize(
                file.CreateReadStream(),
                JavaScriptInitializersManifestJsonContext.Default.StringArray);
            if (initializers is not null)
            {
                for (var i = 0; i < initializers.Length; i++)
                {
                    var initializer = initializers[i];
                    options.JavaScriptInitializers.Add(initializer);
                }
            }
        }
    }

    [JsonSerializable(typeof(string[]))]
    private sealed partial class JavaScriptInitializersManifestJsonContext : JsonSerializerContext;
}
