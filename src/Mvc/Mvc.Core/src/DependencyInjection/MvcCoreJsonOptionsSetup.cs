// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class MvcCoreJsonOptionsSetup : IPostConfigureOptions<JsonOptions>
{
    public void PostConfigure(string? name, JsonOptions options)
    {
        if (!TrimmingAppContextSwitches.EnsureJsonTrimmability)
        {
            InitializeForReflection(options);
        }
    }

    [RequiresUnreferencedCode("ABC")]
    [RequiresDynamicCode("ABC")]
    private static void InitializeForReflection(JsonOptions options)
    {
        if (options.JsonSerializerOptions.TypeInfoResolver is null)
        {
            options.JsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
        }

        // or
        //options.JsonSerializerOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine(options.JsonSerializerOptions.TypeInfoResolver, new DefaultJsonTypeInfoResolver());
    }
}
