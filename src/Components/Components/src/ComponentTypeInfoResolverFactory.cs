// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

internal static class ComponentMetadataFeature
{
    internal const string SwitchName =
        "Microsoft.AspNetCore.Components.ComponentMetadata.IsReflectionEnabledByDefault";

    internal const string ReflectionMessage =
        "Component metadata reflection is not compatible with trimming. Set " +
        "'" + SwitchName + "' to 'false' to disable the reflection fallback.";

    internal const string DynamicCodeMessage =
        "Component metadata reflection may require dynamic code generation. Set " +
        "'" + SwitchName + "' to 'false' to disable the reflection fallback.";

    [FeatureSwitchDefinition(SwitchName)]
    [FeatureGuard(typeof(RequiresUnreferencedCodeAttribute))]
    [FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
    internal static bool IsReflectionEnabledByDefault { get; } =
        AppContext.TryGetSwitch(SwitchName, out var value) ? value : true;
}

internal static class ComponentTypeInfoResolverFactory
{
    internal static IComponentTypeInfoResolver Default { get; } = Create(EmptyServiceProvider.Instance);

    internal static IComponentTypeInfoResolver Create(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var resolvers = new List<IComponentTypeInfoResolver>();
        if (services.GetService<IComponentMetadataResolver>() is { } metadataResolver)
        {
            resolvers.Add(new SourceGeneratedComponentTypeInfoResolver(metadataResolver));
        }

        if (ComponentMetadataFeature.IsReflectionEnabledByDefault)
        {
            resolvers.Add(new ReflectionComponentTypeInfoResolver());
        }

        return new CompositeComponentTypeInfoResolver(resolvers);
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        internal static EmptyServiceProvider Instance { get; } = new();

        public object? GetService(Type serviceType) => null;
    }
}
