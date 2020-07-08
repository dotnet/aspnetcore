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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.WebAssembly;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services
{
    /// <summary>
    /// Provides a service for loading assemblies at runtime in a browser context.
    ///
    /// Supports finding pre-loaded assemblies in a server or pre-rendering context.
    /// </summary>
    public class LazyAssemblyLoader
    {
        internal const string GetDynamicAssemblies = "window.Blazor._internal.getLazyAssemblies";
        internal const string ReadDynamicAssemblies = "window.Blazor._internal.readLazyAssemblies";

        private List<Assembly> _loadedAssemblyCache = new List<Assembly>();

        private readonly IServiceProvider _provider;

        public LazyAssemblyLoader(IServiceProvider provider)
        {
            _provider = provider;
            _loadedAssemblyCache = AppDomain.CurrentDomain.GetAssemblies().ToList();
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Browser))
            {
                return await LoadAssembliesInClientAsync(assembliesToLoad);
            }

            return await LoadAssembliesInServerAsync(assembliesToLoad);
        }

        private Task<IEnumerable<Assembly>> LoadAssembliesInServerAsync(IEnumerable<string> assembliesToLoad)
        {
            var loadedAssemblies = _loadedAssemblyCache.Where(assembly =>
                assembliesToLoad.Contains(assembly.GetName().Name + ".dll"));

            if (loadedAssemblies.Count() != assembliesToLoad.Count())
            {
                var unloadedAssemblies = assembliesToLoad.Except(loadedAssemblies.Select(a => a.GetName().Name + ".dll"));
                throw new InvalidOperationException($"Unable to find the following assemblies: {string.Join(",", unloadedAssemblies)}. Make sure that the appplication is referencing the assemblies and that they are present in the output folder.");
            }

            return Task.FromResult(loadedAssemblies);
        }

        private async Task<IEnumerable<Assembly>> LoadAssembliesInClientAsync(IEnumerable<string> assembliesToLoad)
        {
            var jsRuntime = _provider.GetRequiredService<IJSRuntime>();
            // Only load assemblies that haven't already been lazily-loaded
            var newAssembliesToLoad = assembliesToLoad.Except(_loadedAssemblyCache.Select(a => a.GetName().Name + ".dll"));
            var loadedAssemblies = new List<Assembly>();

            var count = (int)await ((WebAssemblyJSRuntime)jsRuntime).InvokeUnmarshalled<string[], object, object, Task<object>>(
                GetDynamicAssemblies,
                assembliesToLoad.ToArray(),
                null,
                null);

            if (count == 0)
            {
                return loadedAssemblies;
            }

            var assemblies = ((WebAssemblyJSRuntime)jsRuntime).InvokeUnmarshalled<object, object, object, object[]>(
                ReadDynamicAssemblies,
                null,
                null,
                null);

            foreach (byte[] assembly in assemblies)
            {
                // The runtime loads assemblies into an isolated context by default. As a result,
                // assemblies that are loaded via Assembly.Load aren't available in the app's context
                // AKA the default context. To work around this, we explicitly load the assemblies
                // into the default app context.
                var loadedAssembly = AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(assembly));
                loadedAssemblies.Add(loadedAssembly);
                _loadedAssemblyCache.Add(loadedAssembly);
            }

            return loadedAssemblies;
        }
    }
}
