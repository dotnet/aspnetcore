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
        if (options.SerializerOptions.TypeInfoResolver is not null && options.SerializerOptions.TypeInfoResolver is not DefaultJsonTypeInfoResolver)
        {
            // Combine the current resolver with our internal problem details context
            options.SerializerOptions.AddContext<ProblemDetailsJsonContext>();
        }
    }
}
