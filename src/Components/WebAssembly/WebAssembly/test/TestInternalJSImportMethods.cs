// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

internal sealed class TestInternalJSImportMethods : IInternalJSImportMethods
{
    private readonly string _environment;

    public TestInternalJSImportMethods(string environment = "Production")
    {
        _environment = environment;
    }

    public string GetApplicationEnvironment()
        => _environment;

    public string GetPersistedState()
        => null;

    public void AttachRootComponentToElement(string domElementSelector, int componentId, int rendererId) { }

    public void EndUpdateRootComponents(long batchId) { }

    public void NavigationManager_EnableNavigationInterception(int rendererId) { }

    public void NavigationManager_ScrollToElement(string id) { }

    public string NavigationManager_GetBaseUri()
        => "https://www.example.com/awesome-part-that-will-be-truncated-in-tests";

    public string NavigationManager_GetLocationHref()
        => "https://www.example.com/awesome-part-that-will-be-truncated-in-tests/cool";

    public void NavigationManager_SetHasLocationChangingListeners(int rendererId, bool value) { }

    public string RegisteredComponents_GetAssembly(int index)
        => string.Empty;

    public string RegisteredComponents_GetParameterDefinitions(int index)
        => string.Empty;

    public string RegisteredComponents_GetParameterValues(int index)
        => string.Empty;

    public int RegisteredComponents_GetRegisteredComponentsCount()
        => 0;

    public string RegisteredComponents_GetTypeName(int index)
        => string.Empty;
}
