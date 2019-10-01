// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
{
    /// <summary>
    /// This class is a workaround to enable compilation of plugin references which DependencyContext is unaware of
    /// </summary>
    internal class DependencyContextResolver : IAssemblyPartResolver
    {
        public bool IsFullyResolved(AssemblyPart assemblyPart)
        {
            return false;
        }

        public IEnumerable<string> GetReferencePaths(AssemblyPart assemblyPart)
        {
            try
            {
                return assemblyPart.GetReferencePaths();
            }
            catch (InvalidOperationException ex)
            {
                // DependencyContext was unable to resolve the dependencies because it is unaware of additional references.

                throw new Exception($"{nameof(DependencyContextResolver)} failed to resolve the {nameof(AssemblyPart)} {assemblyPart.Assembly.FullName}. " +
                                    $"One possible resolution to this is providing your own implementation of {nameof(IAssemblyPartResolver)} and add it " +
                                    $"to {nameof(MvcRazorRuntimeCompilationOptions)}.{nameof(MvcRazorRuntimeCompilationOptions.AssemblyPartResolvers)}.", ex);
            }
        }
    }

    internal class CompileOptionsPartResolver : IAssemblyPartResolver
    {
        private readonly HashSet<string> _optionReferences;

        internal CompileOptionsPartResolver(HashSet<string> optionReferences)
        {
            _optionReferences = optionReferences ?? throw new ArgumentNullException(nameof(optionReferences));
        }

        public bool IsFullyResolved(AssemblyPart assemblyPart)
        {
            return false;
        }

        public IEnumerable<string> GetReferencePaths(AssemblyPart assemblyPart)
        {
            if (assemblyPart?.Assembly == null)
                yield break;

            if (_optionReferences.Contains(assemblyPart.Assembly.Location))
                yield return assemblyPart.Assembly.Location;
        }
    }

    internal class CompositeAssemblyPartResolver : IAssemblyPartResolver
    {
        private readonly IAssemblyPartResolver[] _resolvers;

        internal CompositeAssemblyPartResolver(IEnumerable<IAssemblyPartResolver> resolvers)
        {
            _resolvers = resolvers.ToArray();
        }

        public bool IsFullyResolved(AssemblyPart assemblyPart)
        {
            return false;
        }

        public IEnumerable<string> GetReferencePaths(AssemblyPart assemblyPart)
        {
            foreach (var resolver in _resolvers)
            {
                foreach (var referencePath in resolver.GetReferencePaths(assemblyPart))
                {
                    yield return referencePath;
                }

                if(resolver.IsFullyResolved(assemblyPart))
                    yield break;
            }
        }
    }
}
