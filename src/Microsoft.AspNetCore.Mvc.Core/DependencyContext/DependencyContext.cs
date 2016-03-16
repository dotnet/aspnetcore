// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    internal class DependencyContext
    {
        private static readonly Lazy<DependencyContext> _defaultContext = new Lazy<DependencyContext>(LoadDefault);

        public DependencyContext(string targetFramework,
            string runtime,
            bool isPortable,
            CompilationOptions compilationOptions,
            IEnumerable<CompilationLibrary> compileLibraries,
            IEnumerable<RuntimeLibrary> runtimeLibraries,
            IEnumerable<RuntimeFallbacks> runtimeGraph)
        {
            if (targetFramework == null)
            {
                throw new ArgumentNullException(nameof(targetFramework));
            }
            if (runtime == null)
            {
                throw new ArgumentNullException(nameof(runtime));
            }
            if (compilationOptions == null)
            {
                throw new ArgumentNullException(nameof(compilationOptions));
            }
            if (compileLibraries == null)
            {
                throw new ArgumentNullException(nameof(compileLibraries));
            }
            if (runtimeLibraries == null)
            {
                throw new ArgumentNullException(nameof(runtimeLibraries));
            }
            if (runtimeGraph == null)
            {
                throw new ArgumentNullException(nameof(runtimeGraph));
            }

            TargetFramework = targetFramework;
            Runtime = runtime;
            IsPortable = isPortable;
            CompilationOptions = compilationOptions;
            CompileLibraries = compileLibraries.ToArray();
            RuntimeLibraries = runtimeLibraries.ToArray();
            RuntimeGraph = runtimeGraph.ToArray();
        }

        public static DependencyContext Default => _defaultContext.Value;

        public string TargetFramework { get; }

        public string Runtime { get; }

        public bool IsPortable { get; }

        public CompilationOptions CompilationOptions { get; }

        public IReadOnlyList<CompilationLibrary> CompileLibraries { get; }

        public IReadOnlyList<RuntimeLibrary> RuntimeLibraries { get; }

        public IReadOnlyList<RuntimeFallbacks> RuntimeGraph { get; }

        public DependencyContext Merge(DependencyContext other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return new DependencyContext(
                TargetFramework,
                Runtime,
                IsPortable,
                CompilationOptions,
                CompileLibraries.Union(other.CompileLibraries, new LibraryMergeEqualityComparer<CompilationLibrary>()),
                RuntimeLibraries.Union(other.RuntimeLibraries, new LibraryMergeEqualityComparer<RuntimeLibrary>()),
                RuntimeGraph.Union(other.RuntimeGraph)
                );
        }

        private static DependencyContext LoadDefault()
        {
            return DependencyContextLoader.Default.Load(Assembly.GetEntryAssembly());
        }

        public static DependencyContext Load(Assembly assembly)
        {
            return DependencyContextLoader.Default.Load(assembly);
        }

        private class LibraryMergeEqualityComparer<T>: IEqualityComparer<T> where T:Library
        {
            public bool Equals(T x, T y)
            {
                return string.Equals(x.Name, y.Name, StringComparison.Ordinal);
            }

            public int GetHashCode(T obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}
