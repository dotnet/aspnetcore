// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity.Endpoints.DTO;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.Endpoints;

// Review: This is trying to do a similar thing to ProblemDetailsOptionsSetup.
// Is this what we landed on for now? Should we update that too? AddContext will be obsolete soon.
// See: https://github.com/dotnet/runtime/issues/83280
internal sealed class PostConfigureIdentityEndpointJsonOptions : IPostConfigureOptions<JsonOptions>
{
    public void PostConfigure(string? name, JsonOptions options)
    {
        // Add our resolver first so it isn't overridden by the default reflection-based resolver.
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, IdentityEndpointJsonSerializerContext.Default);
    }
}
