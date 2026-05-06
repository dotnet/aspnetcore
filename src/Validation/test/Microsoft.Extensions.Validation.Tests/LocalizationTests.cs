#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation.Localization;

namespace Microsoft.Extensions.Validation.Tests;

public class LocalizationTests
{
    // --- Auto-detection / DI integration tests ---

    // TODO

    // --- Display name localization tests ---

    [Fact]
    public async Task DisplayName_NoLocalization_UsesDefaultName()
    {
        var model = new CustomerModel { Name = null, Age = 25 };
        var context = CreateContext(model);

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The Name field is required.", context.ValidationErrors["Name"].First());
    }

    [Fact]
    public async Task DisplayName_Localized_AppearsInErrorMessage()
    {
        var translations = new Dictionary<string, string>
        {
            ["Customer Age"] = "Âge du client"
        };
        var model = new CustomerModel { Name = "Test", Age = 200 };
        var options = new ValidationOptions();
        var context = CreateContext(model, options, translations);

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The field Âge du client must be between 18 and 120.", context.ValidationErrors["Age"].First());
    }

    [Fact]
    public async Task DisplayName_NotFound_FallsBackToAttributeName()
    {
        var model = new CustomerModel { Name = "Test", Age = 200 };
        var context = CreateContext(model);

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        // "Customer Age" is the DisplayAttribute.Name - not localized, used as-is
        Assert.Equal("The field Customer Age must be between 18 and 120.", context.ValidationErrors["Age"].First());
    }

    [Fact]
    public async Task DisplayName_WithResourceType_SkipsLocalization()
    {
        // TODO
    }

    // --- Error message localization tests ---

    [Fact]
    public async Task ErrorMessage_WithLocalizerKey_TranslatesMessage()
    {
        var translations = new Dictionary<string, string>
        {
            ["RequiredError"] = "Le champ {0} est obligatoire."
        };
        var model = new IntegrationCustomer { Name = null, Age = 25 };
        var context = CreateContext(model, null, translations);

        var typeInfo = CreateCustomerTypeInfoWithErrorKeys();
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Le champ Name est obligatoire.", context.ValidationErrors["Name"].First());
    }

    [Fact]
    public async Task ErrorMessage_WithRangeAttribute_FormatsArgs()
    {
        var translations = new Dictionary<string, string>
        {
            ["RangeError"] = "{0}: valeur entre {1} et {2} attendue."
        };
        var model = new IntegrationCustomer { Name = "Test", Age = -5 };
        var context = CreateContext(model, null, translations);

        var typeInfo = CreateCustomerTypeInfoWithErrorKeys();
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Contains("valeur entre 18 et 120 attendue", context.ValidationErrors["Age"].First());
    }

    [Fact]
    public async Task ErrorMessage_TranslationNotFound_FallsBackToDefault()
    {
        var model = new IntegrationCustomer { Name = null, Age = 25 };
        var context = CreateContext(model);

        var typeInfo = CreateCustomerTypeInfoWithErrorKeys();
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        // ErrorMessage is "RequiredError" (the key) - localizer can't find it, so attribute
        // formats with its own template using "RequiredError" as the ErrorMessage
        Assert.Equal("RequiredError", context.ValidationErrors["Name"].First());
    }

    [Fact]
    public async Task ErrorMessage_WithResourceType_SkipsLocalization()
    {
        var model = new ResourceModel { Value = null };
        var requiredAttr = new RequiredAttribute
        {
            ErrorMessageResourceType = typeof(IntegrationResources),
            ErrorMessageResourceName = nameof(IntegrationResources.RequiredError)
        };
        var propInfo = new TestValidatablePropertyInfo(
            typeof(ResourceModel), typeof(string), "Value", [requiredAttr]);
        var typeInfo = new TestValidatableTypeInfo(typeof(ResourceModel), [propInfo]);

        var translations = new Dictionary<string, string>
        {
            ["This field is required."] = "Should not use this"
        };
        var context = CreateContext(model, null, translations);
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal(IntegrationResources.RequiredError, context.ValidationErrors["Value"].First());
    }

    [Fact]
    public async Task ErrorMessage_CustomAttribute_LocalizedViaErrorMessage()
    {
        var translations = new Dictionary<string, string>
        {
            ["Only letters are allowed."] = "Seules les lettres sont autorisées."
        };
        var propInfo = new TestValidatablePropertyInfo(
            typeof(CodeModel), typeof(string), "Code",
            [new AlphaOnlyAttribute { ErrorMessage = "Only letters are allowed." }]);
        var typeInfo = new TestValidatableTypeInfo(typeof(CodeModel), [propInfo]);

        var model = new CodeModel { Code = "abc123" };
        var context = CreateContext(model, null, translations);
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Seules les lettres sont autorisées.", context.ValidationErrors["Code"].First());
    }

    [Fact]
    public async Task ErrorMessage_TypeLevelAttribute_Localized()
    {
        var translations = new Dictionary<string, string>
        {
            ["Start must be less than End."] = "Le début doit être inférieur à la fin."
        };
        var typeInfo = new TestValidatableTypeInfo(
            typeof(RangeModel),
            [
                new TestValidatablePropertyInfo(typeof(RangeModel), typeof(int), "Start", []),
                new TestValidatablePropertyInfo(typeof(RangeModel), typeof(int), "End", [])
            ],
            attributes: [new StartLessThanEndAttribute { ErrorMessage = "Start must be less than End." }]);

        var model = new RangeModel { Start = 10, End = 5 };
        var context = CreateContext(model, null, translations);
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        var errors = context.ValidationErrors.Values.SelectMany(v => v).ToList();
        Assert.Contains("Le début doit être inférieur à la fin.", errors);
    }

    [Fact]
    public async Task ErrorMessage_IValidatableObject_NotAffected()
    {
        var translations = new Dictionary<string, string>
        {
            ["Custom IValidatableObject error"] = "Should not translate this"
        };
        var typeInfo = new TestValidatableTypeInfo(
            typeof(ValidatableModel),
            [
                new TestValidatablePropertyInfo(typeof(ValidatableModel), typeof(string), "Name",
                    [new RequiredAttribute()])
            ]);

        var model = new ValidatableModel { Name = "Test" };
        var context = CreateContext(model, null, translations);
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Contains("Custom IValidatableObject error", context.ValidationErrors.Values.SelectMany(v => v));
    }

    // --- ErrorMessageKeyProvider tests ---

    [Fact]
    public async Task ErrorMessageKeyProvider_UsedForAttributesWithoutErrorMessage()
    {
        var translations = new Dictionary<string, string>
        {
            ["RequiredAttribute_Error"] = "Custom key: {0} needed"
        };
        var options = new ValidationOptions
        {
            ErrorMessageKeyProvider = ctx => $"{ctx.Attribute.GetType().Name}_Error"
        };

        var model = new SimpleModel { Name = null };
        var propInfo = new TestValidatablePropertyInfo(
            typeof(SimpleModel), typeof(string), "Name", [new RequiredAttribute()]);
        var typeInfo = new TestValidatableTypeInfo(typeof(SimpleModel), [propInfo]);

        var context = CreateContext(model, options, translations);
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Custom key: Name needed", context.ValidationErrors["Name"].First());
    }

    [Fact]
    public async Task ErrorMessageKeyProvider_ReturnsNull_SkipsLocalization()
    {
        var translations = new Dictionary<string, string>
        {
            ["The {0} field is required."] = "Should not appear"
        };
        var options = new ValidationOptions
        {
            ErrorMessageKeyProvider = _ => null
        };

        var model = new SimpleModel { Name = null };
        var propInfo = new TestValidatablePropertyInfo(
            typeof(SimpleModel), typeof(string), "Name", [new RequiredAttribute()]);
        var typeInfo = new TestValidatableTypeInfo(typeof(SimpleModel), [propInfo]);

        var context = CreateContext(model, options, translations);
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        // Falls back to attribute's default message
        Assert.Equal("The Name field is required.", context.ValidationErrors["Name"].First());
    }

    // --- LocalizerProvider tests ---

    // TODO

    // --- AddValidationAttributeFormatter tests ---

    [Fact]
    public void AddValidationAttributeFormatter_RegistersCustomFormatter()
    {
        var services = new ServiceCollection();
        services.AddValidation();
        services.AddValidationAttributeFormatter<RequiredAttribute>(
            _ => new TestFormatter());

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        var formatter = options.AttributeFormatters.GetFormatter(new RequiredAttribute());
        Assert.NotNull(formatter);
        Assert.IsType<TestFormatter>(formatter);
    }

    // --- Helpers ---

    private static ValidateContext CreateContext(object model, ValidationOptions? options = null, Dictionary<string, string>? translations = null)
    {
        options ??= new();
        var factory = new TestStringLocalizerFactory(translations ?? []);
        var localizer = new ValidationLocalizer(options, factory);

        return new ValidateContext
        {
            ValidationOptions = options,
            ValidationContext = new ValidationContext(model),
            ValidationLocalizer = localizer,
        };
    }

    private static TestValidatableTypeInfo CreateCustomerTypeInfo()
    {
        return new TestValidatableTypeInfo(
            typeof(CustomerModel),
            [
                new TestValidatablePropertyInfo(typeof(CustomerModel), typeof(string), "Name",
                    [new RequiredAttribute(), new StringLengthAttribute(100)]),
                new TestValidatablePropertyInfo(typeof(CustomerModel), typeof(int), "Age",
                    [new RangeAttribute(18, 120)],
                    displayName: "Customer Age"),
                new TestValidatablePropertyInfo(typeof(CustomerModel), typeof(string), "Email",
                    [new EmailAddressAttribute()])
            ]);
    }

    private static TestValidatableTypeInfo CreateCustomerTypeInfoWithErrorKeys()
    {
        return new TestValidatableTypeInfo(
            typeof(IntegrationCustomer),
            [
                new TestValidatablePropertyInfo(typeof(IntegrationCustomer), typeof(string), "Name",
                    [new RequiredAttribute { ErrorMessage = "RequiredError" }, new StringLengthAttribute(100)]),
                new TestValidatablePropertyInfo(typeof(IntegrationCustomer), typeof(int), "Age",
                    [new RangeAttribute(18, 120) { ErrorMessage = "RangeError" }],
                    displayName: "Customer Age"),
                new TestValidatablePropertyInfo(typeof(IntegrationCustomer), typeof(string), "Email",
                    [new EmailAddressAttribute()])
            ]);
    }

    // --- Test doubles ---

    private class TestStringLocalizerFactory(Dictionary<string, string> translations) : IStringLocalizerFactory
    {
        public IStringLocalizer Create(Type resourceSource)
            => new TestStringLocalizer(translations);

        public IStringLocalizer Create(string baseName, string location)
            => new TestStringLocalizer(translations);
    }

    private class TestStringLocalizer(Dictionary<string, string> translations) : IStringLocalizer
    {
        public LocalizedString this[string name] => translations.TryGetValue(name, out var value)
            ? new LocalizedString(name, value, resourceNotFound: false)
            : new LocalizedString(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments] => translations.TryGetValue(name, out var value)
            ? new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, value, arguments), resourceNotFound: false)
            : new LocalizedString(name, name, resourceNotFound: true);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            translations.Select(kvp => new LocalizedString(kvp.Key, kvp.Value, false));
    }

    private class TestFormatter : IValidationAttributeFormatter
    {
        public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
            => $"TestFormatted: {displayName}";
    }

    private class TestValidatablePropertyInfo(
        Type declaringType,
        Type propertyType,
        string name,
        ValidationAttribute[] validationAttributes,
        string? displayName = null) : ValidatablePropertyInfo(declaringType, propertyType, name, displayName ?? name)
    {
        protected override ValidationAttribute[] GetValidationAttributes() => validationAttributes;
    }

    // --- Model classes ---

    public class CustomerModel
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? Email { get; set; }
    }

    public class IntegrationCustomer
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? Email { get; set; }
    }

    private class SimpleModel
    {
        public string? Name { get; set; }
    }

    private class ResourceModel
    {
        public string? Value { get; set; }
    }

    private class ResourceDisplayModel
    {
        public string? Value { get; set; }
    }

    private class CodeModel
    {
        public string? Code { get; set; }
    }

    private class RangeModel
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    private class ValidatableModel : IValidatableObject
    {
        public string? Name { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            yield return new ValidationResult("Custom IValidatableObject error", ["Name"]);
        }
    }

    internal static class DisplayResources
    {
        public static string ValueLabel => "The Value";
    }

    internal static class IntegrationResources
    {
        public static string RequiredError => "Resource: This field is required.";
    }

    private class AlphaOnlyAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string s && s.Any(c => !char.IsLetter(c)))
            {
                return new ValidationResult(ErrorMessage ?? "Only letters are allowed.");
            }

            return ValidationResult.Success;
        }
    }

    private class StartLessThanEndAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is RangeModel model && model.Start >= model.End)
            {
                return new ValidationResult(
                    ErrorMessage ?? "Start must be less than End.",
                    [nameof(RangeModel.Start)]);
            }

            return ValidationResult.Success;
        }
    }
}
