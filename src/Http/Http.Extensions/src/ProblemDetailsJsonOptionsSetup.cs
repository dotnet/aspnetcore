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
        var typeInfoResolverChain = options.SerializerOptions.TypeInfoResolverChain;
        if (typeInfoResolverChain.Count == 0)
        {
            // Not adding our source gen context when the TypeInfoResolverChain is empty,
            // since adding it will skip the reflection-based resolver and potentially
            // cause unexpected serialization problems
            return;
        }

        var lastResolver = typeInfoResolverChain[typeInfoResolverChain.Count - 1];
        if (lastResolver is DefaultJsonTypeInfoResolver)
        {
            // In this case, the current configuration has a reflection-based resolver at the end
            // and we are inserting our internal ProblemDetails context to be evaluated
            // just before the reflection-based resolver.
            typeInfoResolverChain.Insert(typeInfoResolverChain.Count - 1, ProblemDetailsJsonContext.Default);
        }
        else
        {
            // Combine the current resolver with our internal problem details context (adding last)
            typeInfoResolverChain.Add(ProblemDetailsJsonContext.Default);
        }
    }
}
