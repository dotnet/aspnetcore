// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers.TagHelperFeatureProviderTests;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers
{
    public class TagHelperFeatureProviderTest
    {
        [Fact]
        public void Populate_IncludesTagHelpers()
        {
            // Arrange
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new TestApplicationPart(typeof(DiscoveryTagHelper)));
            manager.FeatureProviders.Add(new TagHelperFeatureProvider());

            var feature = new TagHelperFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            var tagHelperType = Assert.Single(feature.TagHelpers, th => th == typeof(DiscoveryTagHelper).GetTypeInfo());
        }

        [Fact]
        public void Populate_DoesNotIncludeDuplicateTagHelpers()
        {
            // Arrange
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new TestApplicationPart(typeof(DiscoveryTagHelper)));
            manager.ApplicationParts.Add(new TestApplicationPart(typeof(DiscoveryTagHelper)));
            manager.FeatureProviders.Add(new TagHelperFeatureProvider());

            var feature = new TagHelperFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            var tagHelperType = Assert.Single(feature.TagHelpers, th => th == typeof(DiscoveryTagHelper).GetTypeInfo());
        }

        [Fact]
        public void Populate_OnlyRunsOnPartsThatExportTypes()
        {
            // Arrange
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new TestApplicationPart(typeof(DiscoveryTagHelper)));
            manager.ApplicationParts.Add(new NonApplicationTypeProviderPart());
            manager.FeatureProviders.Add(new TagHelperFeatureProvider());

            var feature = new TagHelperFeature();

            // Act
            manager.PopulateFeature(feature);

            // Assert
            var tagHelperType = Assert.Single(feature.TagHelpers, th => th == typeof(DiscoveryTagHelper).GetTypeInfo());
        }

        private class NonApplicationTypeProviderPart : ApplicationPart
        {
            public override string Name => nameof(NonApplicationTypeProviderPart);

            public IEnumerable<TypeInfo> Types => new[] { typeof(AnotherTagHelper).GetTypeInfo() };
        }
    }
}

// These types need to be public for the test to work.
namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers.TagHelperFeatureProviderTests
{
    public class DiscoveryTagHelper : TagHelper
    {
    }

    public class AnotherTagHelper : TagHelper
    {
    }
}
