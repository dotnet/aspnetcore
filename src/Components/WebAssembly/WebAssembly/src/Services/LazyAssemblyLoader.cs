// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.JSInterop.WebAssembly;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services
{
    public class LazyAssemblyLoader
    {
        internal const string GetDynamicAssemblies = "window.Blazor._internal.getLazyAssemblies";
        internal const string ReadDynamicAssemblies = "window.Blazor._internal.readLazyAssemblies";

        private List<string> _loadedAssemblyCache = new List<string>();

        private readonly IServiceProvider _provider;

        public LazyAssemblyLoader(IServiceProvider provider)
        {
            _provider = provider;
        }

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
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly =>
                assembliesToLoad.Contains(assembly.GetName().Name + ".dll"));

            if (assemblies.Count() != assembliesToLoad.Count())
            {
                var unloadedAssemblies = assembliesToLoad.Except(assemblies.Select(assembly => assembly.GetName().Name + ".dll"));
                throw new FileNotFoundException($"Unable to find the following assemblies: {string.Join(",", unloadedAssemblies)}");
            }

            return Task.FromResult(assemblies);
        }

        private async Task<IEnumerable<Assembly>> LoadAssembliesInClientAsync(IEnumerable<string> assembliesToLoad)
        {
            var _jsRuntime = _provider.GetRequiredService<IJSRuntime>();
            // Only load assemblies that haven't already been lazily-loaded
            var newAssembliesToLoad = assembliesToLoad.Where(assembly => !_loadedAssemblyCache.Contains(assembly));
            var loadedAssemblies = new List<Assembly>();

            var count = (int)await ((WebAssemblyJSRuntime)_jsRuntime).InvokeUnmarshalled<string[], object, object, Task<object>>(
                GetDynamicAssemblies,
                assembliesToLoad.ToArray(),
                null,
                null);

            if (count == 0)
            {
                return loadedAssemblies;
            }

            var assemblies = ((WebAssemblyJSRuntime)_jsRuntime).InvokeUnmarshalled<object, object, object, object[]>(
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
                _loadedAssemblyCache.Add(loadedAssembly.GetName().Name + ".dll");
            }

            return loadedAssemblies;
        }
    }
}
