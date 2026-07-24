// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Microsoft.Extensions.Validation.Tests;

public class ValidationLocalizationIntegrationTests : ValidationTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_NoLocalizer_UsesAttributeDefaults(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedDefaultModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedDefaultModel(), context, useAsync, default);

        Assert.Equal("The Name field is required.", context.ValidationErrors!["Name"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_NoLocalizer_LiteralDisplayNamePassesThrough(bool useAsync)
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedLiteralDisplayModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedLiteralDisplayModel(), context, useAsync, default);

        Assert.Equal("The Customer Name field is required.", context.ValidationErrors!["Name"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_WithLocalizer_BothMethodsCalled(bool useAsync)
    {
        var localizer = new RecordingValidationLocalizer { DisplayNameResult = "Localized Display", ErrorMessageResult = "Localized error" };
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices(o => o.Localizer = localizer);
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedLiteralDisplayModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedLiteralDisplayModel(), context, useAsync, default);

        var displayCall = Assert.Single(localizer.DisplayNameCalls);
        Assert.Equal("Customer Name", displayCall.DisplayName);
        Assert.Equal("Name", displayCall.MemberName);
        Assert.Equal(typeof(LocalizedLiteralDisplayModel), displayCall.Type);
        var errorCall = Assert.Single(localizer.ErrorMessageCalls);
        Assert.Equal("Localized Display", errorCall.DisplayName);
        Assert.Equal("Name", errorCall.MemberName);
        Assert.Equal(typeof(LocalizedLiteralDisplayModel), errorCall.DeclaringType);
        Assert.IsType<RequiredAttribute>(errorCall.Attribute);
        Assert.Equal("Localized error", context.ValidationErrors!["Name"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_LocalizerReturnsNull_FallsBackToLiteral(bool useAsync)
    {
        var localizer = new RecordingValidationLocalizer();
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices(o => o.Localizer = localizer);
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedLiteralDisplayModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedLiteralDisplayModel(), context, useAsync, default);

        Assert.Equal("The Customer Name field is required.", context.ValidationErrors!["Name"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_ErrorMessageResourceType_BypassesLocalizer(bool useAsync)
    {
        var localizer = new RecordingValidationLocalizer { ErrorMessageResult = "Should not be used" };
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices(o => o.Localizer = localizer);
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedResourceErrorModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedResourceErrorModel(), context, useAsync, default);

        Assert.Single(localizer.DisplayNameCalls);
        Assert.Empty(localizer.ErrorMessageCalls);
        Assert.Equal(LocalizedResources.RequiredError, context.ValidationErrors!["Name"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_ResourceDisplayName_BypassesLocalizer(bool useAsync)
    {
        var localizer = new RecordingValidationLocalizer { DisplayNameResult = "Should not be used" };
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices(o => o.Localizer = localizer);
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedResourceDisplayModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedResourceDisplayModel(), context, useAsync, default);

        Assert.Empty(localizer.DisplayNameCalls);
        Assert.Equal("Resource-Resolved Name", Assert.Single(localizer.ErrorMessageCalls).DisplayName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IValidatableObject_ResultsNotProcessedThroughLocalizer(bool useAsync)
    {
        var localizer = new RecordingValidationLocalizer { ErrorMessageResult = "Should not be used" };
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices(o => o.Localizer = localizer);
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedValidatableObjectModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedValidatableObjectModel(), context, useAsync, default);

        Assert.Empty(localizer.ErrorMessageCalls);
        Assert.Equal("Object error", context.ValidationErrors!["Value"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Parameter_LocalizerCalledWithNullDeclaringType(bool useAsync)
    {
        var localizer = new RecordingValidationLocalizer { DisplayNameResult = "Localized Parameter", ErrorMessageResult = "Localized parameter error" };
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices(o => o.Localizer = localizer);
        var parameterInfo = typeof(LocalizedParameterActions).GetMethod(nameof(LocalizedParameterActions.Action))!.GetParameters()[0];
        Assert.True(options.TryGetValidatableParameterInfo(parameterInfo, out var paramInfo));
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(paramInfo, null, context, useAsync, default);

        Assert.Null(Assert.Single(localizer.DisplayNameCalls).Type);
        Assert.Null(Assert.Single(localizer.ErrorMessageCalls).DeclaringType);
        Assert.Equal("Localized parameter error", context.ValidationErrors!["value"].Select(e => e.ErrorMessage).Single());
    }
}

[ValidatableType]
public class LocalizedDefaultModel
{
    [Required]
    public string? Name { get; set; }
}

[ValidatableType]
public class LocalizedLiteralDisplayModel
{
    [Display(Name = "Customer Name")]
    [Required]
    public string? Name { get; set; }
}

[ValidatableType]
public class LocalizedResourceErrorModel
{
    [Display(Name = "Customer Name")]
    [Required(ErrorMessageResourceType = typeof(LocalizedResources), ErrorMessageResourceName = nameof(LocalizedResources.RequiredError))]
    public string? Name { get; set; }
}

[ValidatableType]
public class LocalizedResourceDisplayModel
{
    [Display(Name = nameof(LocalizedResources.DisplayName), ResourceType = typeof(LocalizedResources))]
    [Required]
    public string? Name { get; set; }
}

public static class LocalizedResources
{
    public static string RequiredError => "Resource required error";
    public static string DisplayName => "Resource-Resolved Name";
}

[ValidatableType]
public class LocalizedValidatableObjectModel : IValidatableObject
{
    public string? Value { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        yield return new ValidationResult("Object error", [nameof(Value)]);
    }
}

public static class LocalizedParameterActions
{
    public static void Action([Display(Name = "Parameter Name")][Required] string? value) { }
}
