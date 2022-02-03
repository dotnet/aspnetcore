// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

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
        Assert.Same(typeof(ViewComponentContainer.SuffixViewComponent).GetTypeInfo(), result.TypeInfo);
    }

    [Fact]
    public void SelectComponent_ByLongNameWithSuffix()
    {
        // Arrange
        var selector = CreateSelector();

        // Act
        var result = selector.SelectComponent($"{Namespace}.Suffix");

        // Assert
        Assert.Same(typeof(ViewComponentContainer.SuffixViewComponent).GetTypeInfo(), result.TypeInfo);
    }

    [Fact]
    public void SelectComponent_ByShortNameWithoutSuffix()
    {
        // Arrange
        var selector = CreateSelector();

        // Act
        var result = selector.SelectComponent("WithoutSuffix");

        // Assert
        Assert.Same(typeof(ViewComponentContainer.WithoutSuffix).GetTypeInfo(), result.TypeInfo);
    }

    [Fact]
    public void SelectComponent_ByLongNameWithoutSuffix()
    {
        // Arrange
        var selector = CreateSelector();

        // Act
        var result = selector.SelectComponent($"{Namespace}.WithoutSuffix");

        // Assert
        Assert.Same(typeof(ViewComponentContainer.WithoutSuffix).GetTypeInfo(), result.TypeInfo);
    }

    [Fact]
    public void SelectComponent_ByAttribute()
    {
        // Arrange
        var selector = CreateSelector();

        // Act
        var result = selector.SelectComponent("ByAttribute");

        // Assert
        Assert.Same(typeof(ViewComponentContainer.ByAttribute).GetTypeInfo(), result.TypeInfo);
    }

    [Fact]
    public void SelectComponent_ByNamingConvention()
    {
        // Arrange
        var selector = CreateSelector();

        // Act
        var result = selector.SelectComponent("ByNamingConvention");

        // Assert
        Assert.Same(typeof(ViewComponentContainer.ByNamingConventionViewComponent).GetTypeInfo(), result.TypeInfo);
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

    [Theory]
    [InlineData("Name")]
    [InlineData("Ambiguous.Name")]
    public void SelectComponent_AmbiguityDueToDerivation(string name)
    {
        // Arrange
        var selector = CreateSelector();
        var expected =
            $"The view component name '{name}' matched multiple types:" + Environment.NewLine +
            $"Type: '{typeof(ViewComponentContainer.AmbiguousBase)}' - " +
            "Name: 'Ambiguous.Name'" + Environment.NewLine +
            $"Type: '{typeof(ViewComponentContainer.DerivedAmbiguous)}' - " +
            "Name: 'Ambiguous.Name'";

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => selector.SelectComponent(name));

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
        Assert.Same(typeof(ViewComponentContainer.Ambiguous1).GetTypeInfo(), result.TypeInfo);
    }

    [Fact]
    public void SelectComponent_OverrideNameToAvoidAmbiguity()
    {
        // Arrange
        var selector = CreateSelector();

        // Act
        var result = selector.SelectComponent("NonAmbiguousName");

        // Assert
        Assert.Same(typeof(ViewComponentContainer.DerivedAmbiguousWithOverriddenName).GetTypeInfo(), result.TypeInfo);
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
        Assert.Same(typeof(ViewComponentContainer.FullNameInAttribute).GetTypeInfo(), result.TypeInfo);
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

        [ViewComponent(Name = "Ambiguous.Name")]
        public class AmbiguousBase
        {
            public string Invoke() => "Hello";
        }

        public class DerivedAmbiguous : AmbiguousBase
        {
        }

        [ViewComponent(Name = "NonAmbiguousName")]
        public class DerivedAmbiguousWithOverriddenName : AmbiguousBase
        {
        }
    }
    // This will only consider types nested inside this class as ViewComponent classes
    private class FilteredViewComponentDescriptorProvider : DefaultViewComponentDescriptorProvider
    {
        public FilteredViewComponentDescriptorProvider()
            : this(typeof(ViewComponentContainer).GetNestedTypes(bindingAttr: BindingFlags.Public))
        {
        }

        // For error messages in tests above, ensure the TestApplicationPart returns types in a consistent order.
        public FilteredViewComponentDescriptorProvider(params Type[] allowedTypes)
            : base(GetApplicationPartManager(allowedTypes.OrderBy(type => type.Name, StringComparer.Ordinal)))
        {
        }

        private static ApplicationPartManager GetApplicationPartManager(IEnumerable<Type> types)
        {
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new TestApplicationPart(types));
            manager.FeatureProviders.Add(new TestFeatureProvider());
            return manager;
        }

        private class TestFeatureProvider : IApplicationFeatureProvider<ViewComponentFeature>
        {
            public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewComponentFeature feature)
            {
                foreach (var type in parts.OfType<IApplicationPartTypeProvider>().SelectMany(p => p.Types))
                {
                    feature.ViewComponents.Add(type);
                }
            }
        }
    }
}
