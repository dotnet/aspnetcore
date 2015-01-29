// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Runtime;
using Moq;
using Xunit;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc.Core
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
                        CreateLibraryInfo("Microsoft.AspNet.Mvc.Core"),
                        CreateLibraryInfo("Microsoft.AspNet.Mvc"),
                        CreateLibraryInfo("Microsoft.AspNet.Mvc.ModelBinding"),
                        CreateLibraryInfo("SomeRandomAssembly"),
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
                  .Returns(Enumerable.Empty<ILibraryInformation>());
            manager.Setup(f => f.GetReferencingLibraries("Microsoft.AspNet.Mvc.Core"))
                   .Returns(new[] { CreateLibraryInfo("Foo") });
            manager.Setup(f => f.GetReferencingLibraries("Microsoft.AspNet.Mvc.ModelBinding"))
                   .Returns(new[] { CreateLibraryInfo("Bar") });
            manager.Setup(f => f.GetReferencingLibraries("Microsoft.AspNet.Mvc"))
                   .Returns(new[] { CreateLibraryInfo("Baz") });
            var provider = new TestAssemblyProvider(manager.Object);

            // Act
            var candidates = provider.GetCandidateLibraries();

            // Assert
            Assert.Equal(new[] { "Baz", "Foo", "Bar" }, candidates.Select(a => a.Name));
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

        private static ILibraryInformation CreateLibraryInfo(string name)
        {
            var info = new Mock<ILibraryInformation>();
            info.SetupGet(b => b.Name).Returns(name);
            return info.Object;
        }

        private static ILibraryManager CreateLibraryManager()
        {
            var manager = new Mock<ILibraryManager>();
            manager.Setup(f => f.GetReferencingLibraries(It.IsAny<string>()))
                  .Returns(Enumerable.Empty<ILibraryInformation>());
            manager.Setup(f => f.GetReferencingLibraries("Microsoft.AspNet.Mvc.Core"))
                   .Returns(new[] { CreateLibraryInfo("Baz") });
            manager.Setup(f => f.GetReferencingLibraries("MyAssembly"))
                   .Returns(new[] { CreateLibraryInfo("Foo") });
            manager.Setup(f => f.GetReferencingLibraries("AnotherAssembly"))
                   .Returns(new[] { CreateLibraryInfo("Bar") });
            return manager.Object;
        }

        private class TestAssemblyProvider : DefaultAssemblyProvider
        {
            public new IEnumerable<ILibraryInformation> GetCandidateLibraries()
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
    }
}