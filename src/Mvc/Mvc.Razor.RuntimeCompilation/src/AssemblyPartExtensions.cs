// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts;

/// <summary>
/// Static class that adds methods to <see cref="AssemblyPart"/>.
/// </summary>
public static class AssemblyPartExtensions
{
    /// <inheritdoc />
    public static IEnumerable<string> GetReferencePaths(this AssemblyPart assemblyPart)
    {
        var assembly = assemblyPart?.Assembly ?? throw new ArgumentNullException(nameof(assemblyPart));

        if (assembly.IsDynamic)
        {
            // Skip loading process for dynamic assemblies. This prevents DependencyContextLoader from reading the
            // .deps.json file from either manifest resources or the assembly location, which will fail.
            return Enumerable.Empty<string>();
        }

        var dependencyContext = DependencyContext.Load(assembly);
        if (dependencyContext != null)
        {
            return dependencyContext.CompileLibraries.SelectMany(library => library.ResolveReferencePaths());
        }

        // If an application has been compiled without preserveCompilationContext, return the path to the assembly
        // as a reference. For runtime compilation, this will allow the compilation to succeed as long as it least
        // one application part has been compiled with preserveCompilationContext and contains a super set of types
        // required for the compilation to succeed.
        return new[] { assembly.Location };
    }
}
