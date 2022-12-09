// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http;

public sealed class HttpJsonOptionsSetup : IConfigureOptions<JsonOptions>
{
    [UnconditionalSuppressMessage("Trimmer", "IL2026", Justification = "Calls System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver.DefaultJsonTypeInfoResolver().")]
    public void Configure(JsonOptions options)
    {
        if (options.SerializerOptions.TypeInfoResolver is null)
        {
            options.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver();

            // TODO: Should we make it readonly????
            options.SerializerOptions.MakeReadOnly();
        }
    }
}
