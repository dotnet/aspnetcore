// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http;

internal sealed class ProblemDetailsJsonOptionsSetup : IPostConfigureOptions<JsonOptions>
{
    public void PostConfigure(string? name, JsonOptions options)
    {
        if (options.SerializerOptions.TypeInfoResolver is not null)
        {
            // Combine the current resolver with our internal problem details context
            options.SerializerOptions.AddContext<ProblemDetailsJsonContext>();
        }
    }
}
