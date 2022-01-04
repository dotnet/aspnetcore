// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class TempDataFilterPageApplicationModelProviderTest
{
    [Fact]
    public void OnProvidersExecuting_DoesNotAddFilter_IfTypeHasNoTempDataProperties()
    {
        // Arrange
        var type = typeof(TestPageModel_NoTempDataProperties);
        var provider = CreateProvider();
        var context = CreateProviderContext(type);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        Assert.Empty(context.PageApplicationModel.Filters);
    }

    [Fact]
    public void OnProvidersExecuting_ValidatesTempDataProperties()
    {
        // Arrange
        var type = typeof(TestPageModel_PrivateSet);
        var expected = $"The '{type.FullName}.Test' property with TempDataAttribute is invalid. A property using TempDataAttribute must have a public getter and setter.";

        var provider = CreateProvider();
        var context = CreateProviderContext(type);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void OnProvidersExecuting_ThrowsIfThePropertyTypeIsUnsupported()
    {
        // Arrange
        var type = typeof(TestPageModel_InvalidProperties);
        var expected = $"TempData serializer '{typeof(DefaultTempDataSerializer)}' cannot serialize property '{type}.ModelState' of type '{typeof(ModelStateDictionary)}'." +
            Environment.NewLine +
            $"TempData serializer '{typeof(DefaultTempDataSerializer)}' cannot serialize property '{type}.TimeZone' of type '{typeof(TimeZoneInfo)}'.";

        var provider = CreateProvider();
        var context = CreateProviderContext(type);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void AddsTempDataPropertyFilter_ForTempDataAttributeProperties()
    {
        // Arrange
        var type = typeof(TestPageModel_OneTempDataProperty);
        var provider = CreateProvider();
        var context = CreateProviderContext(type);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var filter = Assert.Single(context.PageApplicationModel.Filters);
        Assert.IsType<PageSaveTempDataPropertyFilterFactory>(filter);
    }

    [Fact]
    public void InitializeFilterFactory_WithExpectedPropertyHelpers_ForTempDataAttributeProperties()
    {
        // Arrange
        var type = typeof(TestPageModel_OneTempDataProperty);
        var provider = CreateProvider();
        var context = CreateProviderContext(type);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var filter = Assert.IsType<PageSaveTempDataPropertyFilterFactory>(Assert.Single(context.PageApplicationModel.Filters));
        Assert.Collection(
            filter.Properties,
            property =>
            {
                Assert.Equal("Test2", property.Key);
                Assert.Equal(type.GetProperty(nameof(TestPageModel_OneTempDataProperty.Test2)), property.PropertyInfo);
            });
    }

    [Fact]
    public void OnProvidersExecuting_SetsKeyPrefixToEmptyString()
    {
        // Arrange
        var type = typeof(TestPageModel_OneTempDataProperty);
        var provider = CreateProvider();
        var context = CreateProviderContext(type);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var filter = Assert.IsType<PageSaveTempDataPropertyFilterFactory>(Assert.Single(context.PageApplicationModel.Filters));
        Assert.Collection(
            filter.Properties,
            property =>
            {
                Assert.Equal("Test2", property.Key);
            });
    }

    private static PageApplicationModelProviderContext CreateProviderContext(Type handlerType)
    {
        var descriptor = new CompiledPageActionDescriptor();
        var context = new PageApplicationModelProviderContext(descriptor, typeof(TestPage).GetTypeInfo())
        {
            PageApplicationModel = new PageApplicationModel(descriptor, handlerType.GetTypeInfo(), Array.Empty<object>()),
        };

        return context;
    }

    private static TempDataFilterPageApplicationModelProvider CreateProvider()
    {
        var tempDataSerializer = new DefaultTempDataSerializer();
        return new TempDataFilterPageApplicationModelProvider(tempDataSerializer);
    }

    private class TestPage : Page
    {
        public object Model => null;

        public override Task ExecuteAsync() => null;
    }

    public class TestPageModel_NoTempDataProperties
    {
        public DateTime? DateTime { get; set; }
    }

    public class TestPageModel_NullableNonPrimitiveTempDataProperty
    {
        [TempData]
        public DateTime? DateTime { get; set; }
    }

    public class TestPageModel_OneTempDataProperty
    {
        public string Test { get; set; }

        [TempData]
        public string Test2 { get; set; }
    }

    public class TestPageModel_PrivateSet
    {
        [TempData]
        public string Test { get; private set; }
    }

    public class TestPageModel_InvalidProperties
    {
        [TempData]
        public ModelStateDictionary ModelState { get; set; }

        [TempData]
        public int SomeProperty { get; set; }

        [TempData]
        public TimeZoneInfo TimeZone { get; set; }
    }
}
