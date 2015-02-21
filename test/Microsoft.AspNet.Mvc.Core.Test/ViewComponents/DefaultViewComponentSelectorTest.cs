// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Internal;
using Xunit;

namespace Microsoft.AspNet.Mvc
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
            Assert.Equal(typeof(SuffixViewComponent), result);
        }

        [Fact]
        public void SelectComponent_ByLongNameWithSuffix()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent("Microsoft.AspNet.Mvc.Suffix");

            // Assert
            Assert.Equal(typeof(SuffixViewComponent), result);
        }

        [Fact]
        public void SelectComponent_ByShortNameWithoutSuffix()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent("WithoutSuffix");

            // Assert
            Assert.Equal(typeof(WithoutSuffix), result);
        }

        [Fact]
        public void SelectComponent_ByLongNameWithoutSuffix()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent("Microsoft.AspNet.Mvc.WithoutSuffix");

            // Assert
            Assert.Equal(typeof(WithoutSuffix), result);
        }

        [Fact]
        public void SelectComponent_ByAttribute()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent("ByAttribute");

            // Assert
            Assert.Equal(typeof(ByAttribute), result);
        }

        [Fact]
        public void SelectComponent_ByNamingConvention()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent("ByNamingConvention");

            // Assert
            Assert.Equal(typeof(ByNamingConventionViewComponent), result);
        }

        [Fact]
        public void SelectComponent_Ambiguity()
        {
            // Arrange
            var selector = CreateSelector();

            var expected =
                "The view component name 'Ambiguous' matched multiple types:" + Environment.NewLine +
                "Type: 'Microsoft.AspNet.Mvc.DefaultViewComponentSelectorTest+Ambiguous1' - " +
                "Name: 'Namespace1.Ambiguous'" + Environment.NewLine +
                "Type: 'Microsoft.AspNet.Mvc.DefaultViewComponentSelectorTest+Ambiguous2' - " +
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
            Assert.Equal(typeof(Ambiguous1), result);
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
            Assert.Equal(typeof(FullNameInAttribute), result);
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
        private class FilteredViewComponentSelector : DefaultViewComponentSelector
        {
            public FilteredViewComponentSelector()
                : base(GetAssemblyProvider())
            {
                AllowedTypes = typeof(DefaultViewComponentSelectorTest).GetNestedTypes(BindingFlags.NonPublic);
            }

            public Type[] AllowedTypes { get; private set; }

            protected override bool IsViewComponentType([NotNull] TypeInfo typeInfo)
            {
                return AllowedTypes.Contains(typeInfo.AsType());
            }

            private static IAssemblyProvider GetAssemblyProvider()
            {
                var assemblyProvider = new FixedSetAssemblyProvider();
                assemblyProvider.CandidateAssemblies.Add(
                    typeof(FilteredViewComponentSelector).GetTypeInfo().Assembly);

                return assemblyProvider;
            }
        }
    }
}