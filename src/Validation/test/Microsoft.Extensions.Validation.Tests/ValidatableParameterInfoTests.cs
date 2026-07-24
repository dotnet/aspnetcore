// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Microsoft.Extensions.Validation.Tests;

public class ValidatableParameterInfoTests : ValidationTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_RequiredParameter_AddsErrorWhenNull(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var parameterInfo = GetParameter(nameof(ParameterActions.RequiredParameter));
        Assert.True(options.TryGetValidatableParameterInfo(parameterInfo, out var paramInfo));
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(paramInfo, null, context, useAsync, default);

        var error = Assert.Single(context.ValidationErrors!);
        Assert.Equal("testParam", error.Key);
        Assert.Equal("The Test Parameter field is required.", error.Value.Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_RequiredParameter_ShortCircuitsOtherValidations(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var parameterInfo = GetParameter(nameof(ParameterActions.RequiredAndCustomParameter));
        Assert.True(options.TryGetValidatableParameterInfo(parameterInfo, out var paramInfo));
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(paramInfo, null, context, useAsync, default);

        var error = Assert.Single(context.ValidationErrors!);
        Assert.Equal("testParam", error.Key);
        Assert.Equal("The Test Parameter field is required.", error.Value.Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_SkipsValidation_WhenNullAndNotRequired(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var parameterInfo = GetParameter(nameof(ParameterActions.StringLengthParameter));
        Assert.True(options.TryGetValidatableParameterInfo(parameterInfo, out var paramInfo));
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(paramInfo, null, context, useAsync, default);

        Assert.True(context.ValidationErrors is null || context.ValidationErrors.Count == 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_WithRangeAttribute_ValidatesCorrectly(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var parameterInfo = GetParameter(nameof(ParameterActions.RangeParameter));
        Assert.True(options.TryGetValidatableParameterInfo(parameterInfo, out var paramInfo));
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(paramInfo, 5, context, useAsync, default);

        var error = Assert.Single(context.ValidationErrors!);
        Assert.Equal("testParam", error.Key);
        Assert.Equal("The field Test Parameter must be between 10 and 100.", error.Value.Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_WhenValidatableTypeHasErrors_AddsNestedErrors(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var parameterInfo = GetParameter(nameof(ParameterActions.PersonParameter));
        Assert.True(options.TryGetValidatableParameterInfo(parameterInfo, out var paramInfo));
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(paramInfo, new ParameterPerson(), context, useAsync, default);

        var error = Assert.Single(context.ValidationErrors!);
        Assert.Equal("Name", error.Key);
        Assert.Equal("The Name field is required.", error.Value.Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_WithEnumerableOfValidatableType_ValidatesEachItem(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var parameterInfo = GetParameter(nameof(ParameterActions.PeopleParameter));
        Assert.True(options.TryGetValidatableParameterInfo(parameterInfo, out var paramInfo));
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(paramInfo, new[] { new ParameterPerson { Name = "Valid" }, new ParameterPerson() }, context, useAsync, default);

        var error = Assert.Single(context.ValidationErrors!);
        Assert.Equal("people[1].Name", error.Key);
        Assert.Equal("The Name field is required.", error.Value.Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_MultipleErrorsOnSameParameter_CollectsAllErrors(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var parameterInfo = GetParameter(nameof(ParameterActions.MultipleErrorsParameter));
        Assert.True(options.TryGetValidatableParameterInfo(parameterInfo, out var paramInfo));
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(paramInfo, 5, context, useAsync, default);

        var errors = Assert.Single(context.ValidationErrors!).Value.Select(e => e.ErrorMessage).ToArray();
        Assert.Contains("Range error", errors);
        Assert.Contains("Custom error", errors);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_WithContextPrefix_AddsErrorsWithCorrectPrefix(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var parameterInfo = GetParameter(nameof(ParameterActions.RangeParameter));
        Assert.True(options.TryGetValidatableParameterInfo(parameterInfo, out var paramInfo));
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);
        context.CurrentValidationPath = "parent";

        await ValidateAsync(paramInfo, 5, context, useAsync, default);

        var error = Assert.Single(context.ValidationErrors!);
        Assert.Equal("parent.testParam", error.Key);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_ExceptionDuringValidation_Rethrown(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var parameterInfo = GetParameter(nameof(ParameterActions.ThrowingParameter));
        Assert.True(options.TryGetValidatableParameterInfo(parameterInfo, out var paramInfo));
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => ValidateAsync(paramInfo, "test", context, useAsync, default));

        Assert.Equal("Test exception", ex.Message);
        Assert.True(context.ValidationErrors is null || context.ValidationErrors.Count == 0);
    }

    private static ParameterInfo GetParameter(string methodName)
        => typeof(ParameterActions).GetMethod(methodName)!.GetParameters()[0];
}

public static class ParameterActions
{
    public static void RequiredParameter([Display(Name = "Test Parameter")][Required] string? testParam) { }
    public static void RequiredAndCustomParameter([Display(Name = "Test Parameter")][Required][AlwaysFailsValidation] string? testParam) { }
    public static void StringLengthParameter([Display(Name = "Test Parameter")][StringLength(10)] string? testParam) { }
    public static void RangeParameter([Display(Name = "Test Parameter")][Range(10, 100)] int testParam) { }
    public static void PersonParameter(ParameterPerson person) { }
    public static void PeopleParameter([Required] IEnumerable<ParameterPerson> people) { }
    public static void MultipleErrorsParameter([Display(Name = "Test Parameter")][Range(10, 100, ErrorMessage = "Range error")][AlwaysFailsValidation(ErrorMessage = "Custom error")] int testParam) { }
    public static void ThrowingParameter([ThrowingValidation] string testParam) { }
}

[ValidatableType]
public sealed class ParameterPerson
{
    [Required]
    public string? Name { get; set; }
}

public sealed class AlwaysFailsValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => false;
}

public sealed class ThrowingValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => throw new InvalidOperationException("Test exception");
}
