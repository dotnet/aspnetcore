// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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

        [Theory]
        [MemberData(nameof(ResolveAdditionalReferencesData))]
        public void ResolveAdditionalReferences_DiscoversAdditionalReferences(ResolveAdditionalReferencesTestData testData)
        {
            // Arrange
            var resolver = testData.AssemblyResolver;
            DefaultAssemblyPartDiscoveryProvider.AssemblyResolver = path => resolver.ContainsKey(path);
            DefaultAssemblyPartDiscoveryProvider.AssemblyLoader = path => resolver.TryGetValue(path, out var result) ? result : null;

            // Arrange & Act
            var (additionalReferences, entryAssemblyAdditionalReferences) =
                DefaultAssemblyPartDiscoveryProvider.ResolveAdditionalReferences(testData.EntryAssembly, testData.CandidateAssemblies);

            var additionalRefs = additionalReferences.Select(a => a.FullName).OrderBy(id => id).ToArray();
            var entryAssemblyAdditionalRefs = entryAssemblyAdditionalReferences.Select(a => a.FullName).OrderBy(id => id).ToArray();

            // Assert
            Assert.Equal(testData.ExpectedAdditionalReferences, additionalRefs);
            Assert.Equal(testData.ExpectedEntryAssemblyAdditionalReferences, entryAssemblyAdditionalRefs);
        }

        public class ResolveAdditionalReferencesTestData
        {
            public Assembly EntryAssembly { get; set; }
            public SortedSet<Assembly> CandidateAssemblies { get; set; }
            public IDictionary<string, Assembly> AssemblyResolver { get; set; }
            public string[] ExpectedAdditionalReferences { get; set; }
            public string[] ExpectedEntryAssemblyAdditionalReferences { get; set; }
        }

        public static TheoryData<ResolveAdditionalReferencesTestData> ResolveAdditionalReferencesData
        {
            get
            {
                var data = new TheoryData<ResolveAdditionalReferencesTestData>();
                var noCandidates = Array.Empty<Assembly>();
                var noResolvable = new Dictionary<string, Assembly>();
                var noAdditionalReferences = new string[] { };

                // Single assembly app no precompilation
                var aAssembly = new DiscoveryTestAssembly("A");
                var singleAppNoPrecompilation = new ResolveAdditionalReferencesTestData
                {
                    EntryAssembly = aAssembly,
                    CandidateAssemblies = CreateCandidates(aAssembly),
                    AssemblyResolver = noResolvable,
                    ExpectedAdditionalReferences = Array.Empty<string>(),
                    ExpectedEntryAssemblyAdditionalReferences = Array.Empty<string>()
                };
                data.Add(singleAppNoPrecompilation);

                // Single assembly app with old precompilation not included in the graph
                var bAssembly = new DiscoveryTestAssembly("B");
                var (bPath, bPrecompiledViews) = CreateResolvablePrecompiledViewsAssembly("B");
                var singleAssemblyPrecompilationNotInGraph = new ResolveAdditionalReferencesTestData
                {
                    EntryAssembly = bAssembly,
                    CandidateAssemblies = CreateCandidates(bAssembly),
                    AssemblyResolver = new Dictionary<string, Assembly> { [bPath] = bPrecompiledViews },
                    ExpectedAdditionalReferences = noAdditionalReferences,
                    ExpectedEntryAssemblyAdditionalReferences = new[] { bPrecompiledViews.FullName }
                };
                data.Add(singleAssemblyPrecompilationNotInGraph);

                //// Single assembly app with new precompilation not included in the graph
                var cAssembly = new DiscoveryTestAssembly(
                    "C",
                    DiscoveryTestAssembly.DefaultLocationBase,
                    new[] { ("C.PrecompiledViews.dll", true) });
                var (cPath, cPrecompiledViews) = CreateResolvablePrecompiledViewsAssembly("C");
                var singleAssemblyNewPrecompilationNotInGraph = new ResolveAdditionalReferencesTestData
                {
                    EntryAssembly = cAssembly,
                    CandidateAssemblies = CreateCandidates(cAssembly),
                    AssemblyResolver = new Dictionary<string, Assembly> { [cPath] = cPrecompiledViews },
                    ExpectedAdditionalReferences = noAdditionalReferences,
                    ExpectedEntryAssemblyAdditionalReferences = new[] { cPrecompiledViews.FullName }
                };
                data.Add(singleAssemblyNewPrecompilationNotInGraph);

                //// Single assembly app with new precompilation included in the graph
                var dAssembly = new DiscoveryTestAssembly(
                    "D",
                    DiscoveryTestAssembly.DefaultLocationBase,
                    new[] { (Path.Combine(DiscoveryTestAssembly.DefaultLocationBase, "subfolder", "D.PrecompiledViews.dll"), true) });
                var (dPath, dPrecompiledViews) = CreateResolvablePrecompiledViewsAssembly("D");
                var singleAssemblyNewPrecompilationInGraph = new ResolveAdditionalReferencesTestData
                {
                    EntryAssembly = dAssembly,
                    CandidateAssemblies = CreateCandidates(dAssembly, dPrecompiledViews),
                    AssemblyResolver = new Dictionary<string, Assembly> { [dPath] = dPrecompiledViews },
                    ExpectedAdditionalReferences = noAdditionalReferences,
                    ExpectedEntryAssemblyAdditionalReferences = new[] { dPrecompiledViews.FullName }
                };
                data.Add(singleAssemblyNewPrecompilationInGraph);

                //// Single assembly app with new precompilation included in the graph optional part
                var hAssembly = new DiscoveryTestAssembly(
                    "h",
                    DiscoveryTestAssembly.DefaultLocationBase,
                    new[] { ("H.PrecompiledViews.dll", false) });
                var (hPath, hPrecompiledViews) = CreateResolvablePrecompiledViewsAssembly("H");
                var singleAssemblyNewPrecompilationInGraphOptionalDependency = new ResolveAdditionalReferencesTestData
                {
                    EntryAssembly = hAssembly,
                    CandidateAssemblies = CreateCandidates(hAssembly, hPrecompiledViews),
                    AssemblyResolver = new Dictionary<string, Assembly> { [hPath] = hPrecompiledViews },
                    ExpectedAdditionalReferences = noAdditionalReferences,
                    ExpectedEntryAssemblyAdditionalReferences = noAdditionalReferences
                };
                data.Add(singleAssemblyNewPrecompilationInGraphOptionalDependency);

                //// Entry assembly with two dependencies app with new precompilation included in the graph
                var eAssembly = new DiscoveryTestAssembly(
                    "E",
                    DiscoveryTestAssembly.DefaultLocationBase,
                    new[] { ("E.PrecompiledViews.dll", true) });
                var (ePath, ePrecompiledViews) = CreateResolvablePrecompiledViewsAssembly("E");

                var fAssembly = new DiscoveryTestAssembly(
                    "F",
                    DiscoveryTestAssembly.DefaultLocationBase,
                    new[] { ("F.PrecompiledViews.dll", true) });
                    var (fPath, fPrecompiledViews) = CreateResolvablePrecompiledViewsAssembly("F");

                var gAssembly = new DiscoveryTestAssembly(
                    "G",
                    DiscoveryTestAssembly.DefaultLocationBase,
                    new[] { (Path.Combine(DiscoveryTestAssembly.DefaultLocationBase, "subfolder", "G.PrecompiledViews.dll"), true) });

                var (gPath, gPrecompiledViews) = CreateResolvablePrecompiledViewsAssembly("G");
                var multipleAssembliesNewPrecompilationInGraph = new ResolveAdditionalReferencesTestData
                {
                    EntryAssembly = gAssembly,
                    CandidateAssemblies = CreateCandidates(
                        fAssembly,
                        fPrecompiledViews,
                        gAssembly,
                        gPrecompiledViews,
                        eAssembly,
                        ePrecompiledViews),
                    AssemblyResolver = new Dictionary<string, Assembly> {
                        [ePath] = ePrecompiledViews,
                        [fPath] = fPrecompiledViews,
                        [gPath] = gPrecompiledViews
                    },
                    ExpectedAdditionalReferences = new[] { ePrecompiledViews.FullName, fPrecompiledViews.FullName },
                    ExpectedEntryAssemblyAdditionalReferences = new[] {
                        gPrecompiledViews.FullName
                    }
                };
                data.Add(multipleAssembliesNewPrecompilationInGraph);

                return data;
            }
        }

        private static SortedSet<Assembly> CreateCandidates(params Assembly[] assemblies) =>
            new SortedSet<Assembly>(assemblies, DefaultAssemblyPartDiscoveryProvider.FullNameAssemblyComparer.Instance);

        private static (string, Assembly) CreateResolvablePrecompiledViewsAssembly(string name, string path = null) =>
            (path ?? Path.Combine(DiscoveryTestAssembly.DefaultLocationBase, $"{name}.PrecompiledViews.dll"),
                new DiscoveryTestAssembly($"{name}.PrecompiledViews"));

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
                "Microsoft.AspnetCore.All",
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

        private class DiscoveryTestAssembly : Assembly
        {
            private readonly string _fullName;
            private readonly string _location;
            private readonly Attribute[] _additionalDependencies;
            public static readonly string DefaultLocationBase =
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"c:\app\" : "/app/";

            public DiscoveryTestAssembly(string fullName, string location = null)
                : this(
                      fullName,
                      location ?? Path.Combine(DefaultLocationBase, new AssemblyName(fullName).Name + ".dll"),
                      Array.Empty<(string, bool)>())
            { }

            public DiscoveryTestAssembly(string fullName, string location, IEnumerable<(string, bool)> additionalDependencies)
            {
                _fullName = fullName;
                _location = location;
                _additionalDependencies = additionalDependencies
                    .Select(ad => new AssemblyMetadataAttribute(
                        "Microsoft.AspNetCore.Mvc.AdditionalReference",
                        $"{ad.Item1},{ad.Item2}")).ToArray();
            }

            public override string FullName => _fullName;

            public override string Location => _location;

            public override object[] GetCustomAttributes(bool inherit) => _additionalDependencies;

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                var attributes = _additionalDependencies
                    .Where(t => t.GetType().IsAssignableFrom(attributeType))
                    .ToArray();

                var result = Array.CreateInstance(attributeType, attributes.Length);
                attributes.CopyTo(result, 0);
                return (object[])result;
            }

            public override AssemblyName GetName(bool copiedName) => new AssemblyName(FullName);

            public override AssemblyName GetName() => new AssemblyName(FullName);
        }
    }
}
