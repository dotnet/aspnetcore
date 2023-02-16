// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http;

internal sealed class ProblemDetailsJsonOptionsSetup : IPostConfigureOptions<JsonOptions>
{
    public void PostConfigure(string? name, JsonOptions options)
    {
        if (options.SerializerOptions.TypeInfoResolver is not null)
        {
            if (options.SerializerOptions.IsReadOnly)
            {
                options.SerializerOptions = new(options.SerializerOptions);
            }

            // Combine the current resolver with our internal problem details context
            options.SerializerOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine(options.SerializerOptions.TypeInfoResolver!, ProblemDetailsJsonContext.Default);
        }
    }
}
