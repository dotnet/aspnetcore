// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Routing;

internal sealed class RazorComponentApplicationAssemblyProvider(PersistentComponentState state)
{
    internal static readonly string PersistenceKey = typeof(Router).FullName!;

    private Assembly[]? _assemblies;
    private string[]? _assemblyNames;

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The persisted value is a string array.")]
    internal IReadOnlyList<Assembly>? GetAssemblies()
    {
        if (_assemblyNames is null)
        {
            if (!state.TryTakeFromJson<string[]>(PersistenceKey, out var assemblyNames))
            {
                return null;
            }

            _assemblyNames = assemblyNames;
        }

        if (_assemblies is not null)
        {
            return _assemblies;
        }

        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        var configuredAssemblyNames = _assemblyNames!;
        var assemblies = new List<Assembly>(configuredAssemblyNames.Length);
        foreach (var assemblyName in configuredAssemblyNames)
        {
            Assembly? assembly = null;
            for (var i = 0; i < loadedAssemblies.Length; i++)
            {
                if (string.Equals(loadedAssemblies[i].GetName().Name, assemblyName, StringComparison.Ordinal))
                {
                    assembly = loadedAssemblies[i];
                    break;
                }
            }

            if (assembly is null && OperatingSystem.IsBrowser())
            {
                try
                {
                    assembly = Assembly.Load(assemblyName);
                }
                catch
                {
                }
            }

            if (assembly is not null)
            {
                assemblies.Add(assembly);
            }
        }

        _assemblies = [.. assemblies];

        return _assemblies;
    }
}
