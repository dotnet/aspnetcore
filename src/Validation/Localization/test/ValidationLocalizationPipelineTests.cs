#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation.Tests;

namespace Microsoft.Extensions.Validation.Localization.Tests;

/// <summary>
/// Pipeline tests verifying that <see cref="ValidationLocalizationServiceCollectionExtensions.AddValidationLocalization"/>
/// produces a fully functional pipeline: validation runs, picks up the configured
/// <see cref="IStringLocalizerFactory"/>, and produces localized error messages.
/// </summary>
public class ValidationLocalizationPipelineTests : ValidationTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_ProducesLocalizedErrorMessage(bool useAsync)
    {
        var translations = new Dictionary<string, string>
        {
            ["RequiredKey"] = "Le champ {0} est obligatoire.",
            ["Customer Name"] = "Nom du client",
        };
        var context = SetupPipeline(translations);
        var model = new CustomerModel { Name = null };
        var customerTypeInfo = new TestValidatableTypeInfo(typeof(CustomerModel),
        [
            new TestValidatablePropertyInfo(typeof(CustomerModel), typeof(string), "Name",
                [new RequiredAttribute { ErrorMessage = "RequiredKey" }],
                displayName: "Customer Name")
        ]);

        await ValidateAsync(customerTypeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Le champ Nom du client est obligatoire.", context.ValidationErrors["Name"].Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_RangeAttribute_FormatsMinMaxIntoLocalizedTemplate(bool useAsync)
    {
        var translations = new Dictionary<string, string>
        {
            ["RangeKey"] = "{0}: valeur entre {1} et {2} attendue.",
        };
        var context = SetupPipeline(translations);
        var model = new CustomerModel { Age = -5 };
        var typeInfo = new TestValidatableTypeInfo(typeof(CustomerModel),
        [
            new TestValidatablePropertyInfo(typeof(CustomerModel), typeof(int), "Age",
                [new RangeAttribute(18, 120) { ErrorMessage = "RangeKey" }])
        ]);

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Age: valeur entre 18 et 120 attendue.", context.ValidationErrors["Age"].Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_ErrorMessageResourceType_BypassesLocalization(bool useAsync)
    {
        // Regression: ErrorMessageResourceType-based localization on the attribute itself
        // must NOT be overridden by the validation localizer.
        var translations = new Dictionary<string, string>
        {
            ["This field is required."] = "Should NOT be used",
        };
        var context = SetupPipeline(translations);
        var requiredAttr = new RequiredAttribute
        {
            ErrorMessageResourceType = typeof(IntegrationResources),
            ErrorMessageResourceName = nameof(IntegrationResources.RequiredError),
        };
        var typeInfo = new TestValidatableTypeInfo(typeof(CustomerModel),
        [
            new TestValidatablePropertyInfo(typeof(CustomerModel), typeof(string), "Name", [requiredAttr])
        ]);
        var model = new CustomerModel { Name = null };

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal(IntegrationResources.RequiredError, context.ValidationErrors["Name"].Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_ErrorMessageKeyProvider_LocalizesAttributesWithoutErrorMessage(bool useAsync)
    {
        var translations = new Dictionary<string, string>
        {
            ["RequiredAttribute_Default"] = "Field {0} is required (convention).",
        };
        var context = SetupPipeline(translations, options =>
        {
            options.ErrorMessageKeyProvider = ctx => $"{ctx.Attribute.GetType().Name}_Default";
        });

        var typeInfo = new TestValidatableTypeInfo(typeof(CustomerModel),
        [
            new TestValidatablePropertyInfo(typeof(CustomerModel), typeof(string), "Name",
                [new RequiredAttribute()])
        ]);
        var model = new CustomerModel { Name = null };

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Field Name is required (convention).", context.ValidationErrors["Name"].Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_TypeLevelAttribute_Localized(bool useAsync)
    {
        var translations = new Dictionary<string, string>
        {
            ["StartLessThanEndKey"] = "Le début doit être inférieur à la fin.",
        };
        var context = SetupPipeline(translations);
        var typeInfo = new TestValidatableTypeInfo(
            typeof(RangeModel),
            [
                new TestValidatablePropertyInfo(typeof(RangeModel), typeof(int), "Start", []),
                new TestValidatablePropertyInfo(typeof(RangeModel), typeof(int), "End", [])
            ],
            attributes:
            [
                new StartLessThanEndAttribute { ErrorMessage = "StartLessThanEndKey" }
            ]);
        var model = new RangeModel { Start = 10, End = 5 };

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        var errors = context.ValidationErrors.Values.SelectMany(v => v).ToList();
        Assert.Contains("Le début doit être inférieur à la fin.", errors);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_LocalizationLookupMiss_FallsBackToAttributeDefault(bool useAsync)
    {
        // No translation for the key → localizer returns null → pipeline falls back to
        // the attribute's default ErrorMessage value (the literal "RequiredKey").
        var context = SetupPipeline(translations: []);
        var typeInfo = new TestValidatableTypeInfo(typeof(CustomerModel),
        [
            new TestValidatablePropertyInfo(typeof(CustomerModel), typeof(string), "Name",
                [new RequiredAttribute { ErrorMessage = "RequiredKey" }])
        ]);
        var model = new CustomerModel { Name = null };

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("RequiredKey", context.ValidationErrors["Name"].Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_DisplayNameLookup_LocalizesIntoDefaultErrorTemplate(bool useAsync)
    {
        // Localize only the DisplayName; the error template comes from the attribute's default
        // ("The {0} field is required."), so the localized DisplayName ends up substituted in.
        var translations = new Dictionary<string, string>
        {
            ["Customer Name"] = "Nom du client",
        };
        var context = SetupPipeline(translations);
        var typeInfo = new TestValidatableTypeInfo(typeof(CustomerModel),
        [
            new TestValidatablePropertyInfo(typeof(CustomerModel), typeof(string), "Name",
                [new RequiredAttribute()],
                displayName: "Customer Name")
        ]);
        var model = new CustomerModel { Name = null };

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The Nom du client field is required.", context.ValidationErrors["Name"].Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_ParameterLevel_SharedResource_Localized(bool useAsync)
    {
        // Recommended pattern for Minimal API parameter validation: a shared-resource provider.
        var sharedTranslations = new Dictionary<Type, Dictionary<string, string>>
        {
            [typeof(SharedValidationMessages)] = new()
            {
                ["RequiredKey"] = "Param {0} requis.",
                ["Param Display"] = "Paramètre",
            },
        };
        var context = SetupPipelinePerType(sharedTranslations, options =>
        {
            options.LocalizerProvider = (_, factory) => factory.Create(typeof(SharedValidationMessages));
        });
        var paramInfo = new TestValidatableParameterInfo(typeof(string), "myParam", "Param Display",
            [new RequiredAttribute { ErrorMessage = "RequiredKey" }]);

        await ValidateAsync(paramInfo, null, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Param Paramètre requis.", context.ValidationErrors["myParam"].Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_StringLength_MaxOnly_FormatsLengthIntoTemplate(bool useAsync)
    {
        // StringLengthAttribute with no MinimumLength: BCL default template uses {0} and {1}=Max.
        // The StringLengthAttributeFormatter must pass MaximumLength as {1} for the localized template.
        var translations = new Dictionary<string, string>
        {
            ["MaxLengthKey"] = "{0} doit avoir au plus {1} caractères.",
        };
        var context = SetupPipeline(translations);
        var typeInfo = new TestValidatableTypeInfo(typeof(CustomerModel),
        [
            new TestValidatablePropertyInfo(typeof(CustomerModel), typeof(string), "Name",
                [new StringLengthAttribute(10) { ErrorMessage = "MaxLengthKey" }])
        ]);
        var model = new CustomerModel { Name = new string('a', 50) };

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Name doit avoir au plus 10 caractères.", context.ValidationErrors["Name"].Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_StringLength_MinAndMax_FormatsBothIntoTemplate(bool useAsync)
    {
        // StringLengthAttribute with MinimumLength: BCL default template uses {0}, {1}=Max, {2}=Min.
        // The StringLengthAttributeFormatter must preserve that ordering for the localized template.
        var translations = new Dictionary<string, string>
        {
            ["StringLengthRangeKey"] = "{0} doit avoir entre {2} et {1} caractères.",
        };
        var context = SetupPipeline(translations);
        var typeInfo = new TestValidatableTypeInfo(typeof(CustomerModel),
        [
            new TestValidatablePropertyInfo(typeof(CustomerModel), typeof(string), "Name",
                [new StringLengthAttribute(10) { MinimumLength = 3, ErrorMessage = "StringLengthRangeKey" }])
        ]);
        var model = new CustomerModel { Name = "ab" };

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Name doit avoir entre 3 et 10 caractères.", context.ValidationErrors["Name"].Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_PerInvocationLocalizerOverride_LatestValueWins(bool useAsync)
    {
        // After AddValidationLocalization configures options.Localizer, the user can replace it
        // by direct assignment. The pipeline must read the latest value at validation time,
        // not a value captured at configuration time.
        var context = SetupPipeline(translations: []);
        var override1 = new ConstantLocalizer("FROM-OVERRIDE-1");
        var override2 = new ConstantLocalizer("FROM-OVERRIDE-2");
        var typeInfo = new TestValidatableTypeInfo(typeof(CustomerModel),
        [
            new TestValidatablePropertyInfo(typeof(CustomerModel), typeof(string), "Name",
                [new RequiredAttribute()])
        ]);

        context.ValidationOptions.Localizer = override1;

        await ValidateAsync(typeInfo, new CustomerModel { Name = null }, context, useAsync, default);

        Assert.Equal("FROM-OVERRIDE-1", context.ValidationErrors!["Name"].Single());

        context.ValidationOptions.Localizer = override2;

        await ValidateAsync(typeInfo, new CustomerModel { Name = null }, context, useAsync, default);

        Assert.Equal("FROM-OVERRIDE-2", context.ValidationErrors!["Name"].Last());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_InheritedProperty_LocalizerProviderReceivesDeclaringBaseType(bool useAsync)
    {
        // ValidatablePropertyInfo carries a fixed DeclaringType (set by the source generator to
        // where the property is declared). When validating a Derived instance through a property
        // whose ValidatablePropertyInfo was emitted with declaringType: typeof(BaseInheritedModel),
        // LocalizerProvider must receive typeof(BaseInheritedModel), not typeof(DerivedInheritedModel).
        var seenTypes = new List<Type?>();
        var translations = new Dictionary<Type, Dictionary<string, string>>
        {
            [typeof(BaseInheritedModel)] = new() { ["RequiredKey"] = "{0} is required (from base resource)." },
        };
        var context = SetupPipelinePerType(translations, options =>
        {
            options.LocalizerProvider = (type, factory) =>
            {
                seenTypes.Add(type);
                return factory.Create(type ?? typeof(object));
            };
        });

        // Note declaringType is the BASE type even though the runtime instance is the derived type.
        // This mirrors what the source generator emits for inherited properties.
        var typeInfo = new TestValidatableTypeInfo(typeof(DerivedInheritedModel),
        [
            new TestValidatablePropertyInfo(typeof(BaseInheritedModel), typeof(string), "Name",
                [new RequiredAttribute { ErrorMessage = "RequiredKey" }])
        ]);
        var model = new DerivedInheritedModel { Name = null };

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Name is required (from base resource).", context.ValidationErrors["Name"].Single());
        Assert.Contains(typeof(BaseInheritedModel), seenTypes);
        Assert.DoesNotContain(typeof(DerivedInheritedModel), seenTypes);
    }

    // --- Helpers ---

    private static ValidateContext SetupPipeline(
        Dictionary<string, string> translations,
        Action<ValidationLocalizationOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IStringLocalizerFactory>(new TestStringLocalizerFactory(translations));
        services.AddValidation();
        services.AddValidationLocalization(configureOptions);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        return new ValidateContext
        {
            ValidationOptions = options,
            ValidationContext = new ValidationContext(new object()),
        };
    }

    private static ValidateContext SetupPipelinePerType(
        Dictionary<Type, Dictionary<string, string>> perTypeTranslations,
        Action<ValidationLocalizationOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IStringLocalizerFactory>(new TestStringLocalizerFactory(perTypeTranslations));
        services.AddValidation();
        services.AddValidationLocalization(configureOptions);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        return new ValidateContext
        {
            ValidationOptions = options,
            ValidationContext = new ValidationContext(new object()),
        };
    }

    // --- Test models and attributes ---

    private sealed class CustomerModel
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private sealed class RangeModel
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    private sealed class SharedValidationMessages { }

    private class BaseInheritedModel
    {
        public string? Name { get; set; }
    }

    private sealed class DerivedInheritedModel : BaseInheritedModel
    {
    }

    internal static class IntegrationResources
    {
        public static string RequiredError => "Resource: This field is required.";
    }

    private sealed class StartLessThanEndAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is RangeModel m && m.Start >= m.End)
            {
                return new ValidationResult(ErrorMessage ?? "Start must be less than End.", [nameof(RangeModel.Start)]);
            }
            return ValidationResult.Success;
        }
    }

    private sealed class TestValidatableTypeInfo(
        Type type,
        ValidatablePropertyInfo[] members,
        ValidationAttribute[]? attributes = null) : ValidatableTypeInfo(type, members)
    {
        private readonly ValidationAttribute[] _attributes = attributes ?? [];
        protected override ValidationAttribute[] GetValidationAttributes() => _attributes;
    }

    private sealed class TestValidatablePropertyInfo : ValidatablePropertyInfo
    {
        private readonly ValidationAttribute[] _validationAttributes;

        public TestValidatablePropertyInfo(
            Type declaringType,
            Type propertyType,
            string name,
            ValidationAttribute[] validationAttributes,
            string? displayName = null,
            Func<string?>? displayResourceAccessor = null)
            : base(declaringType, propertyType, name, BuildDisplayNameInfo(displayName, displayResourceAccessor))
        {
            _validationAttributes = validationAttributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;

        private static DisplayNameInfo? BuildDisplayNameInfo(string? displayName, Func<string?>? displayResourceAccessor)
        {
            // Resource-attribute path takes precedence (matches the SG-emitted PropertyResourceDisplayName).
            if (displayResourceAccessor is not null)
            {
                return new TestResourceDisplayName(displayResourceAccessor);
            }

            if (displayName is not null)
            {
                return new TestLiteralDisplayName(displayName);
            }

            return null;
        }
    }

    private sealed class TestValidatableParameterInfo(
        Type parameterType,
        string name,
        string? displayName,
        ValidationAttribute[] validationAttributes)
        : ValidatableParameterInfo(parameterType, name, displayName is null ? null : new TestLiteralDisplayName(displayName))
    {
        protected override ValidationAttribute[] GetValidationAttributes() => validationAttributes;
    }

    private sealed class TestLiteralDisplayName(string literal) : DisplayNameInfo
    {
        public override string? GetDisplayName(ValidateContext context, string memberName, Type? declaringType)
        {
            var localizer = context.ValidationOptions.Localizer;
            if (localizer is null)
            {
                return literal;
            }

            // Literal acts as both lookup key and fallback display name when the localizer doesn't translate.
            return localizer.ResolveDisplayName(new DisplayNameLocalizationContext
            {
                Type = declaringType,
                DisplayName = literal,
                MemberName = memberName,
            }) ?? literal;
        }
    }

    private sealed class TestResourceDisplayName(Func<string?> accessor) : DisplayNameInfo
    {
        public override string? GetDisplayName(ValidateContext context, string memberName, Type? declaringType)
            => accessor();
    }

    private sealed class ConstantLocalizer(string message) : IValidationLocalizer
    {
        public string? ResolveDisplayName(in DisplayNameLocalizationContext context) => null;
        public string? ResolveMessage(in ValidationAttributeLocalizationContext context) => message;
    }
}
