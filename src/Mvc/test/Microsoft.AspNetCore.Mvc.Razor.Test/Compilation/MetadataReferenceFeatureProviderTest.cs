// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    public class MetadataReferenceFeatureProviderTest
    {
        [Fact]
        public void PopulateFeature_ReturnsEmptyList_IfNoAssemblyPartsAreRegistered()
        {
            // Arrange
            var applicationPartManager = new ApplicationPartManager();
            applicationPartManager.ApplicationParts.Add(Mock.Of<ApplicationPart>());
            applicationPartManager.FeatureProviders.Add(new MetadataReferenceFeatureProvider());
            var feature = new MetadataReferenceFeature();

            // Act
            applicationPartManager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.MetadataReferences);
        }

        [Fact]
        public void PopulateFeature_AddsMetadataReferenceForAssemblyPartsWithDependencyContext()
        {
            // Arrange
            var applicationPartManager = new ApplicationPartManager();
            var currentAssembly = GetType().GetTypeInfo().Assembly;
            var assemblyPart1 = new AssemblyPart(currentAssembly);
            applicationPartManager.ApplicationParts.Add(assemblyPart1);
            var assemblyPart2 = new AssemblyPart(typeof(MetadataReferenceFeatureProvider).GetTypeInfo().Assembly);
            applicationPartManager.ApplicationParts.Add(assemblyPart2);
            applicationPartManager.FeatureProviders.Add(new MetadataReferenceFeatureProvider());
            var feature = new MetadataReferenceFeature();

            // Act
            applicationPartManager.PopulateFeature(feature);

            // Assert
            Assert.Contains(
                feature.MetadataReferences,
                reference => reference.Display.Equals(currentAssembly.Location));
        }
    }
}
