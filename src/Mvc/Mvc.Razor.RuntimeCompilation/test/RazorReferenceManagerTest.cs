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

            var applicationPartManager = GetApplicationPartManager();
            var referenceManager = new RazorReferenceManager(
                applicationPartManager,
                Options.Create(options));

            var expected = new[] { ApplicationPartReferencePath, additionalReferencePath };

            // Act
            var references = referenceManager.GetReferencePaths();

            // Assert
            Assert.Equal(expected, references);
        }

//        [Fact]
//        public void GetCompilationReferences_ResolutionOfAssemblyPartsWhichAreInOptionsShouldNotThrow()
//        {
//            /**
//             * If the application throws during the process of reference resolution the purpose of this project is no longer fulfilled.
//             * Ideally this should end up in logs. If it does throw you end up in a scenario where
//             * - you don't use RuntimeCompilation and have to rebuild after every change
//             * - you use RuntimeCompilation but you also have to rebuild because having any exception because of a failed resolution will also force you to rebuild after every change.
//             */
//
//            // Arrange
//            var options = new MvcRazorRuntimeCompilationOptions();
//            var additionalReferencePath = "additional-path";
//            options.AdditionalReferencePaths.Add(additionalReferencePath);
//
//            var applicationPartManager = PartManagerWithAssemblyPart(additionalReferencePath);
//            var referenceManager = new RazorReferenceManager(
//                applicationPartManager,
//                Options.Create(options));
//            
//            // Act
//            var references = referenceManager.GetReferencePaths();
//
//            // Assert
//            Assert.Contains(additionalReferencePath, references);
//        }

        private static ApplicationPartManager PartManagerWithAssemblyPart(string referencePath)
        {
            var applicationPartManager = new ApplicationPartManager();

            var assembly = new Mock<Assembly>();
            assembly
                .Setup(d => d.Location)
                .Returns(referencePath);

            var part = new Mock<AssemblyPart>();

            part
                .Setup(p => p.Assembly)
                .Returns(assembly.Object);

            applicationPartManager.ApplicationParts.Add(part.Object);

            return applicationPartManager;
        }

        private static ApplicationPartManager GetApplicationPartManager()
        {
            var applicationPartManager = new ApplicationPartManager();
            var part = new Mock<ApplicationPart>();

            part.As<ICompilationReferencesProvider>()
                .Setup(p => p.GetReferencePaths())
                .Returns(new[] { ApplicationPartReferencePath });

            applicationPartManager.ApplicationParts.Add(part.Object);

            return applicationPartManager;
        }
    }
}
