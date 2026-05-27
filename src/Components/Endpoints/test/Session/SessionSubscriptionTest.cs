// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using static Microsoft.AspNetCore.Components.Endpoints.SessionCascadingValueSupplierTest;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class SessionSubscriptionTest
{
    private readonly SessionCascadingValueSupplier _supplier;
    private readonly TestComponent _component;

    public SessionSubscriptionTest()
    {
        _supplier = new SessionCascadingValueSupplier(NullLogger<SessionCascadingValueSupplier>.Instance);
        _component = new TestComponent();
    }

    private SessionCascadingValueSupplier.SessionSubscription CreateSubscription(string key, Type propertyType)
    {
        return new SessionCascadingValueSupplier.SessionSubscription(
            _supplier,
            key,
            propertyType,
            () => _component.Value);
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
        var httpContext = CreateHttpContextWithSession();
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("nonexistent", typeof(string));
        var result = subscription.GetCurrentValue();

        Assert.Null(result);
    }

    [Fact]
    public void GetValue_ReturnsValue_WhenKeyExists()
    {
        var httpContext = CreateHttpContextWithSession();
        httpContext.Session.SetString("mykey", "\"myvalue\"");
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("mykey", typeof(string));
        var result = subscription.GetCurrentValue();

        Assert.Equal("myvalue", result);
    }

    [Fact]
    public void GetValue_LowercasesSessionKey()
    {
        var httpContext = CreateHttpContextWithSession();
        httpContext.Session.SetString("mykey", "\"myvalue\"");
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("MyKey", typeof(string));
        var result = subscription.GetCurrentValue();

        Assert.Equal("myvalue", result);
    }

    [Fact]
    public void GetValue_DeserializesEnum()
    {
        var httpContext = CreateHttpContextWithSession();
        httpContext.Session.SetString("status", "2");
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("status", typeof(TestEnum?));
        var result = subscription.GetCurrentValue();

        Assert.IsType<TestEnum>(result);
        Assert.Equal(TestEnum.Inactive, result);
    }

    [Fact]
    public void GetValue_ReturnsNull_WhenDeserializationFails()
    {
        var httpContext = CreateHttpContextWithSession();
        httpContext.Session.SetString("key", "not-valid-json-for-int");
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("key", typeof(int));
        var result = subscription.GetCurrentValue();

        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentValue_ReturnsComponentValue_OnSubsequentCalls()
    {
        var httpContext = CreateHttpContextWithSession();
        httpContext.Session.SetString("key", "\"original\"");
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("key", typeof(string));
        var firstResult = subscription.GetCurrentValue();

        _component.Value = "modified";
        var secondResult = subscription.GetCurrentValue();

        Assert.Equal("original", firstResult);
        Assert.Equal("modified", secondResult);
    }

    [Fact]
    public async Task CreateSubscription_RegistersValueCallbackAndReturnsSubscription()
    {
        var httpContext = CreateHttpContextWithSession(out var responseFeature);
        httpContext.Session.SetString(nameof(TestComponent.Value).ToLowerInvariant(), "\"from-session\"");
        _supplier.SetRequestContext(httpContext);

        var renderer = new TestRenderer();
        var componentState = new ComponentState(renderer, 0, _component, null);
        var attribute = new SupplyParameterFromSessionAttribute();
        var parameterInfo = new CascadingParameterInfo(attribute, nameof(TestComponent.Value), typeof(string));

        var subscription = _supplier.CreateSubscription(componentState, attribute, parameterInfo);

        Assert.NotNull(subscription);
        Assert.Equal("from-session", subscription.GetCurrentValue());

        _component.Value = "updated";
        await responseFeature.FireOnStartingAsync();
        Assert.Equal("\"updated\"", httpContext.Session.GetString(nameof(TestComponent.Value).ToLowerInvariant()));
    }

    private class TestComponent : IComponent
    {
        public object? Value { get; set; }

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
