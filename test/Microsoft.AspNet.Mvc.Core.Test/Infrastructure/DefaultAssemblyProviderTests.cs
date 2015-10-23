// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Infrastructure
{
    public class DefaultAssemblyProviderTests
    {
        [Fact]
        public void CandidateAssemblies_IgnoresMvcAssemblies()
        {
            // Arrange
            var manager = new Mock<ILibraryManager>();
            manager.Setup(f => f.GetReferencingLibraries(It.IsAny<string>()))
                   .Returns(new[]
                   {
                        new Library("Microsoft.AspNet.Mvc.Core"),
                        new Library("Microsoft.AspNet.Mvc"),
                        new Library("Microsoft.AspNet.Mvc.Abstractions"),
                        new Library("SomeRandomAssembly"),
                   })
                   .Verifiable();
            var provider = new TestAssemblyProvider(manager.Object);

            // Act
            var candidates = provider.GetCandidateLibraries();

            // Assert
            Assert.Equal(new[] { "SomeRandomAssembly" }, candidates.Select(a => a.Name));

            var context = new Mock<HttpContext>();
        }

        [Fact]
        public void CandidateAssemblies_ReturnsLibrariesReferencingAnyMvcAssembly()
        {
            // Arrange
            var manager = new Mock<ILibraryManager>();
            manager.Setup(f => f.GetReferencingLibraries(It.IsAny<string>()))
                  .Returns(Enumerable.Empty<Library>());
            manager.Setup(f => f.GetReferencingLibraries("Microsoft.AspNet.Mvc.Core"))
                   .Returns(new[] { new Library("Foo") });
            manager.Setup(f => f.GetReferencingLibraries("Microsoft.AspNet.Mvc.Abstractions"))
                   .Returns(new[] { new Library("Bar") });
            manager.Setup(f => f.GetReferencingLibraries("Microsoft.AspNet.Mvc"))
                   .Returns(new[] { new Library("Baz") });
            var provider = new TestAssemblyProvider(manager.Object);

            // Act
            var candidates = provider.GetCandidateLibraries();

            // Assert
            Assert.Equal(new[] { "Baz", "Bar", "Foo" }, candidates.Select(a => a.Name));
        }

        [Fact]
        public void CandidateAssemblies_ReturnsLibrariesReferencingDefaultAssemblies()
        {
            // Arrange
            var defaultProvider = new TestAssemblyProvider(CreateLibraryManager());

            // Act
            var defaultProviderCandidates = defaultProvider.GetCandidateLibraries();

            // Assert
            Assert.Equal(new[] { "Baz" }, defaultProviderCandidates.Select(a => a.Name));
        }

        [Fact]
        public void CandidateAssemblies_ReturnsLibrariesReferencingOverriddenAssemblies()
        {
            // Arrange
            var overriddenProvider = new OverriddenAssemblyProvider(CreateLibraryManager());

            // Act
            var overriddenProviderCandidates = overriddenProvider.GetCandidateLibraries();

            // Assert
            Assert.Equal(new[] { "Foo", "Bar" }, overriddenProviderCandidates.Select(a => a.Name));
        }

        [Fact]
        public void CandidateAssemblies_ReturnsEmptySequenceWhenReferenceAssembliesIsNull()
        {
            // Arrange
            var nullProvider = new NullAssemblyProvider(CreateLibraryManager());

            // Act
            var nullProviderCandidates = nullProvider.GetCandidateLibraries();

            // Assert
            Assert.Empty(nullProviderCandidates.Select(a => a.Name));
        }

        // This test verifies DefaultAssemblyProvider.ReferenceAssemblies reflects the actual loadable assemblies
        // of the libraries that Microsoft.AspNet.Mvc dependes on.
        // If we add or remove dependencies, this test should be changed together.
        [Fact]
        public void ReferenceAssemblies_ReturnsLoadableReferenceAssemblies()
        {
            // Arrange
            var provider = new MvcAssembliesTestingProvider();

            var expected = provider.LoadableReferenceAssemblies;

            // Act
            var referenceAssemblies = provider.ReferenceAssemblies;

            // Assert
            Assert.True(expected.SetEquals(referenceAssemblies));
        }

        private static ILibraryManager CreateLibraryManager()
        {
            var manager = new Mock<ILibraryManager>();
            manager.Setup(f => f.GetReferencingLibraries(It.IsAny<string>()))
                  .Returns(Enumerable.Empty<Library>());
            manager.Setup(f => f.GetReferencingLibraries("Microsoft.AspNet.Mvc.Core"))
                   .Returns(new[] { new Library("Baz") });
            manager.Setup(f => f.GetReferencingLibraries("MyAssembly"))
                   .Returns(new[] { new Library("Foo") });
            manager.Setup(f => f.GetReferencingLibraries("AnotherAssembly"))
                   .Returns(new[] { new Library("Bar") });
            return manager.Object;
        }

        private class TestAssemblyProvider : DefaultAssemblyProvider
        {
            public new IEnumerable<Library> GetCandidateLibraries()
            {
                return base.GetCandidateLibraries();
            }

            public TestAssemblyProvider(ILibraryManager libraryManager) : base(libraryManager)
            {
            }
        }

        private class OverriddenAssemblyProvider : TestAssemblyProvider
        {
            protected override HashSet<string> ReferenceAssemblies
            {
                get
                {
                    return new HashSet<string>
                    {
                        "MyAssembly",
                        "AnotherAssembly"
                    };
                }
            }

            public OverriddenAssemblyProvider(ILibraryManager libraryManager) : base(libraryManager)
            {
            }
        }

        private class NullAssemblyProvider : TestAssemblyProvider
        {
            protected override HashSet<string> ReferenceAssemblies
            {
                get
                {
                    return null;
                }
            }

            public NullAssemblyProvider(ILibraryManager libraryManager) : base(libraryManager)
            {
            }
        }

        private class MvcAssembliesTestingProvider : DefaultAssemblyProvider
        {
            private static readonly ILibraryManager _libraryManager = GetLibraryManager();
            private static readonly string _mvcName = "Microsoft.AspNet.Mvc";

            public MvcAssembliesTestingProvider() : base(_libraryManager)
            { }

            public HashSet<string> LoadableReferenceAssemblies
            {
                get
                {
                    var dependencies = new HashSet<string>() { _mvcName };
                    GetAllDependencies(_mvcName, dependencies);

                    return new HashSet<string>(
                        SelectMvcAssemblies(
                            GetAssemblies(dependencies)));
                }
            }

            public new HashSet<string> ReferenceAssemblies
            {
                get
                {
                    return base.ReferenceAssemblies;
                }
            }

            private static void GetAllDependencies(string libraryName, HashSet<string> dependencies)
            {
                var directDependencies = _libraryManager.GetLibrary(libraryName).Dependencies;

                if (directDependencies != null)
                {
                    foreach (var dependency in directDependencies)
                    {
                        GetAllDependencies(dependency, dependencies);
                        dependencies.Add(dependency);
                    }
                }
            }

            private static IEnumerable<string> SelectMvcAssemblies(IEnumerable<string> assemblies)
            {
                var exceptionalAssebmlies = new string[]
                {
                    "Microsoft.AspNet.Mvc.WebApiCompatShim",
                };

                var mvcAssemblies = assemblies
                    .Distinct()
                    .Where(n => n.StartsWith(_mvcName))
                    .Except(exceptionalAssebmlies)
                    .ToList();

                // The following assemblies are not reachable from Microsoft.AspNet.Mvc
                mvcAssemblies.Add("Microsoft.AspNet.Mvc.TagHelpers");
                mvcAssemblies.Add("Microsoft.AspNet.Mvc.Formatters.Xml");
                mvcAssemblies.Add("Microsoft.AspNet.PageExecutionInstrumentation.Interfaces");

                return mvcAssemblies;
            }

            private static IEnumerable<string> GetAssemblies(IEnumerable<string> libraries)
            {
                return libraries
                    .Select(n => _libraryManager.GetLibrary(n))
                    .SelectMany(n => n.Assemblies)
                    .Distinct()
                    .Select(n => n.Name);
            }

            private static ILibraryManager GetLibraryManager()
            {
                return PlatformServices.Default.LibraryManager;
            }
        }
    }
}