// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

// Protected browser storage round-trips application types, so its serializer needs the
// application's contracts. Those reach this assembly through the registered metadata contexts,
// which are the public shape of that data; the resolver interface behind them is not visible here.
[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
internal sealed class ProtectedBrowserStorageSerializerOptions
{
    public ProtectedBrowserStorageSerializerOptions(IServiceProvider services)
    {
        Options = Create(services);
    }

    public JsonSerializerOptions Options { get; }

    private static JsonSerializerOptions Create(IServiceProvider services)
    {
        var applicationResolver = ComponentJsonMetadata.GetApplicationResolver(services);
        if (applicationResolver is null)
        {
            return JsonSerializerOptionsProvider.Options;
        }

        var options = new JsonSerializerOptions(JsonSerializerOptionsProvider.Options);

        // Copying the options copies whatever resolver the shared instance already materialized, so
        // the chain is reset before the ordered one is built.
        options.TypeInfoResolverChain.Clear();

        options.TypeInfoResolverChain.Add(applicationResolver);

        // Reflection goes last, so generated contracts win, and is added only where reflection-based
        // serialization is available at all. In a native build its absence is what makes an
        // unregistered type fail loudly instead of silently.
        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            options.TypeInfoResolverChain.Add(CreateReflectionResolver());
        }

        return options;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Guarded by JsonSerializer.IsReflectionEnabledByDefault.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "Guarded by JsonSerializer.IsReflectionEnabledByDefault.")]
    private static DefaultJsonTypeInfoResolver CreateReflectionResolver() => new();
}
