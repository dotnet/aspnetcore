// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    internal class SatelliteResourcesLoader
    {
        internal const string GetSatelliteAssemblies = "window.Blazor._internal.getSatelliteAssemblies";
        internal const string ReadSatelliteAssemblies = "window.Blazor._internal.readSatelliteAssemblies";

        private readonly WebAssemblyJSRuntimeInvoker _invoker;

        // For unit testing.
        internal SatelliteResourcesLoader(WebAssemblyJSRuntimeInvoker invoker)
        {
            _invoker = invoker;
        }

        public virtual async ValueTask LoadCurrentCultureResourcesAsync()
        {
            var culturesToLoad = GetCultures(CultureInfo.CurrentCulture);

            if (culturesToLoad.Count == 0)
            {
                return;
            }

            // Now that we know the cultures we care about, let WebAssemblyResourceLoader (in JavaScript) load these
            // assemblies. We effectively want to resovle a Task<byte[][]> but there is no way to express this
            // using interop. We'll instead do this in two parts:
            // getSatelliteAssemblies resolves when all satellite assemblies to be loaded in .NET are fetched and available in memory.
            var count = (int)await _invoker.InvokeUnmarshalled<string[], object, object, Task<object>>(
                GetSatelliteAssemblies,
                culturesToLoad.ToArray(),
                null,
                null);

            if (count == 0)
            {
                return;
            }

            // readSatelliteAssemblies resolves the assembly bytes
            var assemblies = _invoker.InvokeUnmarshalled<object, object, object, object[]>(
                ReadSatelliteAssemblies,
                null,
                null,
                null);

            for (var i = 0; i < assemblies.Length; i++)
            {
                Assembly.Load((byte[])assemblies[i]);
            }
        }

        internal static List<string> GetCultures(CultureInfo cultureInfo)
        {
            var culturesToLoad = new List<string>();

            // Once WASM is ready, we have to use .NET's assembly loading to load additional assemblies.
            // First calculate all possible cultures that the application might want to load. We do this by
            // starting from the current culture and walking up the graph of parents.
            // At the end of the the walk, we'll have a list of culture names that look like
            // [ "fr-FR", "fr" ]
            while (cultureInfo != null && cultureInfo != CultureInfo.InvariantCulture)
            {
                culturesToLoad.Add(cultureInfo.Name);

                if (cultureInfo.Parent == cultureInfo)
                {
                    break;
                }

                cultureInfo = cultureInfo.Parent;
            }

            return culturesToLoad;
        }
    }
}
