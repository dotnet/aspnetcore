using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Net.Runtime;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    public class DefaultControllerAssemblyProviderTests
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
            var provider = new DefaultControllerAssemblyProvider(manager.Object);

            // Act
            var candidates = provider.GetCandidateLibraries();

            // Assert
            Assert.Equal(new[] { "SomeRandomAssembly" }, candidates.Select(a => a.Name));
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
            var provider = new DefaultControllerAssemblyProvider(manager.Object);

            // Act
            var candidates = provider.GetCandidateLibraries();

            // Assert
            Assert.Equal(new[] { "Baz", "Foo", "Bar" }, candidates.Select(a => a.Name));
        }

        private static ILibraryInformation CreateLibraryInfo(string name)
        {
            var info = new Mock<ILibraryInformation>();
            info.SetupGet(b => b.Name).Returns(name);
            return info.Object;
        }
    }
}