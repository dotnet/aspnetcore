// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity;

internal sealed class IdentityEndpointsJsonOptionsSetup : IConfigureOptions<JsonOptions>
{
    public void Configure(JsonOptions options)
    {
        // Put our resolver in front of the reflection-based one. See ProblemDetailsOptionsSetup for a detailed explanation.
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, IdentityEndpointsJsonSerializerContext.Default);
    }
}
