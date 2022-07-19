// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using Microsoft.AspNetCore.Components.Web;
using WebAssembly.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Infrastructure;

internal interface IComponentsInternalCalls : IWebAssemblyInternalCalls
{
    byte[] RetrieveByteArray();
    int GetRegisteredComponentsCount();
    int GetId(int index);
    string GetAssembly(int id);
    string GetTypeName(int id);
    string GetParameterDefinitions(int id);
    string GetParameterValues(int id);

    void EnableNavigationInterception();
    string GetLocationHref();
    string GetBaseUri();

    Task<int> GetSatelliteAssemblies(string[] culturesToLoad);
    int ReadSatelliteAssembliesCount();
    byte[] ReadSatelliteAssembly(int index);
    byte[] GetConfig(string configFile);
    string GetApplicationEnvironment();
    string? GetPersistedState();
    Task InitHotReload(string url);
    unsafe void RenderBatch(int id, void* batch);
    Task<int> GetLazyAssemblies(string[] assembliesToLoad);
    int ReadLazyAssembliesCount();
    byte[] ReadLazyAssembly(int index);
    byte[]? ReadLazyPDB(int index);
    void ConsoleDebug(string message);
    void ConsoleInfo(string message);
    void ConsoleWarn(string message);
    void ConsoleError(string message);
    void DotNetCriticalError(string message);
}

/// <summary>
/// Methods that map to the functions compiled into the Mono WebAssembly runtime,
/// as defined by 'mono_add_internal_call' calls in driver.c.
/// Or inside Blazor's Boot.WebAssembly.ts
/// </summary>
internal partial class DefaultComponentsInternalCalls : DefaultWebAssemblyInternalCalls, IComponentsInternalCalls
{
    public static new readonly IComponentsInternalCalls Instance = new DefaultComponentsInternalCalls();

    public byte[] RetrieveByteArray() => _RetrieveByteArray();
    public int GetRegisteredComponentsCount() => _GetRegisteredComponentsCount();
    public int GetId(int index) => _GetId(index);
    public string GetAssembly(int id) => _GetAssembly(id);
    public string GetTypeName(int id) => _GetTypeName(id);
    public string GetParameterDefinitions(int id) => _GetParameterDefinitions(id);
    public string GetParameterValues(int id) => _GetParameterValues(id);

    public void EnableNavigationInterception() => _EnableNavigationInterception();
    public string GetLocationHref() => _GetLocationHref();
    public string GetBaseUri() => _GetBaseUri();

    public Task<int> GetSatelliteAssemblies(string[] culturesToLoad) => _GetSatelliteAssemblies(culturesToLoad);
    public int ReadSatelliteAssembliesCount() => _ReadSatelliteAssembliesCount();
    public byte[] ReadSatelliteAssembly(int index) => _ReadSatelliteAssembly(index);
    public byte[] GetConfig(string configFile) => _GetConfig(configFile);
    public string GetApplicationEnvironment() => _GetApplicationEnvironment();
    public string? GetPersistedState() => _GetPersistedState();
    public Task InitHotReload(string url) => _InitHotReload(url);
    public unsafe void RenderBatch(int id, void* batch) => _RenderBatch(id, batch);
    public Task<int> GetLazyAssemblies(string[] assembliesToLoad) => _GetLazyAssemblies(assembliesToLoad);
    public int ReadLazyAssembliesCount() => _ReadLazyAssembliesCount();
    public byte[] ReadLazyAssembly(int index) => _ReadLazyAssembly(index);
    public byte[]? ReadLazyPDB(int index) => _ReadLazyPDB(index);
    public void ConsoleDebug(string message) => _ConsoleDebug(message);
    public void ConsoleInfo(string message) => _ConsoleInfo(message);
    public void ConsoleWarn(string message) => _ConsoleWarn(message);
    public void ConsoleError(string message) => _ConsoleError(message);
    public void DotNetCriticalError(string message) => _DotNetCriticalError(message);

    [JSImport("Blazor._internal.retrieveByteArray")]
    private static partial byte[] _RetrieveByteArray();

    private const string RegisteredComponentsPrefix = "Blazor._internal.registeredComponents.";

    [JSImport(RegisteredComponentsPrefix + "getRegisteredComponentsCount")]
    private static partial int _GetRegisteredComponentsCount();

    [JSImport(RegisteredComponentsPrefix + "getId")]
    private static partial int _GetId(int index);

    [JSImport(RegisteredComponentsPrefix + "getAssembly")]
    private static partial string _GetAssembly(int id);

    [JSImport(RegisteredComponentsPrefix + "getTypeName")]
    private static partial string _GetTypeName(int id);

    [JSImport(RegisteredComponentsPrefix + "getParameterDefinitions")]
    private static partial string _GetParameterDefinitions(int id);

    [JSImport(RegisteredComponentsPrefix + "getParameterValues")]
    private static partial string _GetParameterValues(int id);

    [JSImport(BrowserNavigationManagerInterop.EnableNavigationInterceptionName)]
    private static partial void _EnableNavigationInterception();

    [JSImport(BrowserNavigationManagerInterop.NavigationManagerPrefix + "getLocationHref")]
    private static partial string _GetLocationHref();

    [JSImport(BrowserNavigationManagerInterop.NavigationManagerPrefix + "getBaseUri")]
    private static partial string _GetBaseUri();

    [JSImport("Blazor._internal.getSatelliteAssemblies")]
    private static partial Task<int> _GetSatelliteAssemblies(string[] culturesToLoad);
    [JSImport("Blazor._internal.readSatelliteAssembliesCount")]
    private static partial int _ReadSatelliteAssembliesCount();
    [JSImport("Blazor._internal.readSatelliteAssembly")]
    private static partial byte[] _ReadSatelliteAssembly(int index);

    [JSImport("Blazor._internal.getConfig")]
    private static partial byte[] _GetConfig(string configFile);
    [JSImport("Blazor._internal.getApplicationEnvironment")]
    private static partial string _GetApplicationEnvironment();
    [JSImport("Blazor._internal.getPersistedState")]
    private static partial string? _GetPersistedState();
    [JSImport("Blazor._internal.initHotReload")]
    private static partial Task _InitHotReload(string url);

    [JSImport("Blazor._internal.renderBatch")]
    private static unsafe partial void _RenderBatch(int id, void* batch);

    [JSImport("Blazor._internal.getLazyAssemblies")]
    private static partial Task<int> _GetLazyAssemblies(string[] assembliesToLoad);
    [JSImport("Blazor._internal.readLazyAssembliesCount")]
    private static partial int _ReadLazyAssembliesCount();
    [JSImport("Blazor._internal.readLazyAssembly")]
    private static partial byte[] _ReadLazyAssembly(int index);
    [JSImport("Blazor._internal.readLazyPdb")]
    private static partial byte[]? _ReadLazyPDB(int index);

    [JSImport("globalThis.console.debug")]
    public static partial void _ConsoleDebug(string message);
    [JSImport("globalThis.console.info")]
    public static partial void _ConsoleInfo(string message);
    [JSImport("globalThis.console.warn")]
    public static partial void _ConsoleWarn(string message);
    [JSImport("globalThis.console.error")]
    public static partial void _ConsoleError(string message);
    [JSImport("Blazor._internal.dotNetCriticalError")]
    public static partial void _DotNetCriticalError(string message);
}
