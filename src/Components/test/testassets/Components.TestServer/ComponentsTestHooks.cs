// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;

namespace TestServer;

public static class ComponentsTestHooks
{
    private static readonly Type _componentsHotReloadManagerType = typeof(ComponentBase).Assembly.GetType("Microsoft.AspNetCore.Components.HotReload.HotReloadManager")
        ?? throw new InvalidOperationException("Failed to locate HotReloadManager for test hooks.");
    private static readonly MethodInfo _setIsSupportedOverrideForTestMethod = _componentsHotReloadManagerType.GetMethod("SetIsSupportedOverrideForTest", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Could not find test override hook for metadata update support.");
    private static readonly MethodInfo _updateApplicationMethod = _componentsHotReloadManagerType.GetMethod("UpdateApplication", BindingFlags.Static | BindingFlags.Public)
        ?? throw new InvalidOperationException("Could not find hot reload update entry point.");

    public static IDisposable SetDisableThrowNavigationExceptionForTest(bool disableThrowNavigationException)
        => HttpNavigationManager.SetThrowNavigationExceptionOverrideForTest(!disableThrowNavigationException);

    public static IDisposable SetMetadataUpdaterIsSupportedForTest(bool isSupported)
        => (IDisposable)_setIsSupportedOverrideForTestMethod.Invoke(obj: null, parameters: [isSupported])!;

    public static void TriggerHotReloadForTest()
        => _ = _updateApplicationMethod.Invoke(obj: null, parameters: [null]);
}
