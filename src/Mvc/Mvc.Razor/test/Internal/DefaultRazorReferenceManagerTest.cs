// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Test.Internal
{
    public class DefaultRazorReferenceManagerTest
    {
        [Fact]
        public void GetCompilationReferences_CombinesApplicationPartAndOptionMetadataReferences()
        {
            // Arrange
            var options = new RazorViewEngineOptions();
            var objectAssemblyLocation = typeof(object).GetTypeInfo().Assembly.Location;
            var objectAssemblyMetadataReference = MetadataReference.CreateFromFile(objectAssemblyLocation);
            options.AdditionalCompilationReferences.Add(objectAssemblyMetadataReference);

            var applicationPartManager = GetApplicationPartManager();
            var feature = new MetadataReferenceFeature();
            applicationPartManager.PopulateFeature(feature);
            var partReferences = feature.MetadataReferences;
            var expectedReferenceDisplays = partReferences
                .Concat(new[] { objectAssemblyMetadataReference })
                .Select(r => r.Display);
            var referenceManager = new DefaultRazorReferenceManager(
                applicationPartManager,
                Options.Create(options));

            // Act
            var references = referenceManager.CompilationReferences;
            var referenceDisplays = references.Select(reference => reference.Display);

            // Assert
            Assert.Equal(expectedReferenceDisplays, referenceDisplays);
        }

        private static ApplicationPartManager GetApplicationPartManager()
        {
            var applicationPartManager = new ApplicationPartManager();
            var assembly = typeof(DefaultRazorReferenceManagerTest).GetTypeInfo().Assembly;
            applicationPartManager.ApplicationParts.Add(new AssemblyPart(assembly));
            applicationPartManager.FeatureProviders.Add(new MetadataReferenceFeatureProvider());

            return applicationPartManager;
        }
    }
}
