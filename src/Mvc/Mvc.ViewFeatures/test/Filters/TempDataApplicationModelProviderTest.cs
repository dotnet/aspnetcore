// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

public class TempDataApplicationModelProviderTest
{
    [Fact]
    public void OnProvidersExecuting_DoesNotAddFilter_IfTypeHasNoTempDataProperties()
    {
        // Arrange
        var type = typeof(TestController_NoTempDataProperties);
        var provider = CreateProvider();

        var context = GetContext(type);

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var controller = Assert.Single(context.Result.Controllers);
        Assert.Empty(controller.Filters);
    }

    [Fact]
    public void OnProvidersExecuting_ValidatesTempDataProperties()
    {
        // Arrange
        var type = typeof(TestController_PrivateSet);
        var provider = CreateProvider();
        var expected = $"The '{type.FullName}.Test' property with TempDataAttribute is invalid. A property using TempDataAttribute must have a public getter and setter.";

        var context = GetContext(type);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void OnProvidersExecuting_ThrowsIfThePropertyTypeIsUnsupported()
    {
        // Arrange
        var type = typeof(TestController_InvalidProperties);
        var expected = $"TempData serializer '{typeof(DefaultTempDataSerializer)}' cannot serialize property '{type}.ModelState' of type '{typeof(ModelStateDictionary)}'." +
            Environment.NewLine +
            $"TempData serializer '{typeof(DefaultTempDataSerializer)}' cannot serialize property '{type}.TimeZone' of type '{typeof(TimeZoneInfo)}'.";
        var provider = CreateProvider();

        var context = GetContext(type);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void InitializeFilterFactory_WithExpectedPropertyHelpers_ForTempDataAttributeProperties()
    {
        // Arrange
        var type = typeof(TestController_OneTempDataProperty);
        var expected = type.GetProperty(nameof(TestController_OneTempDataProperty.Test2));
        var provider = CreateProvider();

        var context = GetContext(type);

        // Act
        provider.OnProvidersExecuting(context);
        var controller = context.Result.Controllers.SingleOrDefault();
        var filter = Assert.IsType<ControllerSaveTempDataPropertyFilterFactory>(Assert.Single(controller.Filters));

        // Assert
        Assert.NotNull(filter);
        var property = Assert.Single(filter.TempDataProperties);
        Assert.Same(expected, property.PropertyInfo);
        Assert.Equal("Test2", property.Key);
    }

    [Fact]
    public void OnProvidersExecuting_SetsKeyPrefixToEmptyString()
    {
        // Arrange
        var expected = typeof(TestController_OneTempDataProperty).GetProperty(nameof(TestController_OneTempDataProperty.Test2));
        var type = typeof(TestController_OneTempDataProperty);
        var provider = CreateProvider();
        var context = GetContext(type);

        // Act
        provider.OnProvidersExecuting(context);
        var controller = context.Result.Controllers.SingleOrDefault();
        var filter = Assert.IsType<ControllerSaveTempDataPropertyFilterFactory>(Assert.Single(controller.Filters));

        // Assert
        Assert.NotNull(filter);
        var property = Assert.Single(filter.TempDataProperties);
        Assert.Same(expected, property.PropertyInfo);
        Assert.Equal("Test2", property.Key);
    }

    private static TempDataApplicationModelProvider CreateProvider()
    {
        var tempDataSerializer = new DefaultTempDataSerializer();
        return new TempDataApplicationModelProvider(tempDataSerializer);
    }

    private static ApplicationModelProviderContext GetContext(Type type)
    {
        var defaultProvider = new DefaultApplicationModelProvider(
            Options.Create(new MvcOptions()),
            new EmptyModelMetadataProvider());

        var context = new ApplicationModelProviderContext(new[] { type.GetTypeInfo() });
        defaultProvider.OnProvidersExecuting(context);
        return context;
    }

    public class TestController_NoTempDataProperties
    {
        public DateTime? DateTime { get; set; }
    }

    public class TestController_OneTempDataProperty
    {
        public string Test { get; set; }

        [TempData]
        public string Test2 { get; set; }
    }

    public class TestController_PrivateSet
    {
        [TempData]
        public string Test { get; private set; }
    }

    public class TestController_InvalidProperties
    {
        [TempData]
        public ModelStateDictionary ModelState { get; set; }

        [TempData]
        public int SomeProperty { get; set; }

        [TempData]
        public TimeZoneInfo TimeZone { get; set; }
    }
}
