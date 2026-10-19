// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using static Microsoft.AspNetCore.Components.Endpoints.SessionCascadingValueSupplierTest;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class SessionSubscriptionTest
{
    private readonly SessionCascadingValueSupplier _supplier;
    private readonly TestComponent _component;

    public SessionSubscriptionTest()
    {
        _supplier = new SessionCascadingValueSupplier(new JsonStoredDataSerializer(), NullLogger<SessionCascadingValueSupplier>.Instance);
        _component = new TestComponent();
    }

    private static readonly JsonStoredDataSerializer _serializer = new();

    private static void SetSessionValue(HttpContext httpContext, string key, object value, Type type)
    {
        httpContext.Session.Set(key, _serializer.SerializeValue(value, type));
    }

    private SessionCascadingValueSupplier.SessionSubscription CreateSubscription(string key, Type propertyType)
    {
        return new SessionCascadingValueSupplier.SessionSubscription(
            _supplier,
            key,
            propertyType,
            () => _component.Value);
    }

    private void CreateSubscriptionForPropertyType(Type propertyType)
    {
        var renderer = new TestRenderer();
        var componentState = new ComponentState(renderer, 0, _component, null);
        var attribute = new SupplyParameterFromSessionAttribute();
        var parameterInfo = new CascadingParameterInfo(attribute, nameof(TestComponent.Value), propertyType);
        _supplier.CreateSubscription(componentState, attribute, parameterInfo);
    }

    [Fact]
    public void CreateSubscription_Throws_ForUnsupportedType()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => CreateSubscriptionForPropertyType(typeof(CustomObject)));
        Assert.Contains("not supported", ex.Message);
    }

    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(int?))]
    [InlineData(typeof(TestEnum))]
    [InlineData(typeof(TestEnum?))]
    [InlineData(typeof(List<int>))]
    [InlineData(typeof(object))]
    [InlineData(typeof(IList<int>))]
    public void CreateSubscription_DoesNotThrow_ForSupportedOrPolymorphicType(Type propertyType)
    {
        var exception = Record.Exception(() => CreateSubscriptionForPropertyType(propertyType));

        Assert.Null(exception);
    }

    [Fact]
    public void GetValue_ReturnsNull_WhenHttpContextNotSet()
    {
        var subscription = CreateSubscription("key", typeof(string));

        var result = subscription.GetCurrentValue();

        Assert.Null(result);
    }

    [Fact]
    public void GetValue_LogsWarning_WhenHttpContextNotSet()
    {
        var sink = new TestSink();
        var supplier = new SessionCascadingValueSupplier(
            new JsonStoredDataSerializer(),
            new TestLoggerFactory(sink, enabled: true).CreateLogger<SessionCascadingValueSupplier>());
        var subscription = new SessionCascadingValueSupplier.SessionSubscription(
            supplier, "key", typeof(string), () => _component.Value);

        var result = subscription.GetCurrentValue();

        Assert.Null(result);
        var write = Assert.Single(sink.Writes);
        Assert.Equal(LogLevel.Warning, write.LogLevel);
        Assert.Equal("SessionUnavailable", write.EventId.Name);
    }

    [Fact]
    public void GetValue_Throws_WhenSessionIsNull()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<Microsoft.AspNetCore.Http.Features.ISessionFeature>(new TestSessionFeature(null!));
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("key", typeof(string));

        Assert.Throws<InvalidOperationException>(() => subscription.GetCurrentValue());
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
        SetSessionValue(httpContext, "mykey", "myvalue", typeof(string));
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("mykey", typeof(string));
        var result = subscription.GetCurrentValue();

        Assert.Equal("myvalue", result);
    }

    [Fact]
    public void GetValue_LowercasesSessionKey()
    {
        var httpContext = CreateHttpContextWithSession();
        SetSessionValue(httpContext, "mykey", "myvalue", typeof(string));
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("MyKey", typeof(string));
        var result = subscription.GetCurrentValue();

        Assert.Equal("myvalue", result);
    }

    [Fact]
    public void GetValue_DeserializesEnum()
    {
        var httpContext = CreateHttpContextWithSession();
        SetSessionValue(httpContext, "status", TestEnum.Inactive, typeof(TestEnum));
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("status", typeof(TestEnum?));
        var result = subscription.GetCurrentValue();

        Assert.IsType<TestEnum>(result);
        Assert.Equal(TestEnum.Inactive, result);
    }

    [Fact]
    public void GetValue_RestoresList_FromSession()
    {
        var httpContext = CreateHttpContextWithSession();
        SetSessionValue(httpContext, "numbers", new List<int> { 1, 2, 3 }, typeof(List<int>));
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("numbers", typeof(List<int>));
        var result = subscription.GetCurrentValue();

        var list = Assert.IsType<List<int>>(result);
        Assert.Equal(new List<int> { 1, 2, 3 }, list);
    }

    [Fact]
    public void GetValue_RestoresEnumArray_FromSession()
    {
        var httpContext = CreateHttpContextWithSession();
        SetSessionValue(httpContext, "statuses", new[] { TestEnum.Active, TestEnum.Inactive }, typeof(TestEnum[]));
        _supplier.SetRequestContext(httpContext);

        var subscription = CreateSubscription("statuses", typeof(TestEnum[]));
        var result = subscription.GetCurrentValue();

        var array = Assert.IsType<TestEnum[]>(result);
        Assert.Equal(new[] { TestEnum.Active, TestEnum.Inactive }, array);
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
        SetSessionValue(httpContext, "key", "original", typeof(string));
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
        var httpContext = CreateHttpContextWithSession();
        httpContext.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        SetSessionValue(httpContext, nameof(TestComponent.Value).ToLowerInvariant(), "from-session", typeof(string));
        _supplier.SetRequestContext(httpContext);

        var renderer = new TestRenderer();
        var componentState = new ComponentState(renderer, 0, _component, null);
        var attribute = new SupplyParameterFromSessionAttribute();
        var parameterInfo = new CascadingParameterInfo(attribute, nameof(TestComponent.Value), typeof(string));

        var subscription = _supplier.CreateSubscription(componentState, attribute, parameterInfo);

        Assert.NotNull(subscription);
        Assert.Equal("from-session", subscription.GetCurrentValue());

        _component.Value = "updated";
        await _supplier.PersistAllValues();
        Assert.True(httpContext.Session.TryGetValue(nameof(TestComponent.Value).ToLowerInvariant(), out var updatedBytes));
        var updatedValue = _serializer.DeserializeValue(updatedBytes, typeof(string));
        Assert.Equal("updated", updatedValue);
    }

    private class TestComponent : IComponent
    {
        public object? Value { get; set; }

        public void Attach(RenderHandle renderHandle) { }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    private class CustomObject
    {
        public int Value { get; set; }
    }

    public enum TestEnum
    {
        None = 0,
        Active = 1,
        Inactive = 2,
    }
}
