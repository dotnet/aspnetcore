// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class TempDataCascadingValueSupplierTest
{
    private readonly TempDataCascadingValueSupplier _supplier;

    public TempDataCascadingValueSupplierTest()
    {
        _supplier = new TempDataCascadingValueSupplier(NullLogger<TempDataCascadingValueSupplier>.Instance);
    }

    [Fact]
    public void RegisterValueCallback_AddsCallback()
    {
        var callbackInvoked = false;
        _supplier.RegisterValueCallback("key", () =>
        {
            callbackInvoked = true;
            return "value";
        });

        var tempData = new TempData(() => new Dictionary<string, object>());
        _supplier.PersistValues(tempData);

        Assert.True(callbackInvoked);
    }

    [Fact]
    public void RegisterValueCallback_ThrowsForDuplicateKey()
    {
        _supplier.RegisterValueCallback("key", () => "value1");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _supplier.RegisterValueCallback("key", () => "value2"));

        Assert.Contains("key", ex.Message);
    }

    [Fact]
    public void PersistValues_SetsValueInTempData()
    {
        _supplier.RegisterValueCallback("key", () => "persisted value");

        var tempData = new TempData(() => new Dictionary<string, object>());
        _supplier.PersistValues(tempData);

        Assert.Equal("persisted value", tempData.Peek("key"));
    }

    [Fact]
    public void PersistValues_RemovesKey_WhenCallbackReturnsNull()
    {
        var tempData = new TempData(() => new Dictionary<string, object>());
        tempData["key"] = "existing";

        _supplier.RegisterValueCallback("key", () => null);
        _supplier.PersistValues(tempData);

        Assert.Null(tempData.Peek("key"));
    }

    [Fact]
    public void PersistValues_HandlesMultipleKeys()
    {
        _supplier.RegisterValueCallback("key1", () => "value1");
        _supplier.RegisterValueCallback("key2", () => "value2");
        _supplier.RegisterValueCallback("key3", () => "value3");

        var tempData = new TempData(() => new Dictionary<string, object>());
        _supplier.PersistValues(tempData);

        Assert.Equal("value1", tempData.Peek("key1"));
        Assert.Equal("value2", tempData.Peek("key2"));
        Assert.Equal("value3", tempData.Peek("key3"));
    }

    [Fact]
    public void PersistValues_ContinuesOnCallbackException()
    {
        _supplier.RegisterValueCallback("key1", () => throw new InvalidOperationException("Test exception"));
        _supplier.RegisterValueCallback("key2", () => "value2");

        var tempData = new TempData(() => new Dictionary<string, object>());
        _supplier.PersistValues(tempData);

        Assert.Null(tempData.Peek("key1"));
        Assert.Equal("value2", tempData.Peek("key2"));
    }

    [Fact]
    public void PersistValues_IsCaseInsensitiveForKeys()
    {
        _supplier.RegisterValueCallback("MyKey", () => "value");

        var tempData = new TempData(() => new Dictionary<string, object>());
        _supplier.PersistValues(tempData);

        Assert.True(tempData.ContainsKey("mykey"));
    }

    [Fact]
    public void DeleteCallbacks_RemovesCallbacksForKey()
    {
        var callbackInvoked = false;
        _supplier.RegisterValueCallback("key", () =>
        {
            callbackInvoked = true;
            return "value";
        });

        _supplier.DeleteValueCallback("key");

        var tempData = new TempData(() => new Dictionary<string, object>());
        _supplier.PersistValues(tempData);

        Assert.False(callbackInvoked);
        Assert.Null(tempData.Peek("key"));
    }
}
