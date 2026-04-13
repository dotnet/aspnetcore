// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms.ClientValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

namespace Microsoft.AspNetCore.Components.Forms;

public class DefaultClientValidationServiceTest
{
    private readonly DefaultClientValidationService _service = CreateService();

    private static DefaultClientValidationService CreateService(ValidationOptions? options = null)
    {
        options ??= new ValidationOptions();
        return new DefaultClientValidationService(Options.Create(options));
    }

    [Fact]
    public void RequiredAttribute_GeneratesDataValRequired()
    {
        var attrs = GetAttributes<RequiredModel>(nameof(RequiredModel.Name));

        Assert.NotNull(attrs);
        Assert.Equal("true", attrs["data-val"]);
        Assert.True(attrs.ContainsKey("data-val-required"));
    }

    [Fact]
    public void StringLengthAttribute_GeneratesDataValLength()
    {
        var attrs = GetAttributes<StringLengthModel>(nameof(StringLengthModel.Name));

        Assert.NotNull(attrs);
        Assert.True(attrs.ContainsKey("data-val-length"));
        Assert.Equal("2", attrs["data-val-length-min"]);
        Assert.Equal("100", attrs["data-val-length-max"]);
    }

    [Fact]
    public void MaxLengthAttribute_GeneratesDataValMaxlength()
    {
        var attrs = GetAttributes<MaxLengthModel>(nameof(MaxLengthModel.Name));

        Assert.NotNull(attrs);
        Assert.True(attrs.ContainsKey("data-val-maxlength"));
        Assert.Equal("50", attrs["data-val-maxlength-max"]);
    }

    [Fact]
    public void MinLengthAttribute_GeneratesDataValMinlength()
    {
        var attrs = GetAttributes<MinLengthModel>(nameof(MinLengthModel.Name));

        Assert.NotNull(attrs);
        Assert.True(attrs.ContainsKey("data-val-minlength"));
        Assert.Equal("3", attrs["data-val-minlength-min"]);
    }

    [Fact]
    public void RangeAttribute_GeneratesDataValRange()
    {
        var attrs = GetAttributes<RangeModel>(nameof(RangeModel.Age));

        Assert.NotNull(attrs);
        Assert.True(attrs.ContainsKey("data-val-range"));
        Assert.Equal("18", attrs["data-val-range-min"]);
        Assert.Equal("120", attrs["data-val-range-max"]);
    }

    [Fact]
    public void RegularExpressionAttribute_GeneratesDataValRegex()
    {
        var attrs = GetAttributes<RegexModel>(nameof(RegexModel.ZipCode));

        Assert.NotNull(attrs);
        Assert.True(attrs.ContainsKey("data-val-regex"));
        Assert.Equal(@"\d{5}", attrs["data-val-regex-pattern"]);
    }

    [Fact]
    public void CompareAttribute_GeneratesDataValEqualto()
    {
        var attrs = GetAttributes<CompareModel>(nameof(CompareModel.ConfirmPassword));

        Assert.NotNull(attrs);
        Assert.True(attrs.ContainsKey("data-val-equalto"));
        Assert.Equal("*.Password", attrs["data-val-equalto-other"]);
    }

    [Fact]
    public void EmailAddressAttribute_GeneratesDataValEmail()
    {
        var attrs = GetAttributes<EmailModel>(nameof(EmailModel.Email));

        Assert.NotNull(attrs);
        Assert.True(attrs.ContainsKey("data-val-email"));
    }

    [Fact]
    public void UrlAttribute_GeneratesDataValUrl()
    {
        var attrs = GetAttributes<UrlModel>(nameof(UrlModel.Website));

        Assert.NotNull(attrs);
        Assert.True(attrs.ContainsKey("data-val-url"));
    }

    [Fact]
    public void PhoneAttribute_GeneratesDataValPhone()
    {
        var attrs = GetAttributes<PhoneModel>(nameof(PhoneModel.Phone));

        Assert.NotNull(attrs);
        Assert.True(attrs.ContainsKey("data-val-phone"));
    }

    [Fact]
    public void CreditCardAttribute_GeneratesDataValCreditcard()
    {
        var attrs = GetAttributes<CreditCardModel>(nameof(CreditCardModel.Card));

        Assert.NotNull(attrs);
        Assert.True(attrs.ContainsKey("data-val-creditcard"));
    }

    [Fact]
    public void FileExtensionsAttribute_GeneratesDataValFileextensions()
    {
        var attrs = GetAttributes<FileExtensionsModel>(nameof(FileExtensionsModel.Avatar));

        Assert.NotNull(attrs);
        Assert.True(attrs.ContainsKey("data-val-fileextensions"));
        Assert.Equal(".png,.jpg,.jpeg,.gif", attrs["data-val-fileextensions-extensions"]);
    }

    [Fact]
    public void MultipleAttributes_AllGenerated()
    {
        var attrs = GetAttributes<MultipleAttrsModel>(nameof(MultipleAttrsModel.Email));

        Assert.NotNull(attrs);
        Assert.True(attrs.ContainsKey("data-val-required"));
        Assert.True(attrs.ContainsKey("data-val-email"));
    }

    [Fact]
    public void PropertyWithNoValidationAttributes_ReturnsNull()
    {
        var attrs = GetAttributes<NoValidationModel>(nameof(NoValidationModel.Name));

        Assert.Null(attrs);
    }

    [Fact]
    public void NonExistentProperty_ReturnsNull()
    {
        var model = new NoValidationModel();
        var fieldId = new FieldIdentifier(model, "NonExistent");

        var attrs = _service.GetHtmlAttributes(fieldId);

        Assert.Null(attrs);
    }

    [Fact]
    public void DisplayAttribute_UsedInErrorMessage()
    {
        var attrs = GetAttributes<DisplayNameModel>(nameof(DisplayNameModel.Email));

        Assert.NotNull(attrs);
        var errorMessage = (string)attrs["data-val-required"];
        Assert.Contains("Email Address", errorMessage);
    }

    [Fact]
    public void CachesResultsForSameProperty()
    {
        var model1 = new RequiredModel();
        var model2 = new RequiredModel();
        var fieldId1 = new FieldIdentifier(model1, nameof(RequiredModel.Name));
        var fieldId2 = new FieldIdentifier(model2, nameof(RequiredModel.Name));

        var attrs1 = _service.GetHtmlAttributes(fieldId1);
        var attrs2 = _service.GetHtmlAttributes(fieldId2);

        Assert.Same(attrs1, attrs2);
    }

    [Fact]
    public void CustomAdapter_GeneratesCustomAttributes()
    {
        var attrs = GetAttributes<CustomAdapterModel>(nameof(CustomAdapterModel.Value));

        Assert.NotNull(attrs);
        Assert.Equal("true", attrs["data-val"]);
        Assert.True(attrs.ContainsKey("data-val-custom"));
        Assert.Equal("custom-value", attrs["data-val-custom-param"]);
    }

    private IReadOnlyDictionary<string, object>? GetAttributes<TModel>(string propertyName) where TModel : new()
    {
        var model = new TModel();
        var fieldId = new FieldIdentifier(model, propertyName);
        return _service.GetHtmlAttributes(fieldId);
    }

    // Test models
    private class RequiredModel { [Required] public string Name { get; set; } = ""; }
    private class StringLengthModel { [StringLength(100, MinimumLength = 2)] public string Name { get; set; } = ""; }
    private class MaxLengthModel { [MaxLength(50)] public string Name { get; set; } = ""; }
    private class MinLengthModel { [MinLength(3)] public string Name { get; set; } = ""; }
    private class RangeModel { [Range(18, 120)] public int Age { get; set; } }
    private class RegexModel { [RegularExpression(@"\d{5}")] public string ZipCode { get; set; } = ""; }
    private class CompareModel { public string Password { get; set; } = ""; [Compare("Password")] public string ConfirmPassword { get; set; } = ""; }
    private class EmailModel { [EmailAddress] public string Email { get; set; } = ""; }
    private class UrlModel { [Url] public string Website { get; set; } = ""; }
    private class PhoneModel { [Phone] public string Phone { get; set; } = ""; }
    private class CreditCardModel { [CreditCard] public string Card { get; set; } = ""; }
    private class FileExtensionsModel { [FileExtensions(Extensions = "png,jpg,jpeg,gif")] public string Avatar { get; set; } = ""; }
    private class MultipleAttrsModel { [Required][EmailAddress] public string Email { get; set; } = ""; }
    private class NoValidationModel { public string Name { get; set; } = ""; }
    private class DisplayNameModel { [Required][Display(Name = "Email Address")] public string Email { get; set; } = ""; }
    private class CustomAdapterModel { [CustomValidation] public string Value { get; set; } = ""; }

    private class CustomValidationAttribute : ValidationAttribute, IClientValidationAdapter
    {
        public void AddClientValidation(in ClientValidationContext context)
        {
            context.MergeAttribute("data-val-custom", context.ErrorMessage);
            context.MergeAttribute("data-val-custom-param", "custom-value");
        }
    }

    // Nested model tests
    private class ParentModel
    {
        public AddressModel Address { get; set; } = new();
    }

    private class AddressModel
    {
        [Required]
        [StringLength(200)]
        public string Street { get; set; } = "";

        [Required]
        [RegularExpression(@"\d{5}")]
        public string ZipCode { get; set; } = "";
    }

    [Fact]
    public void NestedModel_GeneratesAttributesFromNestedType()
    {
        // FieldIdentifier for nested model: Model = parentModel.Address, FieldName = "Street"
        var parent = new ParentModel();
        var fieldId = new FieldIdentifier(parent.Address, nameof(AddressModel.Street));

        var attrs = _service.GetHtmlAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Equal("true", attrs["data-val"]);
        Assert.True(attrs.ContainsKey("data-val-required"));
        Assert.True(attrs.ContainsKey("data-val-length"));
        Assert.Equal("200", attrs["data-val-length-max"]);
    }

    [Fact]
    public void NestedModel_DifferentFieldsOnSameNestedType()
    {
        var parent = new ParentModel();
        var streetField = new FieldIdentifier(parent.Address, nameof(AddressModel.Street));
        var zipField = new FieldIdentifier(parent.Address, nameof(AddressModel.ZipCode));

        var streetAttrs = _service.GetHtmlAttributes(streetField);
        var zipAttrs = _service.GetHtmlAttributes(zipField);

        Assert.NotNull(streetAttrs);
        Assert.NotNull(zipAttrs);

        // Street has StringLength, ZipCode has RegularExpression
        Assert.True(streetAttrs.ContainsKey("data-val-length"));
        Assert.False(streetAttrs.ContainsKey("data-val-regex"));

        Assert.True(zipAttrs.ContainsKey("data-val-regex"));
        Assert.False(zipAttrs.ContainsKey("data-val-length"));
    }

    // --- Localization integration tests ---

    [Fact]
    public void Localization_ErrorMessages_AreLocalized()
    {
        var translations = new Dictionary<string, string>
        {
            ["RequiredError"] = "Le champ {0} est obligatoire."
        };
        var options = CreateOptionsWithLocalization(translations);
        var service = CreateService(options);

        var model = new LocalizedModel();
        var fieldId = new FieldIdentifier(model, nameof(LocalizedModel.Name));
        var attrs = service.GetHtmlAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Equal("Le champ Name est obligatoire.", attrs["data-val-required"]);
    }

    [Fact]
    public void Localization_DisplayNames_AreLocalized()
    {
        var translations = new Dictionary<string, string>
        {
            ["User Name"] = "Nom d'utilisateur",
            ["RequiredError"] = "Le champ {0} est obligatoire."
        };
        var options = CreateOptionsWithLocalization(translations);
        var service = CreateService(options);

        var model = new LocalizedDisplayModel();
        var fieldId = new FieldIdentifier(model, nameof(LocalizedDisplayModel.Name));
        var attrs = service.GetHtmlAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Equal("Le champ Nom d'utilisateur est obligatoire.", attrs["data-val-required"]);
    }

    [Fact]
    public void Localization_RangeAttribute_FormatsArgs()
    {
        var translations = new Dictionary<string, string>
        {
            ["RangeError"] = "{0} doit être entre {1} et {2}."
        };
        var options = CreateOptionsWithLocalization(translations);
        var service = CreateService(options);

        var model = new LocalizedRangeModel();
        var fieldId = new FieldIdentifier(model, nameof(LocalizedRangeModel.Age));
        var attrs = service.GetHtmlAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Equal("Age doit être entre 18 et 120.", attrs["data-val-range"]);
    }

    [Fact]
    public void Localization_NoLocalization_UsesDefaultMessages()
    {
        // No localizer factory configured → default attribute messages
        var service = CreateService();

        var model = new RequiredModel();
        var fieldId = new FieldIdentifier(model, nameof(RequiredModel.Name));
        var attrs = service.GetHtmlAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Equal("The Name field is required.", attrs["data-val-required"]);
    }

    private static ValidationOptions CreateOptionsWithLocalization(Dictionary<string, string> translations)
    {
        var factory = new TestStringLocalizerFactory(translations);
        var options = new ValidationOptions
        {
            LocalizerProvider = (_, _) => new TestStringLocalizer(translations)
        };
        // Use the public FormatErrorMessage API to verify it works —
        // but we need the localization context initialized. Use DI pattern.
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton<Microsoft.Extensions.Localization.IStringLocalizerFactory>(factory);
        services.Configure<ValidationOptions>(o =>
        {
            o.LocalizerProvider = options.LocalizerProvider;
        });
        services.AddValidation();
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ValidationOptions>>().Value;
    }

    private class TestStringLocalizerFactory(Dictionary<string, string> translations)
        : Microsoft.Extensions.Localization.IStringLocalizerFactory
    {
        public Microsoft.Extensions.Localization.IStringLocalizer Create(Type resourceSource)
            => new TestStringLocalizer(translations);

        public Microsoft.Extensions.Localization.IStringLocalizer Create(string baseName, string location)
            => new TestStringLocalizer(translations);
    }

    private class TestStringLocalizer(Dictionary<string, string> translations)
        : Microsoft.Extensions.Localization.IStringLocalizer
    {
        public Microsoft.Extensions.Localization.LocalizedString this[string name] =>
            translations.TryGetValue(name, out var value)
                ? new(name, value, resourceNotFound: false)
                : new(name, name, resourceNotFound: true);

        public Microsoft.Extensions.Localization.LocalizedString this[string name, params object[] arguments] =>
            translations.TryGetValue(name, out var value)
                ? new(name, string.Format(System.Globalization.CultureInfo.CurrentCulture, value, arguments), resourceNotFound: false)
                : new(name, name, resourceNotFound: true);

        public IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(bool includeParentCultures) =>
            translations.Select(kvp => new Microsoft.Extensions.Localization.LocalizedString(kvp.Key, kvp.Value, false));
    }

    // Localization test models
    private class LocalizedModel
    {
        [Required(ErrorMessage = "RequiredError")]
        public string Name { get; set; } = "";
    }

    private class LocalizedDisplayModel
    {
        [Required(ErrorMessage = "RequiredError")]
        [Display(Name = "User Name")]
        public string Name { get; set; } = "";
    }

    private class LocalizedRangeModel
    {
        [Range(18, 120, ErrorMessage = "RangeError")]
        public int Age { get; set; }
    }
}
