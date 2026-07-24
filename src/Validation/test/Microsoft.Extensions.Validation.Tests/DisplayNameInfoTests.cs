// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Microsoft.Extensions.Validation.Tests;

public class DisplayNameInfoTests : ValidationTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_NoDisplayNameInfo_UsesMemberNameDirectly(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<DisplayDefaultModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new DisplayDefaultModel(), context, useAsync, default);

        Assert.Equal("The Name field is required.", context.ValidationErrors!["Name"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_DisplayAttributeName_UsedInErrorMessage(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<DisplayAttributeModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new DisplayAttributeModel(), context, useAsync, default);

        Assert.Equal("The Custom Display Name field is required.", context.ValidationErrors!["Name"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_DisplayNameAttribute_UsedInErrorMessage(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<DisplayNameAttributeModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new DisplayNameAttributeModel(), context, useAsync, default);

        Assert.Equal("The Component Display Name field is required.", context.ValidationErrors!["Name"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TypeLevelAttribute_DisplayAttributeName_UsedInErrorMessage(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<DisplayTypeLevelModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new DisplayTypeLevelModel { Start = 10, End = 5 }, context, useAsync, default);

        Assert.Equal("Display Range is invalid.", Assert.Single(context.ValidationErrors!).Value.Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Parameter_DisplayAttribute_UsedInErrorMessage(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var parameterInfo = typeof(DisplayParameterActions).GetMethod(nameof(DisplayParameterActions.Action))!.GetParameters()[0];
        Assert.True(options.TryGetValidatableParameterInfo(parameterInfo, out var paramInfo));
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(paramInfo, null, context, useAsync, default);

        Assert.Equal("The Parameter Display field is required.", context.ValidationErrors!["value"].Select(e => e.ErrorMessage).Single());
    }
}

[ValidatableType]
public class DisplayDefaultModel
{
    [Required]
    public string? Name { get; set; }
}

[ValidatableType]
public class DisplayAttributeModel
{
    [Display(Name = "Custom Display Name")]
    [Required]
    public string? Name { get; set; }
}

[ValidatableType]
public class DisplayNameAttributeModel
{
    [DisplayName("Component Display Name")]
    [Required]
    public string? Name { get; set; }
}

[Display(Name = "Display Range")]
[DisplayTypeLevel]
[ValidatableType]
public class DisplayTypeLevelModel
{
    public int Start { get; set; }
    public int End { get; set; }
}

public sealed class DisplayTypeLevelAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => new($"{validationContext.DisplayName} is invalid.");
}

public static class DisplayParameterActions
{
    public static void Action([Display(Name = "Parameter Display")][Required] string? value) { }
}
