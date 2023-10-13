// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Loader;
using Microsoft.JSInterop;
using System.Linq;
using System.Runtime.Versioning;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

/// <summary>
/// Provides a service for loading assemblies at runtime in a browser context.
///
/// Supports finding pre-loaded assemblies in a server or pre-rendering context.
/// </summary>
public sealed partial class LazyAssemblyLoader
{
    /// <summary>
    /// Initializes a new instance of <see cref="LazyAssemblyLoader"/>.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
    public LazyAssemblyLoader(IJSRuntime jsRuntime)
    {
    }

    /// <summary>
    /// In a browser context, calling this method will fetch the assemblies requested
    /// via a network call and load them into the runtime. In a server or pre-rendered
    /// context, this method will look for the assemblies already loaded in the runtime
    /// and return them.
    /// </summary>
    /// <param name="assembliesToLoad">The names of the assemblies to load (e.g. "MyAssembly.dll")</param>
    /// <returns>A list of the loaded <see cref="Assembly"/></returns>
    [RequiresUnreferencedCode("Types and members the loaded assemblies depend on might be removed")]
    public Task<IEnumerable<Assembly>> LoadAssembliesAsync(IEnumerable<string> assembliesToLoad)
    {
        if (OperatingSystem.IsBrowser())
        {
            return LoadAssembliesInClientAsync(assembliesToLoad);
        }

        return LoadAssembliesInServerAsync(assembliesToLoad);
    }

    private static Task<IEnumerable<Assembly>> LoadAssembliesInServerAsync(IEnumerable<string> assembliesToLoad)
    {
        var loadedAssemblies = new List<Assembly>();

        try
        {
            foreach (var assemblyName in assembliesToLoad)
            {
                loadedAssemblies.Add(Assembly.Load(Path.GetFileNameWithoutExtension(assemblyName)));
            }
        }
        catch (FileNotFoundException ex)
        {
            throw new InvalidOperationException($"Unable to find the following assembly: {ex.FileName}. Make sure that the appplication is referencing the assemblies and that they are present in the output folder.");
        }

        return Task.FromResult<IEnumerable<Assembly>>(loadedAssemblies);
    }

    [RequiresUnreferencedCode("Types and members the loaded assemblies depend on might be removed")]
    [SupportedOSPlatform("browser")]
    private static async Task<IEnumerable<Assembly>> LoadAssembliesInClientAsync(IEnumerable<string> assembliesToLoad)
    {
        var newAssembliesToLoad = assembliesToLoad.ToList();
        var loadedAssemblies = new List<Assembly>();
        var pendingLoads = newAssembliesToLoad.Select(LazyAssemblyLoaderInterop.LoadLazyAssembly);

        var loadedStatus = await Task.WhenAll(pendingLoads);
        int i = 0;

        List<Assembly>? allAssemblies = null;
        foreach (var loaded in loadedStatus)
        {
            if (loaded)
            {
                if (allAssemblies == null)
                {
                    allAssemblies = AssemblyLoadContext.Default.Assemblies.ToList();
                }

                var assemblyName = Path.GetFileNameWithoutExtension(newAssembliesToLoad[i]);
                var assembly = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName);
                if (assembly != null)
                {
                    loadedAssemblies.Add(assembly);
                }
            }

            i++;
        }

        return loadedAssemblies;
    }

    private partial class LazyAssemblyLoaderInterop
    {
        [JSImport("INTERNAL.loadLazyAssembly")]
        public static partial Task<bool> LoadLazyAssembly(string assemblyToLoad);
    }
}
