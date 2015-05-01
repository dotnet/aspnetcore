// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public class DefaultViewComponentDescriptorProviderTest
    {
        [Fact]
        public void GetDescriptor_DefaultConventions()
        {
            // Arrange
            var provider = CreateProvider(typeof(ConventionsViewComponent));

            // Act
            var descriptors = provider.GetViewComponents();

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(typeof(ConventionsViewComponent), descriptor.Type);
            Assert.Equal("Microsoft.AspNet.Mvc.ViewComponents.Conventions", descriptor.FullName);
            Assert.Equal("Conventions", descriptor.ShortName);
        }

        [Fact]
        public void GetDescriptor_WithAttribute()
        {
            // Arrange
            var provider = CreateProvider(typeof(AttributeViewComponent));

            // Act
            var descriptors = provider.GetViewComponents();

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(typeof(AttributeViewComponent), descriptor.Type);
            Assert.Equal("AttributesAreGreat", descriptor.FullName);
            Assert.Equal("AttributesAreGreat", descriptor.ShortName);
        }

        private class ConventionsViewComponent
        {
        }

        [ViewComponent(Name = "AttributesAreGreat")]
        private class AttributeViewComponent
        {
        }

        private DefaultViewComponentDescriptorProvider CreateProvider(Type componentType)
        {
            return new FilteredViewComponentDescriptorProvider(componentType);
        }

        // This will only consider types nested inside this class as ViewComponent classes
        private class FilteredViewComponentDescriptorProvider : DefaultViewComponentDescriptorProvider
        {
            public FilteredViewComponentDescriptorProvider(params Type[] allowedTypes)
                : base(GetAssemblyProvider())
            {
                AllowedTypes = allowedTypes;
            }

            public Type[] AllowedTypes { get; }

            protected override bool IsViewComponentType(TypeInfo typeInfo)
            {
                return AllowedTypes.Contains(typeInfo.AsType());
            }

            // Need to override this since the default provider does not support private classes.
            protected override IEnumerable<TypeInfo> GetCandidateTypes()
            {
                return
                    GetAssemblyProvider()
                    .CandidateAssemblies
                    .SelectMany(a => a.DefinedTypes)
                    .Select(t => t.GetTypeInfo());
            }

            private static IAssemblyProvider GetAssemblyProvider()
            {
                var assemblyProvider = new FixedSetAssemblyProvider();
                assemblyProvider.CandidateAssemblies.Add(
                    typeof(FilteredViewComponentDescriptorProvider).GetTypeInfo().Assembly);

                return assemblyProvider;
            }
        }
    }
}