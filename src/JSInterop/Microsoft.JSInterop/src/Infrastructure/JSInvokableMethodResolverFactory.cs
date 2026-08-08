// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.JSInterop.Infrastructure;

internal static class JSInvokableMethodResolutionFeature
{
    internal const string SwitchName =
        "Microsoft.JSInterop.JSInvokableMethodResolution.IsReflectionEnabledByDefault";

    [FeatureSwitchDefinition(SwitchName)]
    [FeatureGuard(typeof(RequiresUnreferencedCodeAttribute))]
    [FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
    internal static bool IsReflectionEnabledByDefault { get; } =
        AppContext.TryGetSwitch(SwitchName, out var value) ? value : true;
}

internal static class JSInvokableMethodResolverFactory
{
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "ReflectionJSInvokableMethodResolver is constructed only when the feature-guarded reflection switch is enabled.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification = "ReflectionJSInvokableMethodResolver is constructed only when the feature-guarded reflection switch is enabled.")]
    internal static CompositeJSInvokableMethodResolver Create(JSRuntime runtime)
    {
        var resolvers = new List<IJSInvokableMethodResolver>();
        if (runtime.InvokableMethods is { Count: > 0 } descriptors)
        {
            resolvers.Add(new SourceGeneratedJSInvokableMethodResolver(descriptors));
        }

        if (JSInvokableMethodResolutionFeature.IsReflectionEnabledByDefault)
        {
            resolvers.Add(new ReflectionJSInvokableMethodResolver());
        }

        return new CompositeJSInvokableMethodResolver(resolvers);
    }
}
