// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Adds the ProblemDetailsJsonContext to the current JsonSerializerOptions.
///
/// This allows for consistent serialization behavior for ProblemDetails regardless if
/// the default reflection-based serializer is used or not. And makes it trim/NativeAOT compatible.
/// </summary>
internal sealed class ProblemDetailsJsonOptionsSetup : IConfigureOptions<JsonOptions>
{
    public void Configure(JsonOptions options)
    {
        // Always insert the ProblemDetailsJsonContext to the beginning of the chain at the time
        // this Configure is invoked. This JsonTypeInfoResolver will be before the default reflection-based resolver,
        // and before any other resolvers currently added.
        // If apps need to customize ProblemDetails serialization, they can prepend a custom ProblemDetails resolver
        // to the chain in an IConfigureOptions<JsonOptions> registered after the call to AddProblemDetails().
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, new ProblemDetailsJsonContext());
    }
}
