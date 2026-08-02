// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

internal class DefaultRazorComponentsServiceOptionsConfiguration(
    IConfiguration configuration,
    ILoggerFactory loggerFactory,
    IWebHostEnvironment environment)
    : IPostConfigureOptions<RazorComponentsServiceOptions>
{
    public IConfiguration Configuration { get; } = configuration;

    public void PostConfigure(string? name, RazorComponentsServiceOptions options)
    {
        var value = Configuration[WebHostDefaults.DetailedErrorsKey];
        options.DetailedErrors = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);

        options._formMappingOptions = new FormDataMapperOptions(loggerFactory)
        {
            MaxRecursionDepth = options.MaxFormMappingRecursionDepth,
            MaxErrorCount = options.MaxFormMappingErrorCount,
            MaxCollectionSize = options.MaxFormMappingCollectionSize,
            MaxKeyBufferSize = options.MaxFormMappingKeySize
        };

        var file = environment.WebRootFileProvider.GetFileInfo($"{environment.ApplicationName}.modules.json");

        if (file.Exists)
        {
            // We are going to emit the initializers as JSON, so avoid deserializing them, since we are going to
            // serialize them again later.
            using var reader = new StreamReader(file.CreateReadStream());
            options.JavaScriptInitializers = reader.ReadToEnd();

            if (options.JavaScriptInitializers == "")
            {
                options.JavaScriptInitializers = null;
            }
            else
            {
                // Do an extra check to see if the file just contains an empty json array, and if so, set the value to null,
                // as this avoids emitting any value later on in the response.
                try
                {
                    if (options.JavaScriptInitializers != null)
                    {
                        var initializers = JsonSerializer.Deserialize<string[]>(options.JavaScriptInitializers);
                        if (initializers == null || initializers.Length == 0)
                        {
                            options.JavaScriptInitializers = null;
                        }
                    }
                }
                catch (Exception)
                {
                    // We should never get here because the SDK is responsible for generating the file we use,
                    // and that should always produce well formed JSON, but in any case, we just ignore the error
                    // and continue, letting the failure happen on the client.
                }
            }

        }
    }
}
