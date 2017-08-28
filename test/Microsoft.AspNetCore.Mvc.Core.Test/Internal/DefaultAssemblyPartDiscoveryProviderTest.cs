// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class DefaultAssemblyPartDiscoveryProviderTests
    {
        private static readonly Assembly CurrentAssembly =
            typeof(DefaultAssemblyPartDiscoveryProviderTests).GetTypeInfo().Assembly;

        [Fact]
        public void CandidateResolver_ThrowsIfDependencyContextContainsDuplicateRuntimeLibraryNames()
        {
            // Arrange
            var upperCaseLibrary = "Microsoft.AspNetCore.Mvc";
            var mixedCaseLibrary = "microsoft.aspNetCore.mvc";

            var dependencyContext = new DependencyContext(
                new TargetInfo("framework", "runtime", "signature", isPortable: true),
                CompilationOptions.Default,
                new CompilationLibrary[0],
                new[]
                {
                     GetLibrary(mixedCaseLibrary),
                     GetLibrary(upperCaseLibrary),
                },
                Enumerable.Empty<RuntimeFallbacks>());

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => DefaultAssemblyPartDiscoveryProvider.GetCandidateLibraries(dependencyContext));

            // Assert
            Assert.Equal($"A duplicate entry for library reference {upperCaseLibrary} was found. Please check that all package references in all projects use the same casing for the same package references.", exception.Message);
        }

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

            // Act
            var candidates = DefaultAssemblyPartDiscoveryProvider.GetCandidateLibraries(dependencyContext);

            // Assert
            Assert.Equal(new[] { expected }, candidates);
        }

        [Fact]
        public void GetCandidateLibraries_DoesNotThrow_IfLibraryDoesNotHaveRuntimeComponent()
        {
            // Arrange
            var expected = GetLibrary("MyApplication", "Microsoft.AspNetCore.Server.Kestrel", "Microsoft.AspNetCore.Mvc");
            var deps = new DependencyContext(
                new TargetInfo("netcoreapp2.0", "rurntime", "signature", isPortable: true),
                CompilationOptions.Default,
                Enumerable.Empty<CompilationLibrary>(),
                new[]
                {
                    expected,
                    GetLibrary("Microsoft.AspNetCore.Server.Kestrel", "Libuv"),
                    GetLibrary("Microsoft.AspNetCore.Mvc"),
                },
                Enumerable.Empty<RuntimeFallbacks>());

            // Act
            var candidates = DefaultAssemblyPartDiscoveryProvider.GetCandidateLibraries(deps).ToList();

            // Assert
            Assert.Equal(new[] { expected }, candidates);
        }

        [Fact]
        public void CandidateAssemblies_ReturnsEntryAssemblyIfDependencyContextIsNull()
        {
            // Arrange & Act
            var candidates = DefaultAssemblyPartDiscoveryProvider.GetCandidateAssemblies(CurrentAssembly, dependencyContext: null);

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
                     GetLibrary("Microsoft.AspNetCore.Mvc.Core"),
                     GetLibrary("Microsoft.AspNetCore.Mvc"),
                     GetLibrary("Not.Mvc.Assembly"),
                     GetLibrary("Unofficial.Microsoft.AspNetCore.Mvc"),
                     GetLibrary("Microsoft.AspNetCore.Mvc.Abstractions"),

                },
                Enumerable.Empty<RuntimeFallbacks>());

            // Act
            var candidates = DefaultAssemblyPartDiscoveryProvider.GetCandidateLibraries(dependencyContext);

            // Assert
            Assert.Equal(new[] { "Foo", "Bar", "Baz" }, candidates.Select(a => a.Name));
        }

        [Fact]
        public void GetCandidateLibraries_LibraryNameComparisonsAreCaseInsensitive()
        {
            // Arrange
            var dependencyContext = new DependencyContext(
                new TargetInfo("framework", "runtime", "signature", isPortable: true),
                CompilationOptions.Default,
                new CompilationLibrary[0],
                new[]
                {
                     GetLibrary("Foo", "MICROSOFT.ASPNETCORE.MVC.CORE"),
                     GetLibrary("Bar", "microsoft.aspnetcore.mvc"),
                     GetLibrary("Qux", "Not.Mvc.Assembly", "Unofficial.Microsoft.AspNetCore.Mvc"),
                     GetLibrary("Baz", "mIcRoSoFt.AsPnEtCoRe.MvC.aBsTrAcTiOnS"),
                     GetLibrary("Microsoft.AspNetCore.Mvc.Core"),
                     GetLibrary("LibraryA", "LIBRARYB"),
                     GetLibrary("LibraryB", "microsoft.aspnetcore.mvc"),
                     GetLibrary("Microsoft.AspNetCore.Mvc"),
                     GetLibrary("Not.Mvc.Assembly"),
                     GetLibrary("Unofficial.Microsoft.AspNetCore.Mvc"),
                     GetLibrary("Microsoft.AspNetCore.Mvc.Abstractions"),
                },
                Enumerable.Empty<RuntimeFallbacks>());

            // Act
            var candidates = DefaultAssemblyPartDiscoveryProvider.GetCandidateLibraries(dependencyContext);

            // Assert
            Assert.Equal(new[] { "Foo", "Bar", "Baz", "LibraryA", "LibraryB" }, candidates.Select(a => a.Name));
        }

        [Fact]
        public void GetCandidateLibraries_ReturnsLibrariesWithTransitiveReferencesToAnyMvcAssembly()
        {
            // Arrange
            var expectedLibraries = new[] { "Foo", "Bar", "Baz", "LibraryA", "LibraryB", "LibraryC", "LibraryE", "LibraryG", "LibraryH" };

            var dependencyContext = new DependencyContext(
                new TargetInfo("framework", "runtime", "signature", isPortable: true),
                CompilationOptions.Default,
                new CompilationLibrary[0],
                new[]
                {
                     GetLibrary("Foo", "Bar"),
                     GetLibrary("Bar", "Microsoft.AspNetCore.Mvc"),
                     GetLibrary("Qux", "Not.Mvc.Assembly", "Unofficial.Microsoft.AspNetCore.Mvc"),
                     GetLibrary("Baz", "Microsoft.AspNetCore.Mvc.Abstractions"),
                     GetLibrary("Microsoft.AspNetCore.Mvc"),
                     GetLibrary("Not.Mvc.Assembly"),
                     GetLibrary("Microsoft.AspNetCore.Mvc.Abstractions"),
                     GetLibrary("Unofficial.Microsoft.AspNetCore.Mvc"),
                     GetLibrary("LibraryA", "LibraryB"),
                     GetLibrary("LibraryB","LibraryC"),
                     GetLibrary("LibraryC", "LibraryD", "Microsoft.AspNetCore.Mvc.Abstractions"),
                     GetLibrary("LibraryD"),
                     GetLibrary("LibraryE","LibraryF","LibraryG"),
                     GetLibrary("LibraryF"),
                     GetLibrary("LibraryG", "LibraryH"),
                     GetLibrary("LibraryH", "LibraryI", "Microsoft.AspNetCore.Mvc"),
                     GetLibrary("LibraryI")
                },
                Enumerable.Empty<RuntimeFallbacks>());

            // Act
            var candidates = DefaultAssemblyPartDiscoveryProvider.GetCandidateLibraries(dependencyContext);

            // Assert
            Assert.Equal(expectedLibraries, candidates.Select(a => a.Name));
        }

        [Fact]
        public void GetCandidateLibraries_SkipsMvcAssemblies()
        {
            // Arrange
            var dependencyContext = new DependencyContext(
                new TargetInfo("framework", "runtime", "signature", isPortable: true),
                CompilationOptions.Default,
                new CompilationLibrary[0],
                new[]
                {
                     GetLibrary("MvcSandbox", "Microsoft.AspNetCore.Mvc.Core", "Microsoft.AspNetCore.Mvc"),
                     GetLibrary("Microsoft.AspNetCore.Mvc.Core", "Microsoft.AspNetCore.HttpAbstractions"),
                     GetLibrary("Microsoft.AspNetCore.HttpAbstractions"),
                     GetLibrary("Microsoft.AspNetCore.Mvc", "Microsoft.AspNetCore.Mvc.Abstractions", "Microsoft.AspNetCore.Mvc.Core"),
                     GetLibrary("Microsoft.AspNetCore.Mvc.Abstractions"),
                     GetLibrary("Microsoft.AspNetCore.Mvc.TagHelpers", "Microsoft.AspNetCore.Mvc.Razor"),
                     GetLibrary("Microsoft.AspNetCore.Mvc.Razor"),
                     GetLibrary("ControllersAssembly", "Microsoft.AspNetCore.Mvc"),
                },
                Enumerable.Empty<RuntimeFallbacks>());

            // Act
            var candidates = DefaultAssemblyPartDiscoveryProvider.GetCandidateLibraries(dependencyContext);

            // Assert
            Assert.Equal(new[] { "MvcSandbox", "ControllersAssembly" }, candidates.Select(a => a.Name));
        }

        // This test verifies DefaultAssemblyPartDiscoveryProvider.ReferenceAssemblies reflects the actual loadable assemblies
        // of the libraries that Microsoft.AspNetCore.Mvc depends on.
        // If we add or remove dependencies, this test should be changed together.
        [Fact]
        public void ReferenceAssemblies_ReturnsLoadableReferenceAssemblies()
        {
            // Arrange
            var excludeAssemblies = new string[]
            {
                "Microsoft.AspNetCore.Mvc.Core.Test",
                "Microsoft.AspNetCore.Mvc.TestCommon",
                "Microsoft.AspNetCore.Mvc.TestDiagnosticListener",
                "Microsoft.AspNetCore.Mvc.WebApiCompatShim",
            };

            var additionalAssemblies = new[]
            {
                // The following assemblies are not reachable from Microsoft.AspNetCore.Mvc
                "Microsoft.AspNetCore.Mvc.Formatters.Xml",
            };

            var dependencyContextLibraries = DependencyContext.Load(CurrentAssembly)
                .RuntimeLibraries
                .Where(r => r.Name.StartsWith("Microsoft.AspNetCore.Mvc", StringComparison.OrdinalIgnoreCase) &&
                    !excludeAssemblies.Contains(r.Name, StringComparer.OrdinalIgnoreCase))
                .Select(r => r.Name);

            var expected = dependencyContextLibraries
                .Concat(additionalAssemblies)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

            // Act
            var referenceAssemblies = DefaultAssemblyPartDiscoveryProvider
                .ReferenceAssemblies
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

            // Assert
            Assert.Equal(expected, referenceAssemblies, StringComparer.OrdinalIgnoreCase);
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
    }
}
