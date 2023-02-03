// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Loader;
using Microsoft.JSInterop;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

/// <summary>
/// Provides a service for loading assemblies at runtime in a browser context.
///
/// Supports finding pre-loaded assemblies in a server or pre-rendering context.
/// </summary>
public sealed partial class LazyAssemblyLoader
{
    private HashSet<string>? _loadedAssemblyCache;

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
    private async Task<IEnumerable<Assembly>> LoadAssembliesInClientAsync(IEnumerable<string> assembliesToLoad)
    {
        if (_loadedAssemblyCache is null)
        {
            var loadedAssemblyCache = new HashSet<string>(StringComparer.Ordinal);
            var appDomainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < appDomainAssemblies.Length; i++)
            {
                var assembly = appDomainAssemblies[i];
                loadedAssemblyCache.Add(assembly.GetName().Name + ".dll");
            }

            _loadedAssemblyCache = loadedAssemblyCache;
        }

        // Check to see if the assembly has already been loaded and avoids reloading it if so.
        // Note: in the future, as an extra precaution, we can call `Assembly.Load` and check
        // to see if it throws FileNotFound to ensure that an assembly hasn't been loaded
        // between when the cache of loaded assemblies was instantiated in the constructor
        // and the invocation of this method.
        var newAssembliesToLoad = new List<string>();
        foreach (var assemblyToLoad in assembliesToLoad)
        {
            if (!_loadedAssemblyCache.Contains(assemblyToLoad))
            {
                newAssembliesToLoad.Add(assemblyToLoad);
            }
        }

        if (newAssembliesToLoad.Count == 0)
        {
            return Array.Empty<Assembly>();
        }

        var loadedAssemblies = new List<Assembly>();
        var pendingLoads = newAssembliesToLoad.Select(async assemblyToLoad =>
        {
            byte[]? buffer = null;
            GCHandle pinnedArray = default;

            Func<int, int, IntPtr> allocator = (dllLength, pdbLength) =>
            {
                buffer = new byte[dllLength + pdbLength];
                pinnedArray = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                return pinnedArray.AddrOfPinnedObject();
            };
            Action<int, int> loader = (dllLength, pdbLength) =>
            {
                if (buffer == null)
                {
                    throw new InvalidOperationException("The buffer should not be null.");
                }

                Assembly loadedAssembly;
                if (pdbLength == 0)
                {
                    loadedAssembly = AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(buffer));
                }
                else
                {
                    loadedAssembly = AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(buffer, 0, dllLength), new MemoryStream(buffer, dllLength, pdbLength));
                }
                loadedAssemblies.Add(loadedAssembly);
                _loadedAssemblyCache.Add(assemblyToLoad);
            };

            await LazyAssemblyLoaderInterop.LoadLazyAssembly(assemblyToLoad, allocator, loader);
            if (pinnedArray.IsAllocated)
            {
                pinnedArray.Free();
            }
        });

        await Task.WhenAll(pendingLoads);
        return loadedAssemblies;
    }

    private partial class LazyAssemblyLoaderInterop
    {
        [JSImport("Blazor._internal.loadLazyAssembly", "blazor-internal")]
        public static partial Task LoadLazyAssembly(string assemblyToLoad,
            [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>] Func<int, int, IntPtr> allocator,
            [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Action<int, int> loader);
    }
}
