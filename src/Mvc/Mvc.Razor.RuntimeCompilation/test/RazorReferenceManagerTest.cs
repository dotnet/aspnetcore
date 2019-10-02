// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
{
    public class RazorReferenceManagerTest
    {
        private static readonly string ApplicationPartReferencePath = "some-path";

        [Fact]
        public void GetCompilationReferences_CombinesApplicationPartAndOptionMetadataReferences()
        {
            // Arrange
            var options = new MvcRazorRuntimeCompilationOptions();
            var additionalReferencePath = "additional-path";
            options.AdditionalReferencePaths.Add(additionalReferencePath);

            var applicationPartManager = GetApplicationPartManager(ApplicationPartReferencePath);
            var referenceManager = new RazorReferenceManager(
                applicationPartManager,
                Options.Create(options));

            var expected = new[] { ApplicationPartReferencePath, additionalReferencePath };

            // Act
            var references = referenceManager.GetReferencePaths();

            // Assert
            Assert.Equal(expected, references);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetCompilationReferences_VerifyThatResolutionCanBeAborted(bool firstFullyResolves)
        {
            // Arrange
            var assembly = typeof(RazorReferenceManager).Assembly;
            var assemblyPart = new AssemblyPart(assembly);
            var partResolver1 = new Mock<IAssemblyPartResolver>();
            var partResolver2 = new Mock<IAssemblyPartResolver>();
            
            partResolver1
                .Setup(resolver => resolver.GetReferencePaths(assemblyPart))
                .Returns(new[] { assembly.Location });
            partResolver1
                .Setup(resolver => resolver.IsFullyResolved(assemblyPart))
                .Returns(firstFullyResolves);
            partResolver2
                .Setup(resolver => resolver.GetReferencePaths(assemblyPart))
                .Returns(new[] { assembly.Location });
            partResolver2
                .Setup(resolver => resolver.IsFullyResolved(assemblyPart))
                .Returns(false);
            
            var options = new MvcRazorRuntimeCompilationOptions();
            options.AssemblyPartResolvers.Add(partResolver1.Object);
            options.AssemblyPartResolvers.Add(partResolver2.Object);

            var applicationPartManager = GetApplicationPartManager(assemblyPart);
            var referenceManager = new RazorReferenceManager(
                applicationPartManager,
                Options.Create(options));
            
            // Act
            var references = referenceManager.GetReferencePaths();

            // Assert
            Assert.Contains(assemblyPart.Assembly.Location, references);
            if (firstFullyResolves)
            {
                partResolver2.Verify(d => d.GetReferencePaths(assemblyPart), Times.Never);
            }
            else
            {
                partResolver2.Verify(d => d.GetReferencePaths(assemblyPart), Times.Once);
            }
        }

        private static ApplicationPartManager GetApplicationPartManager(string referencePath)
        {
            var applicationPartManager = new ApplicationPartManager();
            var part = new Mock<ApplicationPart>();

            part.As<ICompilationReferencesProvider>()
                .Setup(p => p.GetReferencePaths())
                .Returns(new[] { referencePath });

            applicationPartManager.ApplicationParts.Add(part.Object);

            return applicationPartManager;
        }

        private static ApplicationPartManager GetApplicationPartManager(AssemblyPart assemblyPart)
        {
            var applicationPartManager = new ApplicationPartManager();

            applicationPartManager.ApplicationParts.Add(assemblyPart);

            return applicationPartManager;
        }
    }
}
