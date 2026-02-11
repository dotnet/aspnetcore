// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class TempDataValueMapperTest
{
    private readonly TempDataValueMapper _mapper;

    public TempDataValueMapperTest()
    {
        _mapper = new TempDataValueMapper(NullLogger<TempDataValueMapper>.Instance);
    }

    private static HttpContext CreateHttpContextWithTempData(ITempData tempData)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[typeof(ITempData)] = tempData;
        return httpContext;
    }

    [Fact]
    public void GetValue_ReturnsNull_WhenHttpContextNotSet()
    {
        var result = _mapper.GetValue("key", typeof(string));

        Assert.Null(result);
    }

    [Fact]
    public void GetValue_ReturnsNull_WhenKeyNotFound()
    {
        var tempData = new TempData(() => new Dictionary<string, object>());
        var httpContext = CreateHttpContextWithTempData(tempData);
        _mapper.SetRequestContext(httpContext);

        var result = _mapper.GetValue("nonexistent", typeof(string));

        Assert.Null(result);
    }

    [Fact]
    public void GetValue_ReturnsValue_WhenKeyExists()
    {
        var tempData = new TempData(() => new Dictionary<string, object>());
        tempData["mykey"] = "myvalue";
        var httpContext = CreateHttpContextWithTempData(tempData);
        _mapper.SetRequestContext(httpContext);

        var result = _mapper.GetValue("mykey", typeof(string));

        Assert.Equal("myvalue", result);
    }

    [Fact]
    public void GetValue_IsCaseInsensitive()
    {
        var tempData = new TempData(() => new Dictionary<string, object>());
        tempData["MyKey"] = "myvalue";
        var httpContext = CreateHttpContextWithTempData(tempData);
        _mapper.SetRequestContext(httpContext);

        var result = _mapper.GetValue("mykey", typeof(string));

        Assert.Equal("myvalue", result);
    }

    [Fact]
    public void RegisterValueCallback_AddsCallback()
    {
        var callbackInvoked = false;
        _mapper.RegisterValueCallback("key", () =>
        {
            callbackInvoked = true;
            return "value";
        });

        var tempData = new TempData(() => new Dictionary<string, object>());
        _mapper.PersistValues(tempData);

        Assert.True(callbackInvoked);
    }

    [Fact]
    public void RegisterValueCallback_ThrowsForDuplicateKey()
    {
        _mapper.RegisterValueCallback("key", () => "value1");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _mapper.RegisterValueCallback("key", () => "value2"));

        Assert.Contains("key", ex.Message);
    }

    [Fact]
    public void PersistValues_SetsValueInTempData()
    {
        _mapper.RegisterValueCallback("key", () => "persisted value");

        var tempData = new TempData(() => new Dictionary<string, object>());
        _mapper.PersistValues(tempData);

        Assert.Equal("persisted value", tempData.Peek("key"));
    }

    [Fact]
    public void PersistValues_RemovesKey_WhenCallbackReturnsNull()
    {
        var tempData = new TempData(() => new Dictionary<string, object>());
        tempData["key"] = "existing";

        _mapper.RegisterValueCallback("key", () => null);
        _mapper.PersistValues(tempData);

        Assert.Null(tempData.Peek("key"));
    }

    [Fact]
    public void PersistValues_HandlesMultipleKeys()
    {
        _mapper.RegisterValueCallback("key1", () => "value1");
        _mapper.RegisterValueCallback("key2", () => "value2");
        _mapper.RegisterValueCallback("key3", () => "value3");

        var tempData = new TempData(() => new Dictionary<string, object>());
        _mapper.PersistValues(tempData);

        Assert.Equal("value1", tempData.Peek("key1"));
        Assert.Equal("value2", tempData.Peek("key2"));
        Assert.Equal("value3", tempData.Peek("key3"));
    }

    [Fact]
    public void PersistValues_ContinuesOnCallbackException()
    {
        _mapper.RegisterValueCallback("key1", () => throw new InvalidOperationException("Test exception"));
        _mapper.RegisterValueCallback("key2", () => "value2");

        var tempData = new TempData(() => new Dictionary<string, object>());
        _mapper.PersistValues(tempData);

        Assert.Null(tempData.Peek("key1"));
        Assert.Equal("value2", tempData.Peek("key2"));
    }

    [Fact]
    public void PersistValues_IsCaseInsensitiveForKeys()
    {
        _mapper.RegisterValueCallback("MyKey", () => "value");

        var tempData = new TempData(() => new Dictionary<string, object>());
        _mapper.PersistValues(tempData);

        Assert.True(tempData.ContainsKey("mykey"));
    }

    [Fact]
    public void DeleteCallbacks_RemovesCallbacksForKey()
    {
        var callbackInvoked = false;
        _mapper.RegisterValueCallback("key", () =>
        {
            callbackInvoked = true;
            return "value";
        });

        _mapper.DeleteValueCallback("key");

        var tempData = new TempData(() => new Dictionary<string, object>());
        _mapper.PersistValues(tempData);

        Assert.False(callbackInvoked);
        Assert.Null(tempData.Peek("key"));
    }
}
