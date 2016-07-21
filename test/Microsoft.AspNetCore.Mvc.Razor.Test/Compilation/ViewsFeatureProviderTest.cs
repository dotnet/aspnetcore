// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    public class ViewsFeatureProviderTest
    {
        [Fact]
        public void PopulateFeature_ReturnsEmptySequenceIfNoAssemblyPartHasViewAssembly()
        {
            // Arrange
            var applicationPartManager = new ApplicationPartManager();
            applicationPartManager.ApplicationParts.Add(
                new AssemblyPart(typeof(ViewsFeatureProviderTest).GetTypeInfo().Assembly));
            applicationPartManager.FeatureProviders.Add(new ViewsFeatureProvider());
            var feature = new MetadataReferenceFeature();

            // Act
            applicationPartManager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.MetadataReferences);
        }

        [Fact]
        public void PopulateFeature_ReturnsViewsFromAllAvailableApplicationParts()
        {
            // Arrange
            var applicationPart1 = new Mock<ApplicationPart>();
            var viewsProvider1 = applicationPart1
                .As<IViewsProvider>()
                .SetupGet(p => p.Views)
                .Returns(new[]
                {
                    new ViewInfo("/Views/test/Index.cshtml", typeof(object))
                });
            var applicationPart2 = new Mock<ApplicationPart>();
            var viewsProvider2 = applicationPart2
                .As<IViewsProvider>()
                .SetupGet(p => p.Views)
                .Returns(new[]
                {
                    new ViewInfo("/Areas/Admin/Views/Index.cshtml", typeof(string)),
                    new ViewInfo("/Areas/Admin/Views/About.cshtml", typeof(int))
                });


            var applicationPartManager = new ApplicationPartManager();
            applicationPartManager.ApplicationParts.Add(applicationPart1.Object);
            applicationPartManager.ApplicationParts.Add(applicationPart2.Object);
            applicationPartManager.FeatureProviders.Add(new ViewsFeatureProvider());
            var feature = new MetadataReferenceFeature();

            // Act
            applicationPartManager.PopulateFeature(feature);

            // Assert
            Assert.Empty(feature.MetadataReferences);
        }
    }
}
