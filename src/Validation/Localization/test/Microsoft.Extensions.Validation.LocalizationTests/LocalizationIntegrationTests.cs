#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation.Localization;
using Microsoft.Extensions.Validation.LocalizationTests.Helpers;

namespace Microsoft.Extensions.Validation.LocalizationTests;

public class LocalizationIntegrationTests
{
    [Fact]
    public async Task Localization_TranslatesRequiredMessage_WithStringLocalizer()
    {
        var translations = new Dictionary<string, string>
        {
            ["RequiredError"] = "Le champ {0} est obligatoire."
        };
        var factory = new TestStringLocalizerFactory(translations);
        var formatterProvider = new ValidationAttributeFormatterProvider();

        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions());
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions();
        configureOptions.Configure(validationOptions);

        var model = new IntegrationCustomer { Name = null, Age = 25 };
        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(model)
        };

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Le champ Name est obligatoire.", context.ValidationErrors["Name"].First());
    }

    [Fact]
    public async Task Localization_TranslatesDisplayName_InErrorMessage()
    {
        var translations = new Dictionary<string, string>
        {
            ["Customer Age"] = "Âge du client",
            ["RangeError"] = "Le champ {0} doit être entre {1} et {2}."
        };
        var factory = new TestStringLocalizerFactory(translations);
        var formatterProvider = new ValidationAttributeFormatterProvider();

        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions());
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions();
        configureOptions.Configure(validationOptions);

        var model = new IntegrationCustomer { Name = "Test", Age = 200 };
        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(model)
        };

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Le champ Âge du client doit être entre 18 et 120.", context.ValidationErrors["Age"].First());
    }

    [Fact]
    public async Task Localization_TranslatesRangeMessage_WithFormatArgs()
    {
        var translations = new Dictionary<string, string>
        {
            ["RangeError"] = "{0}: valeur entre {1} et {2} attendue."
        };
        var factory = new TestStringLocalizerFactory(translations);
        var formatterProvider = new ValidationAttributeFormatterProvider();

        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions());
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions();
        configureOptions.Configure(validationOptions);

        var model = new IntegrationCustomer { Name = "Test", Age = -5 };
        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(model)
        };

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Contains("valeur entre 18 et 120 attendue", context.ValidationErrors["Age"].First());
    }

    [Fact]
    public async Task Localization_FallsBackToExplicitMessage_WhenTranslationNotFound()
    {
        var translations = new Dictionary<string, string>();
        var factory = new TestStringLocalizerFactory(translations);
        var formatterProvider = new ValidationAttributeFormatterProvider();

        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions());
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions();
        configureOptions.Configure(validationOptions);

        var model = new IntegrationCustomer { Name = null, Age = 25 };
        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(model)
        };

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("RequiredError", context.ValidationErrors["Name"].First());
    }

    [Fact]
    public async Task Localization_UsesResourceProperty_WhenResourceTypeIsSet()
    {
        var factory = new TestStringLocalizerFactory([]);
        var formatterProvider = new ValidationAttributeFormatterProvider();

        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions());
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions();
        configureOptions.Configure(validationOptions);

        var requiredAttr = new RequiredAttribute
        {
            ErrorMessageResourceType = typeof(IntegrationResources),
            ErrorMessageResourceName = nameof(IntegrationResources.RequiredError)
        };
        var propInfo = new TestValidatablePropertyInfo(
            typeof(ResourceModel), typeof(string), "Value",
            [requiredAttr]);
        var typeInfo = new TestValidatableTypeInfo(typeof(ResourceModel), [propInfo]);

        var model = new ResourceModel { Value = null };
        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(model)
        };

        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal(IntegrationResources.RequiredError, context.ValidationErrors["Value"].First());

    }

    [Fact]
    public async Task Localization_CustomAttribute_LocalizedViaProvider()
    {
        var translations = new Dictionary<string, string>
        {
            ["Only letters are allowed."] = "Seules les lettres sont autorisées."
        };
        var factory = new TestStringLocalizerFactory(translations);
        var formatterProvider = new ValidationAttributeFormatterProvider();

        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions());
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions();
        configureOptions.Configure(validationOptions);

        var propInfo = new TestValidatablePropertyInfo(
            typeof(CodeModel), typeof(string), "Code",
            [new AlphaOnlyAttribute { ErrorMessage = "Only letters are allowed." }]);
        var typeInfo = new TestValidatableTypeInfo(typeof(CodeModel), [propInfo]);

        var model = new CodeModel { Code = "abc123" };
        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(model)
        };

        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Seules les lettres sont autorisées.", context.ValidationErrors["Code"].First());
    }

    [Fact]
    public async Task Localization_TypeLevelAttribute_Localized()
    {
        var translations = new Dictionary<string, string>
        {
            ["Start must be less than End."] = "Le début doit être inférieur à la fin."
        };
        var factory = new TestStringLocalizerFactory(translations);
        var formatterProvider = new ValidationAttributeFormatterProvider();

        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions());
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions();
        configureOptions.Configure(validationOptions);

        var typeInfo = new TestValidatableTypeInfo(
            typeof(RangeModel),
            [
                new TestValidatablePropertyInfo(typeof(RangeModel), typeof(int), "Start", []),
                new TestValidatablePropertyInfo(typeof(RangeModel), typeof(int), "End", [])
            ],
            validationAttributes: [new StartLessThanEndAttribute { ErrorMessage = "Start must be less than End." }]);

        var model = new RangeModel { Start = 10, End = 5 };
        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(model)
        };

        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        var errors = context.ValidationErrors.Values.SelectMany(v => v).ToList();
        Assert.Contains("Le début doit être inférieur à la fin.", errors);
    }

    [Fact]
    public async Task Localization_IValidatableObject_NotAffectedByProvider()
    {
        var translations = new Dictionary<string, string>
        {
            ["The {0} field is required."] = "Le champ {0} est obligatoire.",
            ["Custom IValidatableObject error"] = "Should not translate this"
        };
        var factory = new TestStringLocalizerFactory(translations);
        var formatterProvider = new ValidationAttributeFormatterProvider();

        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions());
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions();
        configureOptions.Configure(validationOptions);

        var typeInfo = new TestValidatableTypeInfo(
            typeof(ValidatableModel),
            [
                new TestValidatablePropertyInfo(typeof(ValidatableModel), typeof(string), "Name",
                    [new RequiredAttribute()])
            ]);

        var model = new ValidatableModel { Name = "Test" };
        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(model)
        };

        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Contains("Custom IValidatableObject error", context.ValidationErrors.Values.SelectMany(v => v));
    }

    [Fact]
    public async Task Localization_PerInvocationOverride_WorksCorrectly()
    {
        var translations = new Dictionary<string, string>
        {
            ["The {0} field is required."] = "Options: {0} required"
        };
        var factory = new TestStringLocalizerFactory(translations);
        var formatterProvider = new ValidationAttributeFormatterProvider();

        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions());
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions();
        configureOptions.Configure(validationOptions);

        var model = new SimpleModel { Name = null };
        var propInfo = new TestValidatablePropertyInfo(
            typeof(SimpleModel), typeof(string), "Name", [new RequiredAttribute()]);
        var typeInfo = new TestValidatableTypeInfo(typeof(SimpleModel), [propInfo]);

        validationOptions.ErrorMessageProvider = (in ctx) => $"Override: {ctx.MemberName} needed";
        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(model),
        };

        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Override: Name needed", context.ValidationErrors["Name"].First());
    }

    private static TestValidatableTypeInfo CreateCustomerTypeInfo()
    {
        return new TestValidatableTypeInfo(
            typeof(IntegrationCustomer),
            [
                new TestValidatablePropertyInfo(typeof(IntegrationCustomer), typeof(string), "Name",
                    [new RequiredAttribute() { ErrorMessage = "RequiredError" }, new StringLengthAttribute(100)]),
                new TestValidatablePropertyInfo(typeof(IntegrationCustomer), typeof(int), "Age",
                    [new RangeAttribute(18, 120) { ErrorMessage = "RangeError" } ],
                    displayName: "Customer Age"),
                new TestValidatablePropertyInfo(typeof(IntegrationCustomer), typeof(string), "Email",
                    [new EmailAddressAttribute()])
            ]);
    }

    public class IntegrationCustomer
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? Email { get; set; }
    }

    private class ResourceModel
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

    private class SimpleModel
    {
        public string? Name { get; set; }
    }

    private class ValidatableModel : IValidatableObject
    {
        public string? Name { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            yield return new ValidationResult("Custom IValidatableObject error", ["Name"]);
        }
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

    private class TestStringLocalizerFactory(Dictionary<string, string> translations) : IStringLocalizerFactory
    {
        private readonly Dictionary<string, string> _translations = translations;

        public IStringLocalizer Create(Type resourceSource)
            => new TestStringLocalizer(_translations);

        public IStringLocalizer Create(string baseName, string location)
            => new TestStringLocalizer(_translations);
    }
}
