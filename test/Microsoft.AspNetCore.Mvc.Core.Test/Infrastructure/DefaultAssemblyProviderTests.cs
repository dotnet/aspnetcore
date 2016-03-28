// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class DefaultAssemblyProviderTests
    {
        private static readonly Assembly CurrentAssembly = typeof(DefaultAssemblyProviderTests).GetTypeInfo().Assembly;

        [Fact]
        public void GetCandidateLibraries_IgnoresMvcAssemblies()
        {
            // Arrange
            var expected = GetLibrary("SomeRandomAssembly", "Microsoft.AspNetCore.Mvc.Abstractions");
            var dependencyContext = new DependencyContext(
                new TargetInfo("framework", "runtime", "signature", isPortable: true),
                CompilationOptions.Default,
                new CompilationLibrary[0],
                new[]
                {
                     GetLibrary("Microsoft.AspNetCore.Mvc.Core"),
                     GetLibrary("Microsoft.AspNetCore.Mvc"),
                     GetLibrary("Microsoft.AspNetCore.Mvc.Abstractions"),
                     expected,
                },
                Enumerable.Empty<RuntimeFallbacks>());
            var provider = new DefaultAssemblyProvider(CurrentAssembly, dependencyContext);

            // Act
            var candidates = provider.GetCandidateLibraries();

            // Assert
            Assert.Equal(new[] { expected }, candidates);
        }

        [Fact]
        public void CandidateAssemblies_ReturnsEntryAssemblyIfDependencyContextIsNull()
        {
            // Arrange
            var provider = new DefaultAssemblyProvider(CurrentAssembly, dependencyContext: null);

            // Act
            var candidates = provider.CandidateAssemblies;

            // Assert
            Assert.Equal(new[] { CurrentAssembly }, candidates);
        }

        [Fact]
        public void GetCandidateLibraries_ReturnsLibrariesReferencingAnyMvcAssembly()
        {
            // Arrange
            var dependencyContext = new DependencyContext(
                new TargetInfo("framework", "runtime", "signature", isPortable: true),
                CompilationOptions.Default,
                new CompilationLibrary[0],
                new[]
                {
                     GetLibrary("Foo", "Microsoft.AspNetCore.Mvc.Core"),
                     GetLibrary("Bar", "Microsoft.AspNetCore.Mvc"),
                     GetLibrary("Qux", "Not.Mvc.Assembly", "Unofficial.Microsoft.AspNetCore.Mvc"),
                     GetLibrary("Baz", "Microsoft.AspNetCore.Mvc.Abstractions"),
                },
                Enumerable.Empty<RuntimeFallbacks>());
            var provider = new DefaultAssemblyProvider(CurrentAssembly, dependencyContext);

            // Act
            var candidates = provider.GetCandidateLibraries();

            // Assert
            Assert.Equal(new[] { "Foo", "Bar", "Baz" }, candidates.Select(a => a.Name));
        }

        [Fact]
        public void GetCandidateLibraries_ReturnsLibrariesReferencingOverriddenAssemblies()
        {
            // Arrange
            var dependencyContext = new DependencyContext(
                new TargetInfo("framework", "runtime", "signature", isPortable: true),
                CompilationOptions.Default,
                new CompilationLibrary[0],
                new[]
                {
                     GetLibrary("Foo", "CustomMvc.Modules"),
                     GetLibrary("Bar", "CustomMvc.Application.Loader"),
                     GetLibrary("Baz", "Microsoft.AspNetCore.Mvc.Abstractions"),
                },
                Enumerable.Empty<RuntimeFallbacks>());
            var referenceAssemblies = new HashSet<string>
            {
                "CustomMvc.Modules",
                "CustomMvc.Application.Loader"
            };
            var assemblyProvider = new OverridenAssemblyProvider(
                CurrentAssembly,
                dependencyContext,
                referenceAssemblies);

            // Act
            var candidates = assemblyProvider.GetCandidateLibraries();

            // Assert
            Assert.Equal(new[] { "Foo", "Bar" }, candidates.Select(a => a.Name));
        }

        [Fact]
        public void GetCandidateLibraries_ReturnsEmptySequenceWhenReferenceAssembliesIsNull()
        {
            // Arrange
            var dependencyContext = new DependencyContext(
               new TargetInfo("framework", "runtime", "signature", isPortable: true),
               CompilationOptions.Default,
               new CompilationLibrary[0],
               new[]
               {
                     GetLibrary("Foo", "CustomMvc.Modules"),
                     GetLibrary("Bar", "CustomMvc.Application.Loader"),
                     GetLibrary("Baz", "Microsoft.AspNetCore.Mvc.Abstractions"),
               },
               Enumerable.Empty<RuntimeFallbacks>());
            var assemblyProvider = new OverridenAssemblyProvider(
                CurrentAssembly,
                dependencyContext,
                referenceAssemblies: null);

            // Act
            var candidates = assemblyProvider.GetCandidateLibraries();

            // Assert
            Assert.Empty(candidates);
        }

        // This test verifies DefaultAssemblyProvider.ReferenceAssemblies reflects the actual loadable assemblies
        // of the libraries that Microsoft.AspNetCore.Mvc dependes on.
        // If we add or remove dependencies, this test should be changed together.
        [Fact]
        public void ReferenceAssemblies_ReturnsLoadableReferenceAssemblies()
        {
            // Arrange
            var provider = new TestableAssemblyProvider(CurrentAssembly, dependencyContext: null);
            var excludeAssemblies = new string[]
            {
                "Microsoft.AspNetCore.Mvc.WebApiCompatShim",
                "Microsoft.AspNetCore.Mvc.TestCommon",
                "Microsoft.AspNetCore.Mvc.Core.Test",
                "Microsoft.AspNetCore.Mvc.TestDiagnosticListener.Sources",
            };

            var additionalAssemblies = new[]
            {
                // The following assemblies are not reachable from Microsoft.AspNetCore.Mvc
                "Microsoft.AspNetCore.Mvc.TagHelpers",
                "Microsoft.AspNetCore.Mvc.Formatters.Xml",
            };

            var expected = DependencyContext.Load(CurrentAssembly)
                .RuntimeLibraries
                .Where(r => r.Name.StartsWith("Microsoft.AspNetCore.Mvc", StringComparison.Ordinal) &&
                    !excludeAssemblies.Contains(r.Name, StringComparer.OrdinalIgnoreCase))
                .Select(r => r.Name)
                .Concat(additionalAssemblies)
                .Distinct()
                .OrderBy(p => p, StringComparer.Ordinal);

            // Act
            var referenceAssemblies = provider.ReferenceAssemblies.OrderBy(p => p, StringComparer.Ordinal);

            // Assert
            Assert.Equal(expected, referenceAssemblies);
        }

        private static RuntimeLibrary GetLibrary(string name, params string[] dependencyNames)
        {
            var dependencies = dependencyNames?.Select(d => new Dependency(d, "42.0.0")) ?? new Dependency[0];

            return new RuntimeLibrary(
                "package",
                name,
                "23.0.0",
                "hash",
                new RuntimeAssetGroup[0],
                new RuntimeAssetGroup[0],
                new ResourceAssembly[0],
                dependencies: dependencies.ToArray(),
                serviceable: true);
        }

        private class OverridenAssemblyProvider : DefaultAssemblyProvider
        {
            public OverridenAssemblyProvider(
                Assembly entryAssembly,
                DependencyContext dependencyContext,
                HashSet<string> referenceAssemblies)
                : base(entryAssembly, dependencyContext)
            {
                ReferenceAssemblies = referenceAssemblies;
            }

            protected override HashSet<string> ReferenceAssemblies { get; }
        }

        private class TestableAssemblyProvider : DefaultAssemblyProvider
        {
            public TestableAssemblyProvider(
                Assembly entryAssembly,
                DependencyContext dependencyContext)
                : base(entryAssembly, dependencyContext)
            {
            }

            public new HashSet<string> ReferenceAssemblies => base.ReferenceAssemblies;
        }
    }
}