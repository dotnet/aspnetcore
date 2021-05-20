// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
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
