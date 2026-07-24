// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.IO.Pipelines;
using System.Reflection;
using System.Security.Claims;

namespace Microsoft.Extensions.Validation.Tests;

public class RuntimeValidatableParameterInfoResolverTests : ValidationTestBase
{
    [Fact]
    public void TryGetValidatableTypeInfo_AlwaysReturnsFalse()
    {
        var (_, options) = GeneratedValidationTestHelpers.CreateValidationServices();

        Assert.False(options.TryGetValidatableTypeInfo(typeof(UnannotatedRuntimeParameterClass), out var validatableInfo));
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableParameterInfo_WithNullName_ThrowsInvalidOperationException()
    {
        var (_, options) = GeneratedValidationTestHelpers.CreateValidationServices();

        var exception = Assert.Throws<InvalidOperationException>(() => options.TryGetValidatableParameterInfo(new NullNameParameterInfo(), out _));

        Assert.Contains("without a name", exception.Message);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(DayOfWeek))]
    [InlineData(typeof(ClaimsPrincipal))]
    [InlineData(typeof(PipeReader))]
    [InlineData(typeof(DateTimeOffset))]
    [InlineData(typeof(TimeOnly))]
    [InlineData(typeof(DateOnly))]
    [InlineData(typeof(TimeSpan))]
    [InlineData(typeof(CancellationToken))]
    public void TryGetValidatableParameterInfo_WithSimpleTypesAndNoAttributes_ReturnsFalse(Type parameterType)
    {
        var (_, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var parameterInfo = typeof(RuntimeParameterHolder).GetMethod(nameof(RuntimeParameterHolder.Method))!.MakeGenericMethod(parameterType).GetParameters()[0];

        Assert.False(options.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo));
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableParameterInfo_WithClassTypeAndNoAttributes_ReturnsTrue()
    {
        var (_, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var parameterInfo = typeof(RuntimeParameterHolder).GetMethod(nameof(RuntimeParameterHolder.Method))!.MakeGenericMethod(typeof(UnannotatedRuntimeParameterClass)).GetParameters()[0];

        Assert.True(options.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo));
        Assert.NotNull(validatableInfo);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryGetValidatableParameterInfo_WithDisplayAttribute_UsesDisplayNameFromAttribute(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var parameterInfo = GetParameter(nameof(RuntimeParameterActions.DisplayAttributeParameter));
        Assert.True(options.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo));
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(validatableInfo, null, context, useAsync, default);

        Assert.Equal("The Custom Display Name field is required.", Assert.Single(context.ValidationErrors!).Value.Select(e => e.ErrorMessage).Single());
    }

    [Fact]
    public async Task TryGetValidatableParameterInfo_WithDisplayAttributeWithResourceType_BypassesLocalizer()
    {
        var localizer = new RecordingValidationLocalizer { DisplayNameResult = "Should not be used" };
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices(o => o.Localizer = localizer);
        var parameterInfo = GetParameter(nameof(RuntimeParameterActions.ResourceDisplayAttributeParameter));
        Assert.True(options.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo));
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await validatableInfo.ValidateAsync(null, context, default);

        Assert.Empty(localizer.DisplayNameCalls);
        Assert.Equal("The Resource Display Name field is required.", Assert.Single(context.ValidationErrors!).Value.Select(e => e.ErrorMessage).Single());
    }

    [Fact]
    public async Task TryGetValidatableParameterInfo_WithLiteralDisplayAttribute_ConsultsLocalizerWhenSet()
    {
        var localizer = new RecordingValidationLocalizer { DisplayNameResult = "Localized Custom Display Name" };
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices(o => o.Localizer = localizer);
        var parameterInfo = GetParameter(nameof(RuntimeParameterActions.DisplayAttributeParameter));
        Assert.True(options.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo));
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await validatableInfo.ValidateAsync(null, context, default);

        var call = Assert.Single(localizer.DisplayNameCalls);
        Assert.Equal("Custom Display Name", call.DisplayName);
        Assert.Equal("value", call.MemberName);
        Assert.Null(call.Type);
    }

    [Fact]
    public void TryGetValidatableParameterInfo_WithNullableValueType_ReturnsFalse()
    {
        var (_, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var parameterInfo = typeof(RuntimeParameterHolder).GetMethod(nameof(RuntimeParameterHolder.NullableMethod))!.MakeGenericMethod(typeof(int)).GetParameters()[0];

        Assert.False(options.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo));
        Assert.Null(validatableInfo);
    }

    private static ParameterInfo GetParameter(string methodName)
        => typeof(RuntimeParameterActions).GetMethod(methodName)!.GetParameters()[0];

    private sealed class NullNameParameterInfo : ParameterInfo
    {
        public override string? Name => null;
        public override Type ParameterType => typeof(string);
    }
}

public sealed class UnannotatedRuntimeParameterClass { }

public static class RuntimeParameterHolder
{
    public static void Method<T>(T testParam) { }
    public static void NullableMethod<T>(T? testParam) where T : struct { }
}

public static class RuntimeParameterActions
{
    public static void DisplayAttributeParameter([Display(Name = "Custom Display Name")][Required] string? value) { }
    public static void ResourceDisplayAttributeParameter([Display(Name = "TestKey", ResourceType = typeof(RuntimeParameterResources))][Required] string? value) { }
}

public static class RuntimeParameterResources
{
    public static string TestKey => "Resource Display Name";
}
