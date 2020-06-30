// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Runtime.Loader;
using Microsoft.JSInterop.WebAssembly;

namespace Microsoft.AspNetCore.Components.WebAssembly
{
    public class WebAssemblyDynamicAssemblyLoader
    {
        internal const string GetDynamicAssemblies = "window.Blazor._internal.getDynamicAssemblies";
        internal const string ReadDynamicAssemblies = "window.Blazor._internal.readDynamicAssemblies";

        private List<string> _loadedAssemblyCache = new List<string>();

        private readonly WebAssemblyJSRuntime _jsRuntime;

        internal WebAssemblyDynamicAssemblyLoader(WebAssemblyJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<IEnumerable<Assembly>> LoadAssembliesAsync(IEnumerable<string> assembliesToLoad)
        {
            // Only load assemblies that haven't already been lazily-loaded
            var newAssembliesToLoad = assembliesToLoad.Where(assembly => !_loadedAssemblyCache.Contains(assembly));
            var loadedAssemblies = new List<Assembly>();

            var count = (int)await _jsRuntime.InvokeUnmarshalled<string[], object, object, Task<object>>(
                GetDynamicAssemblies,
                assembliesToLoad.ToArray(),
                null,
                null);

            if (count == 0)
            {
                return loadedAssemblies;
            }

            var assemblies = _jsRuntime.InvokeUnmarshalled<object, object, object, object[]>(
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
