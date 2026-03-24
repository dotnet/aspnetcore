// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;
using Microsoft.Extensions.Validation.Localization;

namespace Microsoft.AspNetCore.Components.Forms;

public class DefaultClientValidationServiceLocalizationTest
{
    /// <summary>
    /// Builds a <see cref="DefaultClientValidationService"/> with the full localization pipeline
    /// wired up (same as a real app calling <c>AddValidation().AddValidationLocalization()</c>).
    /// </summary>
    private static DefaultClientValidationService CreateLocalizedService(
        Dictionary<string, string> translations,
        ClientValidationAdapterRegistry? registry = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization();

        // Replace the real IStringLocalizerFactory with our test one.
        services.AddSingleton<Microsoft.Extensions.Localization.IStringLocalizerFactory>(
            new TestStringLocalizerFactory(translations));

        var sp = services.BuildServiceProvider();

        registry ??= CreateRegistryWithBuiltIns();
        var validationOptions = sp.GetRequiredService<IOptions<ValidationOptions>>();

        return new DefaultClientValidationService(registry, validationOptions, sp);
    }

    private static ClientValidationAdapterRegistry CreateRegistryWithBuiltIns()
    {
        var registry = new ClientValidationAdapterRegistry();
        registry.AddAdapter<RequiredAttribute>(_ => new RequiredClientAdapter());
        registry.AddAdapter<StringLengthAttribute>(a => new StringLengthClientAdapter(a));
        registry.AddAdapter<RangeAttribute>(a => new RangeClientAdapter(a));
        registry.AddAdapter<MinLengthAttribute>(a => new MinLengthClientAdapter(a));
        registry.AddAdapter<MaxLengthAttribute>(a => new MaxLengthClientAdapter(a));
        registry.AddAdapter<RegularExpressionAttribute>(a => new RegexClientAdapter(a));
        registry.AddAdapter<EmailAddressAttribute>(_ => new DataTypeClientAdapter("data-val-email"));
        registry.AddAdapter<PhoneAttribute>(_ => new DataTypeClientAdapter("data-val-phone"));

        return registry;
    }

    private static FieldIdentifier CreateFieldIdentifier<T>(T model, string fieldName) where T : class
        => new(model, fieldName);

    [Fact]
    public void LocalizedRequiredMessage_AppearsInDataValAttribute()
    {
        var translations = new Dictionary<string, string>
        {
            ["CustomerName"] = "Nom du client",
            ["RequiredError"] = "Le champ {0} est obligatoire."
        };
        var service = CreateLocalizedService(translations);
        var model = new LocalizedCustomerModel();
        var field = CreateFieldIdentifier(model, nameof(LocalizedCustomerModel.Name));

        var result = service.GetValidationAttributes(field);

        Assert.True(result.ContainsKey("data-val-required"));
        Assert.Equal("Le champ Nom du client est obligatoire.", result["data-val-required"]);
    }

    [Fact]
    public void LocalizedDisplayName_UsedInErrorMessage()
    {
        var translations = new Dictionary<string, string>
        {
            ["CustomerEmail"] = "Correo electrónico",
            ["RequiredError"] = "{0} es obligatorio.",
            ["EmailError"] = "{0} no es válida."
        };
        var service = CreateLocalizedService(translations);
        var model = new LocalizedCustomerModel();
        var field = CreateFieldIdentifier(model, nameof(LocalizedCustomerModel.Email));

        var result = service.GetValidationAttributes(field);

        Assert.Equal("Correo electrónico es obligatorio.", result["data-val-required"]);
        Assert.Equal("Correo electrónico no es válida.", result["data-val-email"]);
    }

    [Fact]
    public void LocalizedRangeMessage_IncludesMinMax()
    {
        var translations = new Dictionary<string, string>
        {
            ["CustomerAge"] = "Edad",
            ["RangeError"] = "El campo {0} debe estar entre {1} y {2}."
        };
        var service = CreateLocalizedService(translations);
        var model = new LocalizedCustomerModel();
        var field = CreateFieldIdentifier(model, nameof(LocalizedCustomerModel.Age));

        var result = service.GetValidationAttributes(field);

        Assert.True(result.ContainsKey("data-val-range"));
        Assert.Equal("El campo Edad debe estar entre 18 y 120.", result["data-val-range"]);
    }

    [Fact]
    public void LocalizedStringLengthMessage_IncludesMinAndMaxLength()
    {
        var translations = new Dictionary<string, string>
        {
            ["CustomerName"] = "Nome",
            ["StringLengthError"] = "{0} deve ter entre {2} e {1} caracteres."
        };
        var service = CreateLocalizedService(translations);
        var model = new LocalizedCustomerModel();
        var field = CreateFieldIdentifier(model, nameof(LocalizedCustomerModel.Name));

        var result = service.GetValidationAttributes(field);

        Assert.True(result.ContainsKey("data-val-length"));
        Assert.Equal("Nome deve ter entre 2 e 100 caracteres.", result["data-val-length"]);
    }

    [Fact]
    public void FallsBackToDefaultMessage_WhenNoTranslationExists()
    {
        // Empty translations — no localized messages available.
        var service = CreateLocalizedService(new Dictionary<string, string>());
        var model = new LocalizedCustomerModel();
        var field = CreateFieldIdentifier(model, nameof(LocalizedCustomerModel.Phone));

        var result = service.GetValidationAttributes(field);

        // With no translations, ErrorMessageProvider returns null so FormatErrorMessage
        // is used with the raw ErrorMessage string ("PhoneError") as the format template.
        Assert.True(result.ContainsKey("data-val-phone"));
        Assert.NotEmpty(result["data-val-phone"]);
    }

    [Fact]
    public void DifferentCultures_ProduceDifferentCachedResults()
    {
        var translations = new Dictionary<string, string>
        {
            ["CustomerName"] = "Translated Name",
            ["RequiredError"] = "Translated: {0} is required."
        };
        var service = CreateLocalizedService(translations);
        var model = new LocalizedCustomerModel();
        var field = CreateFieldIdentifier(model, nameof(LocalizedCustomerModel.Name));

        // First call with default culture.
        var savedCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var resultEn = service.GetValidationAttributes(field);

            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var resultFr = service.GetValidationAttributes(field);

            // Both should have the localized value (same test localizer), but
            // they must be separate cache entries — not the same object reference.
            Assert.NotSame(resultEn, resultFr);
            Assert.Equal("Translated: Translated Name is required.", resultEn["data-val-required"]);
            Assert.Equal("Translated: Translated Name is required.", resultFr["data-val-required"]);
        }
        finally
        {
            CultureInfo.CurrentUICulture = savedCulture;
        }
    }

    [Fact]
    public void WithoutLocalization_UsesDefaultAttributeMessages()
    {
        // Create a service with no localization providers configured.
        var registry = CreateRegistryWithBuiltIns();
        var options = Options.Create(new ValidationOptions());
        var sp = new ServiceCollection().BuildServiceProvider();
        var service = new DefaultClientValidationService(registry, options, sp);

        var model = new LocalizedCustomerModel();
        var field = CreateFieldIdentifier(model, nameof(LocalizedCustomerModel.Name));

        var result = service.GetValidationAttributes(field);

        // Should use the ErrorMessage keys as literal format strings (default behavior).
        Assert.Contains("data-val-required", result.Keys);
        Assert.Contains("data-val-length", result.Keys);
    }

    [Fact]
    public void LocalizedPhoneMessage_AppearsInDataValAttribute()
    {
        var translations = new Dictionary<string, string>
        {
            ["CustomerPhone"] = "Teléfono",
            ["PhoneError"] = "{0} no es un número de teléfono válido."
        };
        var service = CreateLocalizedService(translations);
        var model = new LocalizedCustomerModel();
        var field = CreateFieldIdentifier(model, nameof(LocalizedCustomerModel.Phone));

        var result = service.GetValidationAttributes(field);

        Assert.True(result.ContainsKey("data-val-phone"));
        Assert.Equal("Teléfono no es un número de teléfono válido.", result["data-val-phone"]);
    }

    [Fact]
    public void MultipleAttributes_AllGetLocalizedMessages()
    {
        var translations = new Dictionary<string, string>
        {
            ["CustomerName"] = "Kundenname",
            ["RequiredError"] = "{0} ist erforderlich.",
            ["StringLengthError"] = "{0} muss zwischen {2} und {1} Zeichen lang sein."
        };
        var service = CreateLocalizedService(translations);
        var model = new LocalizedCustomerModel();
        var field = CreateFieldIdentifier(model, nameof(LocalizedCustomerModel.Name));

        var result = service.GetValidationAttributes(field);

        Assert.Equal("true", result["data-val"]);
        Assert.Equal("Kundenname ist erforderlich.", result["data-val-required"]);
        Assert.Equal("Kundenname muss zwischen 2 und 100 Zeichen lang sein.", result["data-val-length"]);
    }

    // ── Test models ──

    private class LocalizedCustomerModel
    {
        [Required(ErrorMessage = "RequiredError")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "StringLengthError")]
        [Display(Name = "CustomerName")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "RequiredError")]
        [EmailAddress(ErrorMessage = "EmailError")]
        [Display(Name = "CustomerEmail")]
        public string? Email { get; set; }

        [Range(18, 120, ErrorMessage = "RangeError")]
        [Display(Name = "CustomerAge")]
        public int Age { get; set; }

        [Phone(ErrorMessage = "PhoneError")]
        [Display(Name = "CustomerPhone")]
        public string? Phone { get; set; }
    }

    // ── Test doubles ──

    private sealed class TestStringLocalizerFactory(
        Dictionary<string, string> translations)
        : Microsoft.Extensions.Localization.IStringLocalizerFactory
    {
        public Microsoft.Extensions.Localization.IStringLocalizer Create(Type resourceSource)
            => new TestStringLocalizer(translations);

        public Microsoft.Extensions.Localization.IStringLocalizer Create(string baseName, string location)
            => new TestStringLocalizer(translations);
    }

    private sealed class TestStringLocalizer(
        Dictionary<string, string> translations)
        : Microsoft.Extensions.Localization.IStringLocalizer
    {
        public Microsoft.Extensions.Localization.LocalizedString this[string name]
            => translations.TryGetValue(name, out var value)
                ? new(name, value, resourceNotFound: false)
                : new(name, name, resourceNotFound: true);

        public Microsoft.Extensions.Localization.LocalizedString this[string name, params object[] arguments]
            => translations.TryGetValue(name, out var value)
                ? new(name, string.Format(CultureInfo.CurrentCulture, value, arguments), resourceNotFound: false)
                : new(name, name, resourceNotFound: true);

        public IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(bool includeParentCultures)
            => translations.Select(kvp => new Microsoft.Extensions.Localization.LocalizedString(kvp.Key, kvp.Value));
    }
}
