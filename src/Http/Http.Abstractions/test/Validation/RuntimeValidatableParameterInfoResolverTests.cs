// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.IO.Pipelines;
using System.Reflection;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Http.Validation.Tests;

public class RuntimeValidatableParameterInfoResolverTests
{
    private readonly RuntimeValidatableParameterInfoResolver _resolver = new();

    [Fact]
    public void TryGetValidatableTypeInfo_AlwaysReturnsFalse()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(string), out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableParameterInfo_WithNullName_ThrowsInvalidOperationException()
    {
        var parameterInfo = new NullNameParameterInfo();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _resolver.TryGetValidatableParameterInfo(parameterInfo, out _));

        Assert.Contains("without a name", exception.Message);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(DayOfWeek))] // Enum
    [InlineData(typeof(ClaimsPrincipal))]
    [InlineData(typeof(PipeReader))]
    [InlineData(typeof(DateTimeOffset))]
    [InlineData(typeof(TimeOnly))]
    [InlineData(typeof(DateOnly))]
    [InlineData(typeof(TimeSpan))]
    [InlineData(typeof(IFormFile))]
    [InlineData(typeof(IFormFileCollection))]
    [InlineData(typeof(IFormCollection))]
    [InlineData(typeof(HttpContext))]
    [InlineData(typeof(HttpRequest))]
    [InlineData(typeof(HttpResponse))]
    [InlineData(typeof(CancellationToken))]
    public void TryGetValidatableParameterInfo_WithSimpleTypesAndNoAttributes_ReturnsFalse(Type parameterType)
    {
        var parameterInfo = GetParameter(parameterType);

        var result = _resolver.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableParameterInfo_WithClassTypeAndNoAttributes_ReturnsTrue()
    {
        var parameterInfo = GetParameter(typeof(TestClass));

        var result = _resolver.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
        var parameterValidatableInfo = Assert.IsType<RuntimeValidatableParameterInfoResolver.RuntimeValidatableParameterInfo>(validatableInfo);
        Assert.Equal("testParam", parameterValidatableInfo.Name);
        Assert.Equal("testParam", parameterValidatableInfo.DisplayName);
    }

    [Fact]
    public void TryGetValidatableParameterInfo_WithSimpleTypeAndAttributes_ReturnsTrue()
    {
        var parameterInfo = typeof(TestController)
            .GetMethod(nameof(TestController.MethodWithAttributedParam))!
            .GetParameters()[0];

        var result = _resolver.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
        var parameterValidatableInfo = Assert.IsType<RuntimeValidatableParameterInfoResolver.RuntimeValidatableParameterInfo>(validatableInfo);
        Assert.Equal("value", parameterValidatableInfo.Name);
        Assert.Equal("value", parameterValidatableInfo.DisplayName);
    }

    [Fact]
    public void TryGetValidatableParameterInfo_WithDisplayAttribute_UsesDisplayNameFromAttribute()
    {
        var parameterInfo = typeof(TestController)
            .GetMethod(nameof(TestController.MethodWithDisplayAttribute))!
            .GetParameters()[0];

        var result = _resolver.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
        var parameterValidatableInfo = Assert.IsType<RuntimeValidatableParameterInfoResolver.RuntimeValidatableParameterInfo>(validatableInfo);
        Assert.Equal("value", parameterValidatableInfo.Name);
        Assert.Equal("Custom Display Name", parameterValidatableInfo.DisplayName);
    }

    [Fact]
    public void TryGetValidatableParameterInfo_WithDisplayAttributeWithNullName_UsesParameterName()
    {
        var parameterInfo = typeof(TestController)
            .GetMethod(nameof(TestController.MethodWithNullDisplayName))!
            .GetParameters()[0];

        var result = _resolver.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
        var parameterValidatableInfo = Assert.IsType<RuntimeValidatableParameterInfoResolver.RuntimeValidatableParameterInfo>(validatableInfo);
        Assert.Equal("value", parameterValidatableInfo.Name);
        Assert.Equal("value", parameterValidatableInfo.DisplayName);
    }

    [Fact]
    public void TryGetValidatableParameterInfo_WithNullableValueType_ReturnsFalse()
    {
        var parameterInfo = GetParameter(typeof(int?));

        var result = _resolver.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableParameterInfo_WithNullableReferenceType_ReturnsTrue()
    {
        var parameterInfo = GetNullableParameter(typeof(TestClass));

        var result = _resolver.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
        var parameterValidatableInfo = Assert.IsType<RuntimeValidatableParameterInfoResolver.RuntimeValidatableParameterInfo>(validatableInfo);
        Assert.Equal("testParam", parameterValidatableInfo.Name);
        Assert.Equal("testParam", parameterValidatableInfo.DisplayName);
    }

    private static ParameterInfo GetParameter(Type parameterType)
    {
        return typeof(TestParameterHolder)
            .GetMethod(nameof(TestParameterHolder.Method))!
            .MakeGenericMethod(parameterType)
            .GetParameters()[0];
    }

    private static ParameterInfo GetNullableParameter(Type parameterType)
    {
        return typeof(TestParameterHolder)
            .GetMethod(nameof(TestParameterHolder.MethodWithNullable))!
            .MakeGenericMethod(parameterType)
            .GetParameters()[0];
    }

    private class TestClass { }

    private class TestParameterHolder
    {
        public void Method<T>(T testParam) { }
        public void MethodWithNullable<T>(T? testParam) { }
    }

    private class TestController
    {
        public void MethodWithAttributedParam([Required] string value) { }

        public void MethodWithDisplayAttribute([Display(Name = "Custom Display Name")][Required] string value) { }

        public void MethodWithNullDisplayName([Display(Name = null)][Required] string value) { }
    }

    private class NullNameParameterInfo : ParameterInfo
    {
        public override string? Name => null;
        public override Type ParameterType => typeof(string);
    }
}
