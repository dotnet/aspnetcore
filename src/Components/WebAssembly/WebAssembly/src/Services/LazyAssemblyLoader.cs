// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
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

    internal static IEnumerable<string> GetRequiredAssemblyNames(ComponentMarker marker)
    {
        yield return marker.Assembly!;

        var definitions = WebAssemblyComponentParameterDeserializer.GetParameterDefinitions(marker.ParameterDefinitions!);
        foreach (var definition in definitions)
        {
            if (definition.Assembly is not null
                && !(definition.TypeName == typeof(SerializedRenderFragment).FullName
                    && definition.Assembly == "Microsoft.AspNetCore.Components.Endpoints"))
            {
                yield return definition.Assembly;

                // Bound parsing by the serialized name length so valid complex generic names
                // are not rejected by the parser's default node limit.
                if (definition.TypeName is { } typeName && typeName.Contains("[[", StringComparison.Ordinal)
                    && TypeName.TryParse(typeName.AsSpan(), out var parsedTypeName, new TypeNameParseOptions { MaxNodes = typeName.Length }))
                {
                    foreach (var assemblyName in GetGenericArgumentAssemblyNames(parsedTypeName))
                    {
                        yield return assemblyName;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Loads assemblies that are not already loaded.
    /// </summary>
    /// <param name="assemblyNames">The simple names of the assemblies to load (without .dll extension)</param>
    [RequiresUnreferencedCode("Types and members the loaded assemblies depend on might be removed")]
    internal async Task LoadMissingAssembliesAsync(IEnumerable<string> assemblyNames)
    {
        var loadedNames = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetName().Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toLoad = assemblyNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(a => !loadedNames.Contains(a))
            // Binding before a lazy assembly is downloaded can cache a failed bind on CoreCLR.
            .Where(a => !OperatingSystem.IsBrowser() || IsLazyAssembly(a))
            .Select(a => a + ".dll")
            .ToList();

        if (toLoad.Count > 0)
        {
            await LoadAssembliesAsync(toLoad);
        }
    }

    private static IEnumerable<string> GetGenericArgumentAssemblyNames(TypeName typeName)
    {
        while (typeName.IsArray || typeName.IsPointer || typeName.IsByRef)
        {
            typeName = typeName.GetElementType();
        }

        foreach (var argument in typeName.GetGenericArguments())
        {
            if (argument.AssemblyName is { } assemblyName)
            {
                yield return assemblyName.Name;
            }

            foreach (var nestedAssemblyName in GetGenericArgumentAssemblyNames(argument))
            {
                yield return nestedAssemblyName;
            }
        }
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
            throw new InvalidOperationException($"Unable to find the following assembly: {ex.FileName}. Make sure that the application is referencing the assemblies and that they are present in the output folder.");
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

        // this will download the files and install them into memory
        // it would not load them into the VM until we call LoadFromAssemblyName below
        var loadedStatus = await Task.WhenAll(pendingLoads);
        int i = 0;

        foreach (var loaded in loadedStatus)
        {
            if (loaded)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(newAssembliesToLoad[i]);
                try
                {
                    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(assemblyName));
                    if (assembly != null)
                    {
                        loadedAssemblies.Add(assembly);
                    }
                }
                catch (FileNotFoundException)
                {
                    // no op
                }
            }

            i++;
        }

        return loadedAssemblies;
    }

    [SupportedOSPlatform("browser")]
    internal static bool IsLazyAssembly(string assemblyName) => LazyAssemblyLoaderInterop.IsLazyAssembly(assemblyName);

    private partial class LazyAssemblyLoaderInterop
    {
        [JSImport("Blazor._internal.isLazyAssembly", "blazor-internal")]
        public static partial bool IsLazyAssembly(string assemblyName);

        [JSImport("INTERNAL.loadLazyAssembly")]
        public static partial Task<bool> LoadLazyAssembly(string assemblyToLoad);
    }
}
