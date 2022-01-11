// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

public class DefaultViewComponentHelperTest
{
    [Fact]
    public void GetArgumentDictionary_SupportsNullArguments()
    {
        // Arrange
        var helper = CreateHelper();
        var descriptor = CreateDescriptorForType(typeof(ViewComponentSingleParam));

        // Act
        var argumentDictionary = DefaultViewComponentHelper.GetArgumentDictionary(descriptor, null);

        // Assert
        Assert.Equal(0, argumentDictionary.Count);
        Assert.IsType<Dictionary<string, object>>(argumentDictionary);
    }

    [Fact]
    public void GetArgumentDictionary_SupportsAnonymouslyTypedArguments()
    {
        // Arrange
        var helper = CreateHelper();
        var descriptor = CreateDescriptorForType(typeof(ViewComponentSingleParam));

        // Act
        var argumentDictionary = DefaultViewComponentHelper.GetArgumentDictionary(descriptor, new { a = 0 });

        // Assert
        Assert.Collection(argumentDictionary,
            item =>
            {
                Assert.Equal("a", item.Key);
                Assert.IsType<int>(item.Value);
                Assert.Equal(0, item.Value);
            });
    }

    [Fact]
    public void GetArgumentDictionary_SingleParameter_DoesNotNeedAnonymouslyTypedArguments()
    {
        // Arrange
        var helper = CreateHelper();
        var descriptor = CreateDescriptorForType(typeof(ViewComponentSingleParam));

        // Act
        var argumentDictionary = DefaultViewComponentHelper.GetArgumentDictionary(descriptor, 0);

        // Assert
        Assert.Collection(argumentDictionary,
            item =>
            {
                Assert.Equal("a", item.Key);
                Assert.IsType<int>(item.Value);
                Assert.Equal(0, item.Value);
            });
    }

    [Fact]
    public void GetArgumentDictionary_MultipleParameters_NeedsAnonymouslyTypedArguments()
    {
        // Arrange
        var helper = CreateHelper();
        var descriptor = CreateDescriptorForType(typeof(ViewComponentMultipleParam));

        // Act
        var argumentDictionary = DefaultViewComponentHelper.GetArgumentDictionary(descriptor, new { a = 0, b = "foo" });

        // Assert
        Assert.Collection(argumentDictionary,
            item1 =>
            {
                Assert.Equal("a", item1.Key);
                Assert.IsType<int>(item1.Value);
                Assert.Equal(0, item1.Value);
            },
            item2 =>
            {
                Assert.Equal("b", item2.Key);
                Assert.IsType<string>(item2.Value);
                Assert.Equal("foo", item2.Value);
            });
    }

    [Fact]
    public void GetArgumentDictionary_SingleObjectParameter_DoesNotNeedAnonymouslyTypedArguments()
    {
        // Arrange
        var helper = CreateHelper();
        var descriptor = CreateDescriptorForType(typeof(ViewComponentObjectParam));
        var expectedValue = new object();

        // Act
        var argumentDictionary = DefaultViewComponentHelper.GetArgumentDictionary(descriptor, expectedValue);

        // Assert
        Assert.Collection(argumentDictionary,
            item =>
            {
                Assert.Equal("o", item.Key);
                Assert.IsType<object>(item.Value);
                Assert.Same(expectedValue, item.Value);
            });
    }

    [Fact]
    public void GetArgumentDictionary_SingleParameter_AcceptsDictionaryType()
    {
        // Arrange
        var helper = CreateHelper();
        var descriptor = CreateDescriptorForType(typeof(ViewComponentSingleParam));
        var arguments = new Dictionary<string, object>
            {
                { "a", 10 }
            };

        // Act
        var argumentDictionary = DefaultViewComponentHelper.GetArgumentDictionary(descriptor, arguments);

        // Assert
        Assert.Collection(argumentDictionary,
            item =>
            {
                Assert.Equal("a", item.Key);
                Assert.IsType<int>(item.Value);
                Assert.Equal(10, item.Value);
            });
    }

    private DefaultViewComponentHelper CreateHelper()
    {
        var descriptorCollectionProvider = Mock.Of<IViewComponentDescriptorCollectionProvider>();
        var selector = Mock.Of<IViewComponentSelector>();
        var invokerFactory = Mock.Of<IViewComponentInvokerFactory>();
        var viewBufferScope = Mock.Of<IViewBufferScope>();

        return new DefaultViewComponentHelper(
            descriptorCollectionProvider,
            new HtmlTestEncoder(),
            selector,
            invokerFactory,
            viewBufferScope);
    }

    private ViewComponentDescriptor CreateDescriptorForType(Type componentType)
    {
        var provider = CreateProvider(componentType);
        return provider.GetViewComponents().First();
    }

    private class ViewComponentSingleParam
    {
        public IViewComponentResult Invoke(int a) => null;
    }

    private class ViewComponentMultipleParam
    {
        public IViewComponentResult Invoke(int a, string b) => null;
    }

    private class ViewComponentObjectParam
    {
        public IViewComponentResult Invoke(object o) => null;
    }

    private DefaultViewComponentDescriptorProvider CreateProvider(Type componentType)
    {
        return new FilteredViewComponentDescriptorProvider(componentType);
    }

    // This will only consider types nested inside this class as ViewComponent classes
    private class FilteredViewComponentDescriptorProvider : DefaultViewComponentDescriptorProvider
    {
        public FilteredViewComponentDescriptorProvider(params Type[] allowedTypes)
            : base(GetApplicationPartManager(allowedTypes.Select(t => t.GetTypeInfo())))
        {
        }

        private static ApplicationPartManager GetApplicationPartManager(IEnumerable<TypeInfo> types)
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
