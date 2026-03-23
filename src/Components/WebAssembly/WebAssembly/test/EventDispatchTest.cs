// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

public class EventDispatchTest
{
    [Fact]
    public void CreateFieldInfo_ReturnsNull_WhenFieldComponentIdIsNegative()
    {
        var result = DefaultWebAssemblyJSRuntime.CreateFieldInfo(-1, "someValue", false);

        Assert.Null(result);
    }

    [Fact]
    public void CreateFieldInfo_ReturnsNull_WhenFieldComponentIdIsMinusOne()
    {
        var result = DefaultWebAssemblyJSRuntime.CreateFieldInfo(-100, null, true);

        Assert.Null(result);
    }

    [Fact]
    public void CreateFieldInfo_SetsStringFieldValue_WhenFieldValueStringIsNotNull()
    {
        var result = DefaultWebAssemblyJSRuntime.CreateFieldInfo(42, "hello", true);

        Assert.NotNull(result);
        Assert.Equal(42, result.ComponentId);
        Assert.Equal("hello", result.FieldValue);
    }

    [Fact]
    public void CreateFieldInfo_SetsBoolFieldValue_WhenFieldValueStringIsNull_True()
    {
        var result = DefaultWebAssemblyJSRuntime.CreateFieldInfo(7, null, true);

        Assert.NotNull(result);
        Assert.Equal(7, result.ComponentId);
        Assert.Equal(true, result.FieldValue);
    }

    [Fact]
    public void CreateFieldInfo_SetsBoolFieldValue_WhenFieldValueStringIsNull_False()
    {
        var result = DefaultWebAssemblyJSRuntime.CreateFieldInfo(7, null, false);

        Assert.NotNull(result);
        Assert.Equal(7, result.ComponentId);
        Assert.Equal(false, result.FieldValue);
    }

    [Fact]
    public void CreateFieldInfo_UsesCachedBoxedBooleans()
    {
        var resultTrue1 = DefaultWebAssemblyJSRuntime.CreateFieldInfo(1, null, true);
        var resultTrue2 = DefaultWebAssemblyJSRuntime.CreateFieldInfo(2, null, true);
        var resultFalse1 = DefaultWebAssemblyJSRuntime.CreateFieldInfo(1, null, false);
        var resultFalse2 = DefaultWebAssemblyJSRuntime.CreateFieldInfo(2, null, false);

        Assert.Same(resultTrue1!.FieldValue, resultTrue2!.FieldValue);
        Assert.Same(resultFalse1!.FieldValue, resultFalse2!.FieldValue);
        Assert.NotSame(resultTrue1.FieldValue, resultFalse1.FieldValue);
    }

    [Fact]
    public void CreateFieldInfo_SetsComponentId_WhenFieldComponentIdIsZero()
    {
        var result = DefaultWebAssemblyJSRuntime.CreateFieldInfo(0, null, false);

        Assert.NotNull(result);
        Assert.Equal(0, result.ComponentId);
    }

    [Fact]
    public void UnflattenTouchPoints_ReturnsEmptyArray_WhenInputIsEmpty()
    {
        var result = DefaultWebAssemblyJSRuntime.UnflattenTouchPoints([]);

        Assert.Empty(result);
    }

    [Fact]
    public void UnflattenTouchPoints_ReturnsEmptyArray_WhenInputIsNull()
    {
        var result = DefaultWebAssemblyJSRuntime.UnflattenTouchPoints(null);

        Assert.Empty(result);
    }

    [Fact]
    public void UnflattenTouchPoints_ReturnsSingleTouchPoint()
    {
        double[] flat = [42, 100.5, 200.5, 300.5, 400.5, 500.5, 600.5];

        var result = DefaultWebAssemblyJSRuntime.UnflattenTouchPoints(flat);

        Assert.Single(result);
        Assert.Equal(42, result[0].Identifier);
        Assert.Equal(100.5, result[0].ScreenX);
        Assert.Equal(200.5, result[0].ScreenY);
        Assert.Equal(300.5, result[0].ClientX);
        Assert.Equal(400.5, result[0].ClientY);
        Assert.Equal(500.5, result[0].PageX);
        Assert.Equal(600.5, result[0].PageY);
    }

    [Fact]
    public void UnflattenTouchPoints_ReturnsMultipleTouchPoints()
    {
        double[] flat =
        [
            1, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0,
            2, 11.0, 21.0, 31.0, 41.0, 51.0, 61.0,
            3, 12.0, 22.0, 32.0, 42.0, 52.0, 62.0,
        ];

        var result = DefaultWebAssemblyJSRuntime.UnflattenTouchPoints(flat);

        Assert.Equal(3, result.Length);

        Assert.Equal(1, result[0].Identifier);
        Assert.Equal(10.0, result[0].ScreenX);
        Assert.Equal(60.0, result[0].PageY);

        Assert.Equal(2, result[1].Identifier);
        Assert.Equal(11.0, result[1].ScreenX);
        Assert.Equal(41.0, result[1].ClientY);

        Assert.Equal(3, result[2].Identifier);
        Assert.Equal(32.0, result[2].ClientX);
        Assert.Equal(62.0, result[2].PageY);
    }
}
