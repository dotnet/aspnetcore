// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers
{
    public class FeatureTagHelperTypeResolverTest
    {
        [Fact]
        public void Resolve_ReturnsTagHelpers_FromApplicationParts()
        {
            // Arrange
            var manager = new ApplicationPartManager();
            var types = new[] { typeof(TestTagHelper) };
            manager.ApplicationParts.Add(new TestApplicationPart(types));
            manager.FeatureProviders.Add(new TestFeatureProvider());

            var resolver = new FeatureTagHelperTypeResolver(manager);

            var assemblyName = typeof(FeatureTagHelperTypeResolverTest).GetTypeInfo().Assembly.GetName().Name;

            // Act
            var result = resolver.Resolve(assemblyName, SourceLocation.Undefined, new ErrorSink());

            // Assert
            var type = Assert.Single(result);
            Assert.Equal(typeof(TestTagHelper), type);
        }

        [Fact]
        public void Resolve_ReturnsTagHelpers_FilteredByAssembly()
        {
            // Arrange
            var manager = new ApplicationPartManager();
            var types = new[] { typeof(TestTagHelper) };
            manager.ApplicationParts.Add(new AssemblyPart(typeof(InputTagHelper).GetTypeInfo().Assembly));
            manager.ApplicationParts.Add(new TestApplicationPart(types));
            manager.FeatureProviders.Add(new TestFeatureProvider());

            var resolver = new FeatureTagHelperTypeResolver(manager);

            var assemblyName = typeof(FeatureTagHelperTypeResolverTest).GetTypeInfo().Assembly.GetName().Name;

            // Act
            var result = resolver.Resolve(assemblyName, SourceLocation.Undefined, new ErrorSink());

            // Assert
            var type = Assert.Single(result);
            Assert.Equal(typeof(TestTagHelper), type);
        }

        [Fact]
        public void Resolve_ReturnsEmptyTypesList_IfAssemblyLoadFails()
        {
            // Arrange
            var manager = new ApplicationPartManager();
            var types = new[] { typeof(TestTagHelper) };
            manager.ApplicationParts.Add(new AssemblyPart(typeof(InputTagHelper).GetTypeInfo().Assembly));
            manager.ApplicationParts.Add(new TestApplicationPart(types));
            manager.FeatureProviders.Add(new TestFeatureProvider());

            var resolver = new FeatureTagHelperTypeResolver(manager);

            // Act
            var result = resolver.Resolve("UnknownAssembly", SourceLocation.Undefined, new ErrorSink());

            // Assert
            Assert.Empty(result);
        }

        private class TestFeatureProvider : IApplicationFeatureProvider<TagHelperFeature>
        {
            public void PopulateFeature(IEnumerable<ApplicationPart> parts, TagHelperFeature feature)
            {
                foreach (var type in parts.OfType<IApplicationPartTypeProvider>().SelectMany(tp => tp.Types))
                {
                    feature.TagHelpers.Add(type);
                }
            }
        }

        private class TestTagHelper : TagHelper
        {
        }

        private class NotInPartsTagHelper : TagHelper
        {
        }
    }
}
