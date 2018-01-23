// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class RazorProjectEngineBuilderExtensionsTest
    {
        [Fact]
        public void SetImportFeature_SetsTheImportFeature()
        {
            // Arrange
            var builder = new DefaultRazorProjectEngineBuilder(false, Mock.Of<RazorProjectFileSystem>());
            var testFeature1 = Mock.Of<IRazorImportFeature>();
            var testFeature2 = Mock.Of<IRazorImportFeature>();
            builder.Features.Add(testFeature1);
            builder.Features.Add(testFeature2);
            var newFeature = Mock.Of<IRazorImportFeature>();

            // Act
            builder.SetImportFeature(newFeature);

            // Assert
            var feature = Assert.Single(builder.Features);
            Assert.Same(newFeature, feature);
        }
    }
}
