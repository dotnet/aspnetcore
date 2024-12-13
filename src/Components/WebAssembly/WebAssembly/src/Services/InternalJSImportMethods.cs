// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal partial class InternalJSImportMethods : IInternalJSImportMethods
{
    public static readonly InternalJSImportMethods Instance = new();

    private InternalJSImportMethods() { }

    public string GetPersistedState()
        => GetPersistedStateCore();

    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "These are root components which belong to the user and are in assemblies that don't get trimmed.")]
    public static async Task<RootComponentOperationBatch> GetInitialComponentUpdate()
    {
        var components = await GetInitialUpdateCore();
        return DefaultWebAssemblyJSRuntime.DeserializeOperations(components);
    }

    public string GetApplicationEnvironment()
        => GetApplicationEnvironmentCore();

    public void AttachRootComponentToElement(string domElementSelector, int componentId, int rendererId)
        => AttachRootComponentToElementCore(domElementSelector, componentId, rendererId);

    public void EndUpdateRootComponents(long batchId)
        => EndUpdateRootComponentsCore(batchId);

    public void NavigationManager_EnableNavigationInterception(int rendererId)
        => NavigationManager_EnableNavigationInterceptionCore(rendererId);

    public void NavigationManager_ScrollToElement(string id)
        => NavigationManager_ScrollToElementCore(id);

    public string NavigationManager_GetLocationHref()
        => NavigationManager_GetLocationHrefCore();

    public string NavigationManager_GetBaseUri()
        => NavigationManager_GetBaseUriCore();

    public void NavigationManager_SetHasLocationChangingListeners(int rendererId, bool value)
        => NavigationManager_SetHasLocationChangingListenersCore(rendererId, value);

    public int RegisteredComponents_GetRegisteredComponentsCount()
        => RegisteredComponents_GetRegisteredComponentsCountCore();

    public string RegisteredComponents_GetAssembly(int id)
        => RegisteredComponents_GetAssemblyCore(id);

    public string RegisteredComponents_GetTypeName(int id)
        => RegisteredComponents_GetTypeNameCore(id);

    public string RegisteredComponents_GetParameterDefinitions(int id)
        => RegisteredComponents_GetParameterDefinitionsCore(id);

    public string RegisteredComponents_GetParameterValues(int id)
        => RegisteredComponents_GetParameterValuesCore(id);

    [JSImport("Blazor._internal.getPersistedState", "blazor-internal")]
    private static partial string GetPersistedStateCore();

    [JSImport("Blazor._internal.getInitialComponentsUpdate", "blazor-internal")]
    private static partial Task<string> GetInitialUpdateCore();

    [JSImport("Blazor._internal.getApplicationEnvironment", "blazor-internal")]
    private static partial string GetApplicationEnvironmentCore();

    [JSImport("Blazor._internal.attachRootComponentToElement", "blazor-internal")]
    private static partial void AttachRootComponentToElementCore(string domElementSelector, int componentId, int rendererId);

    [JSImport("Blazor._internal.endUpdateRootComponents", "blazor-internal")]
    private static partial void EndUpdateRootComponentsCore([JSMarshalAs<JSType.Number>] long batchId);

    [JSImport(BrowserNavigationManagerInterop.EnableNavigationInterception, "blazor-internal")]
    private static partial void NavigationManager_EnableNavigationInterceptionCore(int rendererId);

    [JSImport(BrowserNavigationManagerInterop.ScrollToElement, "blazor-internal")]
    private static partial void NavigationManager_ScrollToElementCore(string id);

    [JSImport(BrowserNavigationManagerInterop.GetLocationHref, "blazor-internal")]
    private static partial string NavigationManager_GetLocationHrefCore();

    [JSImport(BrowserNavigationManagerInterop.GetBaseUri, "blazor-internal")]
    private static partial string NavigationManager_GetBaseUriCore();

    [JSImport(BrowserNavigationManagerInterop.SetHasLocationChangingListeners, "blazor-internal")]
    private static partial void NavigationManager_SetHasLocationChangingListenersCore(int rendererId, bool value);

    [JSImport(RegisteredComponentsInterop.GetRegisteredComponentsCount, "blazor-internal")]
    private static partial int RegisteredComponents_GetRegisteredComponentsCountCore();

    [JSImport(RegisteredComponentsInterop.GetAssembly, "blazor-internal")]
    private static partial string RegisteredComponents_GetAssemblyCore(int id);

    [JSImport(RegisteredComponentsInterop.GetTypeName, "blazor-internal")]
    private static partial string RegisteredComponents_GetTypeNameCore(int id);

    [JSImport(RegisteredComponentsInterop.GetParameterDefinitions, "blazor-internal")]
    private static partial string RegisteredComponents_GetParameterDefinitionsCore(int id);

    [JSImport(RegisteredComponentsInterop.GetParameterValues, "blazor-internal")]
    private static partial string RegisteredComponents_GetParameterValuesCore(int id);
}
