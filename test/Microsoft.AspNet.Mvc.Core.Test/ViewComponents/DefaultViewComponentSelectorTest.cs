// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Internal;
using Xunit;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public class DefaultViewComponentSelectorTest
    {
        [Fact]
        public void SelectComponent_ByShortNameWithSuffix()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent("Suffix");

            // Assert
            Assert.Equal(typeof(SuffixViewComponent), result.Type);
        }

        [Fact]
        public void SelectComponent_ByLongNameWithSuffix()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent("Microsoft.AspNet.Mvc.ViewComponents.Suffix");

            // Assert
            Assert.Equal(typeof(SuffixViewComponent), result.Type);
        }

        [Fact]
        public void SelectComponent_ByShortNameWithoutSuffix()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent("WithoutSuffix");

            // Assert
            Assert.Equal(typeof(WithoutSuffix), result.Type);
        }

        [Fact]
        public void SelectComponent_ByLongNameWithoutSuffix()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent("Microsoft.AspNet.Mvc.ViewComponents.WithoutSuffix");

            // Assert
            Assert.Equal(typeof(WithoutSuffix), result.Type);
        }

        [Fact]
        public void SelectComponent_ByAttribute()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent("ByAttribute");

            // Assert
            Assert.Equal(typeof(ByAttribute), result.Type);
        }

        [Fact]
        public void SelectComponent_ByNamingConvention()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent("ByNamingConvention");

            // Assert
            Assert.Equal(typeof(ByNamingConventionViewComponent), result.Type);
        }

        [Fact]
        public void SelectComponent_Ambiguity()
        {
            // Arrange
            var selector = CreateSelector();

            var expected =
                "The view component name 'Ambiguous' matched multiple types:" + Environment.NewLine +
                "Type: 'Microsoft.AspNet.Mvc.ViewComponents.DefaultViewComponentSelectorTest+Ambiguous1' - " +
                "Name: 'Namespace1.Ambiguous'" + Environment.NewLine +
                "Type: 'Microsoft.AspNet.Mvc.ViewComponents.DefaultViewComponentSelectorTest+Ambiguous2' - " +
                "Name: 'Namespace2.Ambiguous'";

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => selector.SelectComponent("Ambiguous"));

            // Assert
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void SelectComponent_FullNameToAvoidAmbiguity()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent("Namespace1.Ambiguous");

            // Assert
            Assert.Equal(typeof(Ambiguous1), result.Type);
        }

        [Theory]
        [InlineData("FullNameInAttribute")]
        [InlineData("CoolNameSpace.FullNameInAttribute")]
        public void SelectComponent_FullNameInAttribute(string name)
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent(name);

            // Assert
            Assert.Equal(typeof(FullNameInAttribute), result.Type);
        }

        private IViewComponentSelector CreateSelector()
        {
            return new FilteredViewComponentSelector();
        }

        private class SuffixViewComponent : ViewComponent
        {
        }

        private class WithoutSuffix : ViewComponent
        {
        }

        private class ByNamingConventionViewComponent
        {
        }

        [ViewComponent]
        private class ByAttribute
        {
        }

        [ViewComponent(Name = "Namespace1.Ambiguous")]
        private class Ambiguous1
        {
        }

        [ViewComponent(Name = "Namespace2.Ambiguous")]
        private class Ambiguous2
        {
        }

        [ViewComponent(Name = "CoolNameSpace.FullNameInAttribute")]
        private class FullNameInAttribute
        {
        }

        // This will only consider types nested inside this class as ViewComponent classes
        private class FilteredViewComponentDescriptorProvider : DefaultViewComponentDescriptorProvider
        {
            public FilteredViewComponentDescriptorProvider()
                : base(GetAssemblyProvider())
            {
                AllowedTypes = typeof(DefaultViewComponentSelectorTest).GetNestedTypes(BindingFlags.NonPublic);
            }

            public Type[] AllowedTypes { get; private set; }

            protected override bool IsViewComponentType([NotNull] TypeInfo typeInfo)
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
                    typeof(FilteredViewComponentSelector).GetTypeInfo().Assembly);

                return assemblyProvider;
            }
        }

        // This will only consider types nested inside this class as ViewComponent classes
        private class FilteredViewComponentSelector : DefaultViewComponentSelector
        {
            public FilteredViewComponentSelector()
                : base(new DefaultViewComponentDescriptorCollectionProvider(new FilteredViewComponentDescriptorProvider()))
            {
            }
        }
    }
}