// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.TestHost;

/// <summary>
/// A factory for creating <see cref="IWebHostBuilder" /> instances.
/// </summary>
public static class WebHostBuilderFactory
{
    /// <summary>
    /// Resolves an <see cref="IWebHostBuilder" /> defined in the entry point of an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to look for an <see cref="IWebHostBuilder"/> in.</param>
    /// <param name="args">The arguments to use when creating the <see cref="IWebHostBuilder"/> instance.</param>
    /// <returns>An <see cref="IWebHostBuilder"/> instance retrieved from the assembly in <paramref name="assembly"/>.</returns>
    public static IWebHostBuilder? CreateFromAssemblyEntryPoint(Assembly assembly, string[] args)
    {
        var factory = HostFactoryResolver.ResolveWebHostBuilderFactory<IWebHostBuilder>(assembly);
        return factory?.Invoke(args);
    }

    /// <summary>
    /// Resolves an <see cref="IWebHostBuilder" /> defined in an assembly where <typeparamref name="T"/> is declared.
    /// </summary>
    /// <param name="args">The arguments to use when creating the <see cref="IWebHostBuilder"/> instance.</param>
    /// <typeparam name="T">Type contained in the target assembly</typeparam>
    /// <returns>An <see cref="IWebHostBuilder"/> instance retrieved from the assembly.</returns>
    public static IWebHostBuilder? CreateFromTypesAssemblyEntryPoint<T>(string[] args) =>
        CreateFromAssemblyEntryPoint(typeof(T).Assembly, args);
}
