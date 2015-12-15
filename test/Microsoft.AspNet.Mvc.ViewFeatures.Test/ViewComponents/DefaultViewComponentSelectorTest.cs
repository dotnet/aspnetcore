// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public class DefaultViewComponentSelectorTest
    {
        private static readonly string Namespace = typeof(DefaultViewComponentSelectorTest).Namespace;

        [Fact]
        public void SelectComponent_ByShortNameWithSuffix()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent("Suffix");

            // Assert
            Assert.Same(typeof(ViewComponentContainer.SuffixViewComponent), result.Type);
        }

        [Fact]
        public void SelectComponent_ByLongNameWithSuffix()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent($"{Namespace}.Suffix");

            // Assert
            Assert.Same(typeof(ViewComponentContainer.SuffixViewComponent), result.Type);
        }

        [Fact]
        public void SelectComponent_ByShortNameWithoutSuffix()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent("WithoutSuffix");

            // Assert
            Assert.Same(typeof(ViewComponentContainer.WithoutSuffix), result.Type);
        }

        [Fact]
        public void SelectComponent_ByLongNameWithoutSuffix()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent($"{Namespace}.WithoutSuffix");

            // Assert
            Assert.Same(typeof(ViewComponentContainer.WithoutSuffix), result.Type);
        }

        [Fact]
        public void SelectComponent_ByAttribute()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent("ByAttribute");

            // Assert
            Assert.Same(typeof(ViewComponentContainer.ByAttribute), result.Type);
        }

        [Fact]
        public void SelectComponent_ByNamingConvention()
        {
            // Arrange
            var selector = CreateSelector();

            // Act
            var result = selector.SelectComponent("ByNamingConvention");

            // Assert
            Assert.Same(typeof(ViewComponentContainer.ByNamingConventionViewComponent), result.Type);
        }

        [Fact]
        public void SelectComponent_Ambiguity()
        {
            // Arrange
            var selector = CreateSelector();

            var expected =
                "The view component name 'Ambiguous' matched multiple types:" + Environment.NewLine +
                $"Type: '{typeof(ViewComponentContainer.Ambiguous1)}' - " +
                "Name: 'Namespace1.Ambiguous'" + Environment.NewLine +
                $"Type: '{typeof(ViewComponentContainer.Ambiguous2)}' - " +
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
            Assert.Same(typeof(ViewComponentContainer.Ambiguous1), result.Type);
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
            Assert.Same(typeof(ViewComponentContainer.FullNameInAttribute), result.Type);
        }

        private IViewComponentSelector CreateSelector()
        {
            var provider = new DefaultViewComponentDescriptorCollectionProvider(
                    new FilteredViewComponentDescriptorProvider());
            return new DefaultViewComponentSelector(provider);
        }

        private class ViewComponentContainer
        {
            public class SuffixViewComponent : ViewComponent
            {
                public string Invoke() => "Hello";
            }

            public class WithoutSuffix : ViewComponent
            {
                public string Invoke() => "Hello";
            }

            public class ByNamingConventionViewComponent
            {
                public string Invoke() => "Hello";
            }

            [ViewComponent]
            public class ByAttribute
            {
                public string Invoke() => "Hello";
            }

            [ViewComponent(Name = "Namespace1.Ambiguous")]
            public class Ambiguous1
            {
                public string Invoke() => "Hello";
            }

            [ViewComponent(Name = "Namespace2.Ambiguous")]
            public class Ambiguous2
            {
                public string Invoke() => "Hello";
            }

            [ViewComponent(Name = "CoolNameSpace.FullNameInAttribute")]
            public class FullNameInAttribute
            {
                public string Invoke() => "Hello";
            }
        }

        // This will only consider types nested inside this class as ViewComponent classes
        private class FilteredViewComponentDescriptorProvider : DefaultViewComponentDescriptorProvider
        {
            public FilteredViewComponentDescriptorProvider()
                : base(GetAssemblyProvider())
            {
                AllowedTypes = typeof(ViewComponentContainer).GetNestedTypes(bindingAttr: BindingFlags.Public);
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
#if DNX451
                    .Select(t => t.GetTypeInfo())
#endif
                    ;
            }

            private static IAssemblyProvider GetAssemblyProvider()
            {
                var assemblyProvider = new StaticAssemblyProvider();
                assemblyProvider.CandidateAssemblies.Add(
                    typeof(ViewComponentContainer).GetTypeInfo().Assembly);

                return assemblyProvider;
            }
        }
    }
}