// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal interface IInternalJSImportMethods
{
    string GetPersistedState();

    string GetApplicationEnvironment();

    void AttachRootComponentToElement(string domElementSelector, int componentId, int rendererId);

    void EndUpdateRootComponents(long batchId);

    void NavigationManager_EnableNavigationInterception(int rendererId);

    void NavigationManager_ScrollToElement(string id);

    string NavigationManager_GetLocationHref();

    string NavigationManager_GetBaseUri();

    void NavigationManager_SetHasLocationChangingListeners(int rendererId, bool value);

    int RegisteredComponents_GetRegisteredComponentsCount();

    string RegisteredComponents_GetAssembly(int index);

    string RegisteredComponents_GetTypeName(int index);

    string RegisteredComponents_GetParameterDefinitions(int index);

    string RegisteredComponents_GetParameterValues(int index);
}
