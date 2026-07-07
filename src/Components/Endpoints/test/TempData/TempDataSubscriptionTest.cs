// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class TempDataSubscriptionTest
{
    private readonly TempDataCascadingValueSupplier _supplier;
    private readonly TestComponent _component;

    public TempDataSubscriptionTest()
    {
        _supplier = new TempDataCascadingValueSupplier(NullLogger<TempDataCascadingValueSupplier>.Instance);
        _component = new TestComponent();
    }

    private TempDataCascadingValueSupplier.TempDataSubscription CreateSubscription(string key, Type propertyType)
    {
        return new TempDataCascadingValueSupplier.TempDataSubscription(
            _supplier,
            key,
            propertyType,
            () => _component.Value);
    }

    private static HttpContext CreateHttpContextWithTempData(ITempData tempData)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[TempDataProviderServiceCollectionExtensions.HttpContextItemKey] = tempData;
        return httpContext;
    }

    [Fact]
    public void GetValue_ReturnsNull_WhenHttpContextNotSet()
    {
        var subscription = CreateSubscription("key", typeof(string));

        var result = subscription.GetCurrentValue();

        Assert.Null(result);
    }

    [Fact]
    public void GetValue_ReturnsNull_WhenKeyNotFound()
    {
        var tempData = new TempData(() => new Dictionary<string, object>());
        var httpContext = CreateHttpContextWithTempData(tempData);
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("nonexistent", typeof(string));
        var result = subscription.GetCurrentValue();

        Assert.Null(result);
    }

    [Fact]
    public void GetValue_ReturnsValue_WhenKeyExists()
    {
        var tempData = new TempData(() => new Dictionary<string, object>());
        tempData["mykey"] = "myvalue";
        var httpContext = CreateHttpContextWithTempData(tempData);
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("mykey", typeof(string));
        var result = subscription.GetCurrentValue();

        Assert.Equal("myvalue", result);
    }

    [Fact]
    public void GetValue_IsCaseInsensitive()
    {
        var tempData = new TempData(() => new Dictionary<string, object>());
        tempData["MyKey"] = "myvalue";
        var httpContext = CreateHttpContextWithTempData(tempData);
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("mykey", typeof(string));
        var result = subscription.GetCurrentValue();

        Assert.Equal("myvalue", result);
    }

    [Fact]
    public void GetValue_ConvertsIntToEnum_WhenTargetTypeIsEnum()
    {
        var tempData = new TempData(() => new Dictionary<string, object>());
        tempData["status"] = 1; // Stored as int (enums are serialized as int)
        var httpContext = CreateHttpContextWithTempData(tempData);
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("status", typeof(TestEnum));
        var result = subscription.GetCurrentValue();

        Assert.IsType<TestEnum>(result);
        Assert.Equal(TestEnum.Active, result);
    }

    [Fact]
    public void GetValue_ConvertsIntToNullableEnum_WhenTargetTypeIsNullableEnum()
    {
        var tempData = new TempData(() => new Dictionary<string, object>());
        tempData["status"] = 2;
        var httpContext = CreateHttpContextWithTempData(tempData);
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("status", typeof(TestEnum));
        var result = subscription.GetCurrentValue();

        Assert.IsType<TestEnum>(result);
        Assert.Equal(TestEnum.Inactive, result);
    }

    [Fact]
    public void GetValue_ReturnsNull_WhenKeyNotFound_ForEnumType()
    {
        var tempData = new TempData(() => new Dictionary<string, object>());
        var httpContext = CreateHttpContextWithTempData(tempData);
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("missing", typeof(TestEnum));
        var result = subscription.GetCurrentValue();

        Assert.Null(result);
    }

    [Fact]
    public void GetValue_ReturnsNull_WhenExceptionOccurs()
    {
        var mockTempData = new Mock<ITempData>();
        mockTempData.Setup(t => t.Get(It.IsAny<string>())).Throws(new InvalidOperationException("test error"));
        var httpContext = new DefaultHttpContext();
        httpContext.Items[TempDataProviderServiceCollectionExtensions.HttpContextItemKey] = mockTempData.Object;
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("key", typeof(string));
        var result = subscription.GetCurrentValue();

        Assert.Null(result);
    }

    [Fact]
    public void GetValue_ReturnsNull_WhenStoredValueTypeDoesNotMatchTargetType()
    {
        // TempData has a string, but the component property expects an int
        var tempData = new TempData(() => new Dictionary<string, object>());
        tempData["key"] = "not an int";
        var httpContext = CreateHttpContextWithTempData(tempData);
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("key", typeof(int));
        var result = subscription.GetCurrentValue();

        // Should gracefully return null instead of crashing the render
        Assert.Null(result);
    }

    [Fact]
    public void GetValue_ReturnsNull_WhenStoredIntButTargetIsBool()
    {
        var tempData = new TempData(() => new Dictionary<string, object>());
        tempData["key"] = 42;
        var httpContext = CreateHttpContextWithTempData(tempData);
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("key", typeof(bool));
        var result = subscription.GetCurrentValue();

        Assert.Null(result);
    }

    [Fact]
    public void GetValue_ReturnsNull_WhenStoredBoolButTargetIsGuid()
    {
        var tempData = new TempData(() => new Dictionary<string, object>());
        tempData["key"] = true;
        var httpContext = CreateHttpContextWithTempData(tempData);
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("key", typeof(Guid));
        var result = subscription.GetCurrentValue();

        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentValue_ReturnsComponentValue_OnSubsequentCalls()
    {
        var tempData = new TempData(() => new Dictionary<string, object>());
        tempData["key"] = "original";
        var httpContext = CreateHttpContextWithTempData(tempData);
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("key", typeof(string));
        var firstResult = subscription.GetCurrentValue();

        _component.Value = "modified";
        var secondResult = subscription.GetCurrentValue();

        Assert.Equal("original", firstResult);
        Assert.Equal("modified", secondResult);
    }

    [Fact]
    public void CreateSubscription_RegistersValueCallbackAndReturnsSubscription()
    {
        var tempData = new TempData(() => new Dictionary<string, object>());
        tempData[nameof(TestComponent.Value)] = "from-tempdata";
        var httpContext = CreateHttpContextWithTempData(tempData);
        _supplier.SetRequestContext(httpContext);

        var renderer = new TestRenderer();
        var componentState = new ComponentState(renderer, 0, _component, null);
        var attribute = new SupplyParameterFromTempDataAttribute();
        var parameterInfo = new CascadingParameterInfo(attribute, nameof(TestComponent.Value), typeof(object));

        var subscription = _supplier.CreateSubscription(componentState, attribute, parameterInfo);

        Assert.NotNull(subscription);
        Assert.Equal("from-tempdata", subscription.GetCurrentValue());

        _component.Value = "updated";
        var persistedTempData = new TempData(() => new Dictionary<string, object>());
        _supplier.PersistValues(persistedTempData);
        Assert.Equal("updated", persistedTempData[nameof(TestComponent.Value)]);
    }

    private class TestComponent : IComponent
    {
        public object Value { get; set; }

        public void Attach(RenderHandle renderHandle) { }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    public enum TestEnum
    {
        None = 0,
        Active = 1,
        Inactive = 2,
    }
}
