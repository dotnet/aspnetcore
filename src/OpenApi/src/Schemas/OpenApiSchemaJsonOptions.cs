// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class OpenApiSchemaJsonOptions : IConfigureOptions<JsonOptions>
{
    public void Configure(JsonOptions options)
    {
        // Put our resolver in front of the reflection-based one. See ProblemDetailsJsonOptionsSetup for more info.
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, OpenApiJsonSchemaContext.Default);
    }
}
