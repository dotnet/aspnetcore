// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Ensure Microsoft.AspNetCore.EnsureJsonTrimmability=true.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Ensure Microsoft.AspNetCore.EnsureJsonTrimmability=true.")]
internal sealed class MvcCoreJsonOptionsSetup : IPostConfigureOptions<JsonOptions>
{
    public void PostConfigure(string? name, JsonOptions options)
    {
        InitializeForReflection(options);
    }

    private static void InitializeForReflection(JsonOptions options)
    {
        options.JsonSerializerOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine(options.JsonSerializerOptions.TypeInfoResolver, new DefaultJsonTypeInfoResolver());
    }
}
