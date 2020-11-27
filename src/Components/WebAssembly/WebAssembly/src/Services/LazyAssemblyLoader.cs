// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.JSInterop.WebAssembly;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services
{
    /// <summary>
    /// Provides a service for loading assemblies at runtime in a browser context.
    ///
    /// Supports finding pre-loaded assemblies in a server or pre-rendering context.
    /// </summary>
    public sealed class LazyAssemblyLoader
    {
        internal const string GetLazyAssemblies = "window.Blazor._internal.getLazyAssemblies";
        internal const string ReadLazyAssemblies = "window.Blazor._internal.readLazyAssemblies";
        internal const string ReadLazyPDBs = "window.Blazor._internal.readLazyPdbs";

        private readonly IJSRuntime _jsRuntime;
        private readonly HashSet<string> _loadedAssemblyCache;

        /// <summary>
        /// Initializes a new instance of <see cref="LazyAssemblyLoader"/>.
        /// </summary>
        /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
        public LazyAssemblyLoader(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            _loadedAssemblyCache = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name + ".dll").ToHashSet();
        }

        /// <summary>
        /// In a browser context, calling this method will fetch the assemblies requested
        /// via a network call and load them into the runtime. In a server or pre-rendered
        /// context, this method will look for the assemblies already loaded in the runtime
        /// and return them.
        /// </summary>
        /// <param name="assembliesToLoad">The names of the assemblies to load (e.g. "MyAssembly.dll")</param>
        /// <returns>A list of the loaded <see cref="Assembly"/></returns>
        public async Task<IEnumerable<Assembly>> LoadAssembliesAsync(IEnumerable<string> assembliesToLoad)
        {
            if (OperatingSystem.IsBrowser())
            {
                return await LoadAssembliesInClientAsync(assembliesToLoad);
            }

            return await LoadAssembliesInServerAsync(assembliesToLoad);
        }

        private Task<IEnumerable<Assembly>> LoadAssembliesInServerAsync(IEnumerable<string> assembliesToLoad)
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

        private async Task<IEnumerable<Assembly>> LoadAssembliesInClientAsync(IEnumerable<string> assembliesToLoad)
        {
            // Check to see if the assembly has already been loaded and avoids reloading it if so.
            // Note: in the future, as an extra precuation, we can call `Assembly.Load` and check
            // to see if it throws FileNotFound to ensure that an assembly hasn't been loaded
            // between when the cache of loaded assemblies was instantiated in the constructor
            // and the invocation of this method.
            var newAssembliesToLoad = assembliesToLoad.Where(assembly => !_loadedAssemblyCache.Contains(assembly));
            var loadedAssemblies = new List<Assembly>();

            var count = (int)await ((IJSUnmarshalledRuntime)_jsRuntime).InvokeUnmarshalled<string[], object?, object?, Task<object>>(
               GetLazyAssemblies,
               newAssembliesToLoad.ToArray(),
               null,
               null);

            if (count == 0)
            {
                return loadedAssemblies;
            }

            var assemblies = ((IJSUnmarshalledRuntime)_jsRuntime).InvokeUnmarshalled<object?, object?, object?, byte[][]>(
                ReadLazyAssemblies,
                null,
                null,
                null);

            var pdbs = ((IJSUnmarshalledRuntime)_jsRuntime).InvokeUnmarshalled<object?, object?, object?, byte[][]>(
                ReadLazyPDBs,
                null,
                null,
                null);

            for (int i = 0; i < assemblies.Length; i++)
            {
                // The runtime loads assemblies into an isolated context by default. As a result,
                // assemblies that are loaded via Assembly.Load aren't available in the app's context
                // AKA the default context. To work around this, we explicitly load the assemblies
                // into the default app context.
                var assembly = assemblies[i];
                var pdb = pdbs[i];
                var loadedAssembly = pdb.Length == 0 ?
                    AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(assembly)) :
                    AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(assembly), new MemoryStream(pdb));
                loadedAssemblies.Add(loadedAssembly);
                _loadedAssemblyCache.Add(loadedAssembly.GetName().Name + ".dll");
            }

            return loadedAssemblies;
        }
    }
}
