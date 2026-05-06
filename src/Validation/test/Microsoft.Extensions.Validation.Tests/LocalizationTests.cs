#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Validation.Tests;

public class LocalizationTests
{
    // --- Auto-detection / DI integration tests ---

    [Fact]
    public void AddValidation_WithoutLocalization_LocalizerResolves()
    {
        // Regression test for the original DI bug where ValidationLocalizer registration
        // could not be satisfied when IStringLocalizerFactory was not registered.
        var services = new ServiceCollection();
        services.AddValidation();

        var provider = services.BuildServiceProvider();
        var localizer = provider.GetRequiredService<ValidationLocalizer>();

        Assert.NotNull(localizer);
        // Without an IStringLocalizerFactory, literal display names pass through unchanged.
        Assert.Equal("My Display", localizer.ResolveDisplayName(
            displayName: "My Display", displayResourceAccessor: null, declaringType: null, defaultName: "Member"));
        // And error messages are not localized at all (caller falls back to attribute defaults).
        Assert.Null(localizer.ResolveErrorMessage(
            new RequiredAttribute { ErrorMessage = "MyKey" }, displayName: "Member", declaringType: null));
    }

    [Fact]
    public void AddValidation_WithLocalization_LocalizerUsesIStringLocalizer()
    {
        var translations = new Dictionary<string, string>
        {
            ["MyDisplay"] = "Mon Affichage",
            ["MyError"] = "Erreur sur {0}",
        };
        var services = new ServiceCollection();
        services.AddValidation();
        services.AddSingleton<IStringLocalizerFactory>(new TestStringLocalizerFactory(translations));

        var provider = services.BuildServiceProvider();
        var localizer = provider.GetRequiredService<ValidationLocalizer>();

        Assert.Equal("Mon Affichage", localizer.ResolveDisplayName(
            displayName: "MyDisplay", displayResourceAccessor: null, declaringType: typeof(object), defaultName: "Member"));
        Assert.Equal("Erreur sur Mon Affichage", localizer.ResolveErrorMessage(
            new RequiredAttribute { ErrorMessage = "MyError" }, displayName: "Mon Affichage", declaringType: typeof(object)));
    }

    [Fact]
    public void AddValidation_LocalizerResolvedAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddLocalization();

        var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<ValidationLocalizer>();
        var second = provider.GetRequiredService<ValidationLocalizer>();

        Assert.Same(first, second);
    }

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
    public async Task DisplayName_WithResourceAccessor_BypassesIStringLocalizer()
    {
        // When DisplayResourceAccessor is supplied, the resolved value comes from the accessor,
        // not from IStringLocalizer (even if a translation for the same key is present).
        var translations = new Dictionary<string, string>
        {
            ["ResourceValue"] = "Should not be used",
            // Localizer should also not see the literal display name (it's null because the SG
            // does not bake a literal when a resource accessor is emitted).
            ["My Custom Name"] = "Should not be used either",
        };
        var propInfo = new TestValidatablePropertyInfo(
            typeof(CustomerModel), typeof(string), "Name",
            [new RequiredAttribute()],
            displayName: null,
            displayResourceAccessor: () => "ResourceValue");
        var typeInfo = new TestValidatableTypeInfo(typeof(CustomerModel), [propInfo]);

        var model = new CustomerModel { Name = null };
        var context = CreateContext(model, null, translations);
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The ResourceValue field is required.", context.ValidationErrors["Name"].First());
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
    public async Task ErrorMessage_WithResourceType_NotOverriddenByErrorMessageKeyProvider()
    {
        // Regression: when an attribute uses ErrorMessageResourceType for its own resource-based
        // localization, a globally configured ErrorMessageKeyProvider must not override it.
        var model = new ResourceModel { Value = null };
        var requiredAttr = new RequiredAttribute
        {
            ErrorMessageResourceType = typeof(IntegrationResources),
            ErrorMessageResourceName = nameof(IntegrationResources.RequiredError)
        };
        var propInfo = new TestValidatablePropertyInfo(
            typeof(ResourceModel), typeof(string), "Value", [requiredAttr]);
        var typeInfo = new TestValidatableTypeInfo(typeof(ResourceModel), [propInfo]);

        var options = new ValidationOptions
        {
            ErrorMessageKeyProvider = ctx => $"{ctx.Attribute.GetType().Name}_Error"
        };
        var translations = new Dictionary<string, string>
        {
            ["RequiredAttribute_Error"] = "Should NOT be used (resource type wins)",
        };
        var context = CreateContext(model, options, translations);
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

    [Fact]
    public async Task LocalizerProvider_CalledWithDeclaringType()
    {
        var seenTypes = new List<Type>();
        var translations = new Dictionary<string, string>
        {
            ["RequiredError"] = "Le {0} est requis."
        };
        var options = new ValidationOptions
        {
            LocalizerProvider = (type, factory) =>
            {
                seenTypes.Add(type);
                return factory.Create(typeof(object));
            }
        };
        var context = CreateContext(new IntegrationCustomer { Name = null }, options, translations);

        var typeInfo = CreateCustomerTypeInfoWithErrorKeys();
        await typeInfo.ValidateAsync(context.ValidationContext.ObjectInstance, context, default);

        // Provider should be called with IntegrationCustomer (the declaring type) for the property.
        Assert.Contains(typeof(IntegrationCustomer), seenTypes);
        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Le Name est requis.", context.ValidationErrors["Name"].First());
    }

    [Fact]
    public async Task LocalizerProvider_SharedResource_UsesSingleLocalizer()
    {
        var sharedTranslations = new Dictionary<string, string>
        {
            ["RequiredError"] = "Champ obligatoire: {0}"
        };
        var options = new ValidationOptions
        {
            // Always return the same shared localizer regardless of declaring type.
            LocalizerProvider = (_, factory) => factory.Create(typeof(object))
        };
        var context = CreateContext(new IntegrationCustomer { Name = null }, options, sharedTranslations);

        var typeInfo = CreateCustomerTypeInfoWithErrorKeys();
        await typeInfo.ValidateAsync(context.ValidationContext.ObjectInstance, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Champ obligatoire: Name", context.ValidationErrors["Name"].First());
    }

    [Fact]
    public void LocalizerProvider_ReturnsNull_ThrowsInvalidOperationException()
    {
        // When the user-supplied provider returns null, the localizer surfaces a clear error
        // rather than crashing with a NullReferenceException on the next access.
        var options = new ValidationOptions
        {
            LocalizerProvider = (_, _) => null!
        };
        var localizer = new ValidationLocalizer(options, new TestStringLocalizerFactory([]));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            localizer.ResolveDisplayName(
                displayName: "Foo", displayResourceAccessor: null, declaringType: typeof(object), defaultName: "Member"));

        Assert.Contains(nameof(ValidationOptions.LocalizerProvider), ex.Message);
    }

    [Fact]
    public async Task LocalizerProvider_PerTypeIsolation()
    {
        // Per-type lookup: one resource not found in CustomerA's localizer should not leak into CustomerB's.
        var perTypeTranslations = new Dictionary<Type, Dictionary<string, string>>
        {
            [typeof(IntegrationCustomer)] = new() { ["RequiredError"] = "Per-type required" },
        };
        var options = new ValidationOptions
        {
            LocalizerProvider = (type, _) => new TestStringLocalizer(
                perTypeTranslations.TryGetValue(type, out var t) ? t : [])
        };
        var context = CreateContext(new IntegrationCustomer { Name = null }, options);

        var typeInfo = CreateCustomerTypeInfoWithErrorKeys();
        await typeInfo.ValidateAsync(context.ValidationContext.ObjectInstance, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Per-type required", context.ValidationErrors["Name"].First());
    }

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

    // --- Standalone ValidationLocalizer tests (SSR / out-of-pipeline use) ---
    // These exercise ValidationLocalizer directly without going through the validation pipeline,
    // mirroring how a Blazor SSR client-side rule renderer would invoke it.

    [Fact]
    public void ResolveDisplayName_ResourceAccessor_BypassesIStringLocalizer()
    {
        var translations = new Dictionary<string, string>
        {
            // The key here matches the resource accessor's return value. If the localizer were
            // (incorrectly) consulted on the ResourceType path, it would re-translate this.
            ["Resolved From Resource"] = "Should NOT be re-translated",
        };
        var localizer = new ValidationLocalizer(new ValidationOptions(), new TestStringLocalizerFactory(translations));

        var result = localizer.ResolveDisplayName(
            displayName: null,
            displayResourceAccessor: () => "Resolved From Resource",
            declaringType: typeof(object),
            defaultName: "Member");

        Assert.Equal("Resolved From Resource", result);
    }

    [Fact]
    public void ResolveDisplayName_LiteralWithLocalizer_LooksUpAsKey()
    {
        var translations = new Dictionary<string, string>
        {
            ["Customer Name"] = "Nom du client",
        };
        var localizer = new ValidationLocalizer(new ValidationOptions(), new TestStringLocalizerFactory(translations));

        var result = localizer.ResolveDisplayName(
            displayName: "Customer Name",
            displayResourceAccessor: null,
            declaringType: typeof(object),
            defaultName: "Member");

        Assert.Equal("Nom du client", result);
    }

    [Fact]
    public void ResolveDisplayName_LiteralWithLocalizer_FallsBackToLiteralOnMiss()
    {
        var localizer = new ValidationLocalizer(new ValidationOptions(), new TestStringLocalizerFactory([]));

        var result = localizer.ResolveDisplayName(
            displayName: "Customer Name",
            displayResourceAccessor: null,
            declaringType: typeof(object),
            defaultName: "Member");

        Assert.Equal("Customer Name", result);
    }

    [Fact]
    public void ResolveDisplayName_NoLiteralAndNoAccessor_ReturnsDefault()
    {
        var localizer = new ValidationLocalizer(new ValidationOptions(), factory: null);

        var result = localizer.ResolveDisplayName(
            displayName: null,
            displayResourceAccessor: null,
            declaringType: typeof(object),
            defaultName: "MemberFallback");

        Assert.Equal("MemberFallback", result);
    }

    [Fact]
    public void ResolveDisplayName_AccessorReturnsNull_ReturnsDefault()
    {
        var localizer = new ValidationLocalizer(new ValidationOptions(), factory: null);

        var result = localizer.ResolveDisplayName(
            displayName: null,
            displayResourceAccessor: () => null,
            declaringType: typeof(object),
            defaultName: "MemberFallback");

        Assert.Equal("MemberFallback", result);
    }

    [Fact]
    public void ResolveErrorMessage_NoFactory_ReturnsNull()
    {
        var localizer = new ValidationLocalizer(new ValidationOptions(), factory: null);

        var result = localizer.ResolveErrorMessage(
            new RequiredAttribute { ErrorMessage = "Anything" }, displayName: "X", declaringType: null);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveErrorMessage_RangeAttribute_FormatsWithMinMax()
    {
        var translations = new Dictionary<string, string>
        {
            ["RangeKey"] = "{0} must be between {1} and {2}.",
        };
        var localizer = new ValidationLocalizer(new ValidationOptions(), new TestStringLocalizerFactory(translations));

        var result = localizer.ResolveErrorMessage(
            new RangeAttribute(1, 100) { ErrorMessage = "RangeKey" },
            displayName: "Score",
            declaringType: typeof(object));

        Assert.Equal("Score must be between 1 and 100.", result);
    }

    [Fact]
    public void ResolveErrorMessage_KeyProviderUsedWhenErrorMessageMissing()
    {
        var translations = new Dictionary<string, string>
        {
            ["RequiredAttribute_Default"] = "Required: {0}",
        };
        var options = new ValidationOptions
        {
            ErrorMessageKeyProvider = ctx => $"{ctx.Attribute.GetType().Name}_Default"
        };
        var localizer = new ValidationLocalizer(options, new TestStringLocalizerFactory(translations));

        var result = localizer.ResolveErrorMessage(
            new RequiredAttribute(), displayName: "Name", declaringType: typeof(object));

        Assert.Equal("Required: Name", result);
    }

    // --- Parameter-level localization tests ---

    [Fact]
    public void ResolveDisplayName_ParameterScenario_FallsBackToObjectResource()
    {
        // Parameter validation passes declaringType: null; the localizer falls back to
        // typeof(object) as the resource source. This test pins the documented behavior.
        var translations = new Dictionary<string, string>
        {
            ["MyParam"] = "Mon Paramètre",
        };
        var localizer = new ValidationLocalizer(new ValidationOptions(), new TestStringLocalizerFactory(translations));

        var result = localizer.ResolveDisplayName(
            displayName: "MyParam",
            displayResourceAccessor: null,
            declaringType: null,
            defaultName: "param");

        // The TestStringLocalizerFactory returns the same translations regardless of resource
        // source, so the lookup succeeds. With a real per-type factory, this would miss because
        // typeof(object) would resolve to a non-existent resource file.
        Assert.Equal("Mon Paramètre", result);
    }

    [Fact]
    public void ResolveDisplayName_ParameterScenario_SharedResourceProvider()
    {
        // The recommended pattern for Minimal API parameter validation: a shared-resource
        // LocalizerProvider that ignores the declaring type.
        var sharedTranslations = new Dictionary<string, string>
        {
            ["MyParam"] = "Mon Paramètre",
        };
        var sharedFactory = new TestStringLocalizerFactory(sharedTranslations);
        var options = new ValidationOptions
        {
            LocalizerProvider = (_, factory) => factory.Create(typeof(object))
        };
        var localizer = new ValidationLocalizer(options, sharedFactory);

        var result = localizer.ResolveDisplayName(
            displayName: "MyParam",
            displayResourceAccessor: null,
            declaringType: null,
            defaultName: "param");

        Assert.Equal("Mon Paramètre", result);
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
        string? displayName = null,
        Func<string?>? displayResourceAccessor = null)
        : ValidatablePropertyInfo(declaringType, propertyType, name, displayName ?? (displayResourceAccessor is null ? name : null), displayResourceAccessor)
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
