// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Components.Forms.ClientValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Validation;
using Microsoft.Extensions.Validation.Localization;

namespace Microsoft.AspNetCore.Components.Forms;

public class DefaultClientValidationServiceTest
{
    private readonly DefaultClientValidationService _service = CreateService();

    private static DefaultClientValidationService CreateService(IValidationLocalizer? localizer = null)
    {
        var services = new ServiceCollection();
        services.AddOptions<ValidationOptions>().Configure(o => o.Localizer = localizer);
        return new DefaultClientValidationService(services.BuildServiceProvider());
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

        var attrs = _service.GetClientValidationAttributes(fieldId);

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
    public void CachesReflectionResults_NotRenderedDictionary()
    {
        // The cache stores reflection results (FieldMetadata) keyed by (Type, FieldName), not the
        // rendered HTML attribute dictionary. Each call constructs a fresh dictionary so per-call
        // localization respects the current request culture. Assert that two calls produce
        // equivalent (but not the same instance) output for the same (Type, FieldName) pair.
        var model1 = new RequiredModel();
        var model2 = new RequiredModel();
        var fieldId1 = new FieldIdentifier(model1, nameof(RequiredModel.Name));
        var fieldId2 = new FieldIdentifier(model2, nameof(RequiredModel.Name));

        var attrs1 = _service.GetClientValidationAttributes(fieldId1);
        var attrs2 = _service.GetClientValidationAttributes(fieldId2);

        Assert.NotNull(attrs1);
        Assert.NotNull(attrs2);
        Assert.NotSame(attrs1, attrs2);
        Assert.Equal(attrs1.Count, attrs2.Count);
        foreach (var (key, value) in attrs1)
        {
            Assert.Equal(value, attrs2[key]);
        }
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
        return _service.GetClientValidationAttributes(fieldId);
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
        public IEnumerable<ClientValidationRule> GetClientValidationRules(string errorMessage)
            => [new ClientValidationRule("custom", errorMessage).WithParameter("param", "custom-value")];
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

        var attrs = _service.GetClientValidationAttributes(fieldId);

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

        var streetAttrs = _service.GetClientValidationAttributes(streetField);
        var zipAttrs = _service.GetClientValidationAttributes(zipField);

        Assert.NotNull(streetAttrs);
        Assert.NotNull(zipAttrs);

        // Street has StringLength, ZipCode has RegularExpression
        Assert.True(streetAttrs.ContainsKey("data-val-length"));
        Assert.False(streetAttrs.ContainsKey("data-val-regex"));

        Assert.True(zipAttrs.ContainsKey("data-val-regex"));
        Assert.False(zipAttrs.ContainsKey("data-val-length"));
    }

    [Fact]
    public void DisplayNameAttribute_UsedInErrorMessage()
    {
        var attrs = GetAttributes<DisplayNameAttrModel>(nameof(DisplayNameAttrModel.Name));

        Assert.NotNull(attrs);
        var errorMessage = (string)attrs["data-val-required"];
        Assert.Contains("Full Name", errorMessage);
    }

    [Fact]
    public void StringLength_MaxOnly_OmitsMinAttribute()
    {
        var attrs = GetAttributes<StringLengthMaxOnlyModel>(nameof(StringLengthMaxOnlyModel.Name));

        Assert.NotNull(attrs);
        Assert.True(attrs.ContainsKey("data-val-length"));
        Assert.Equal("50", attrs["data-val-length-max"]);
        Assert.False(attrs.ContainsKey("data-val-length-min"));
    }

    [Fact]
    public void Range_DoubleValues_FormatsWithInvariantCulture()
    {
        var attrs = GetAttributes<DoubleRangeModel>(nameof(DoubleRangeModel.Score));

        Assert.NotNull(attrs);
        Assert.Equal("0.5", attrs["data-val-range-min"]);
        Assert.Equal("99.9", attrs["data-val-range-max"]);
    }

    [Fact]
    public void Range_DateTimeOperand_DoesNotEmitDataValRange()
    {
        // Non-numeric range types (e.g., DateTime) have no client-side equivalent in the JS validator,
        // so we skip emitting data-val-range-* for them. Server-side validation still applies.
        var attrs = GetAttributes<DateTimeRangeModel>(nameof(DateTimeRangeModel.Date));

        Assert.Null(attrs);
    }

    [Fact]
    public void PropertyWithNoDisplayAttribute_UsesPropertyNameInErrorMessage()
    {
        var attrs = GetAttributes<RequiredModel>(nameof(RequiredModel.Name));

        Assert.NotNull(attrs);
        var errorMessage = (string)attrs["data-val-required"];
        Assert.Contains("Name", errorMessage);
    }

    [Fact]
    public void InheritedValidationAttributes_AreIncluded()
    {
        var attrs = GetAttributes<DerivedModel>(nameof(DerivedModel.BaseProp));

        Assert.NotNull(attrs);
        Assert.True(attrs.ContainsKey("data-val-required"));
    }

    private class DisplayNameAttrModel { [Required][System.ComponentModel.DisplayName("Full Name")] public string Name { get; set; } = ""; }
    private class StringLengthMaxOnlyModel { [StringLength(50)] public string Name { get; set; } = ""; }
    private class DoubleRangeModel { [Range(0.5, 99.9)] public double Score { get; set; } }
    private class DateTimeRangeModel { [Range(typeof(DateTime), "2020-01-01", "2030-12-31")] public DateTime Date { get; set; } }
    private class BaseModel { [Required] public string BaseProp { get; set; } = ""; }
    private class DerivedModel : BaseModel { }

    [Fact]
    public void Localizer_NotConfigured_ProducesSameOutputAsNoLocalizer()
    {
        // Regression guard: pin no-localizer baseline.
        var noLocalizer = CreateService();
        var fieldId = new FieldIdentifier(new DisplayNameModel(), nameof(DisplayNameModel.Email));

        var attrs = noLocalizer.GetClientValidationAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Equal("The Email Address field is required.", attrs["data-val-required"]);
    }

    [Fact]
    public void Localizer_Configured_DisplayNameIsLocalized_AndAppearsInErrorMessage()
    {
        var localizer = new RecordingValidationLocalizer
        {
            DisplayNameResults = { ["Email Address"] = "Adresse e-mail" },
        };
        var service = CreateService(localizer);
        var fieldId = new FieldIdentifier(new DisplayNameModel(), nameof(DisplayNameModel.Email));

        var attrs = service.GetClientValidationAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Contains("Adresse e-mail", (string)attrs["data-val-required"]);
    }

    [Fact]
    public void Localizer_Configured_DisplayNameLookupMisses_FallsBackToLiteral()
    {
        // Localizer returns null for the literal; expect the literal (not the member name) in the message.
        var localizer = new RecordingValidationLocalizer();
        var service = CreateService(localizer);
        var fieldId = new FieldIdentifier(new DisplayNameModel(), nameof(DisplayNameModel.Email));

        var attrs = service.GetClientValidationAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Contains("Email Address", (string)attrs["data-val-required"]);
        Assert.DoesNotContain("The Email field", (string)attrs["data-val-required"]);
    }

    [Fact]
    public void Localizer_Configured_NoDisplayAttribute_LocalizerNotCalledForDisplayName()
    {
        // No [Display] / [DisplayName] → LiteralDisplayName == null → ResolveDisplayName must
        // skip the localizer entirely and fall back to the member name.
        var localizer = new RecordingValidationLocalizer();
        var service = CreateService(localizer);
        var fieldId = new FieldIdentifier(new RequiredModel(), nameof(RequiredModel.Name));

        var attrs = service.GetClientValidationAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Empty(localizer.DisplayNameCalls);
        Assert.Contains("Name", (string)attrs["data-val-required"]);
    }

    [Fact]
    public void Localizer_Configured_DisplayNameContext_HasCorrectFields()
    {
        var localizer = new RecordingValidationLocalizer();
        var service = CreateService(localizer);
        var fieldId = new FieldIdentifier(new DisplayNameModel(), nameof(DisplayNameModel.Email));

        service.GetClientValidationAttributes(fieldId);

        var displayCall = Assert.Single(localizer.DisplayNameCalls);
        Assert.Equal("Email Address", displayCall.DisplayName);
        Assert.Equal("Email", displayCall.MemberName);
        Assert.Equal(typeof(DisplayNameModel), displayCall.Type);
    }

    [Fact]
    public void Localizer_Configured_DeclaringType_IsBaseClassForInheritedProperty()
    {
        // BaseProp is declared on BaseModel; FieldIdentifier carries a DerivedModel instance.
        // The localizer must see typeof(BaseModel) as DeclaringType, matching MEV's
        // per-type localizer scoping for inherited members.
        var localizer = new RecordingValidationLocalizer();
        var service = CreateService(localizer);
        var fieldId = new FieldIdentifier(new InheritedDisplayDerivedModel(), nameof(InheritedDisplayDerivedModel.BaseDisplayProp));

        service.GetClientValidationAttributes(fieldId);

        var displayCall = Assert.Single(localizer.DisplayNameCalls);
        Assert.Equal(typeof(InheritedDisplayBaseModel), displayCall.Type);
    }

    [Fact]
    public void Localizer_Configured_ErrorMessageIsLocalized()
    {
        var localizer = new RecordingValidationLocalizer
        {
            ErrorMessageResults = { ["RequiredKey"] = "Le champ {0} est requis" },
        };
        var service = CreateService(localizer);
        var fieldId = new FieldIdentifier(new LocalizedRequiredModel(), nameof(LocalizedRequiredModel.Email));

        var attrs = service.GetClientValidationAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Equal("Le champ {0} est requis", attrs["data-val-required"]);
    }

    [Fact]
    public void Localizer_Configured_ErrorMessageMisses_FallsBackToFormatErrorMessage()
    {
        var localizer = new RecordingValidationLocalizer();
        var service = CreateService(localizer);
        var fieldId = new FieldIdentifier(new LocalizedRequiredModel(), nameof(LocalizedRequiredModel.Email));

        var attrs = service.GetClientValidationAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Equal("RequiredKey", attrs["data-val-required"]);
    }

    [Fact]
    public void Localizer_Configured_ErrorMessageContext_HasCorrectFields()
    {
        var localizer = new RecordingValidationLocalizer
        {
            DisplayNameResults = { ["Email Address"] = "Adresse e-mail" },
        };
        var service = CreateService(localizer);
        var fieldId = new FieldIdentifier(new DisplayNameModel(), nameof(DisplayNameModel.Email));

        service.GetClientValidationAttributes(fieldId);

        var errorCall = Assert.Single(localizer.ErrorMessageCalls);
        Assert.Equal("Email", errorCall.MemberName);
        Assert.Equal("Adresse e-mail", errorCall.DisplayName);
        Assert.Equal(typeof(DisplayNameModel), errorCall.DeclaringType);
        Assert.IsType<RequiredAttribute>(errorCall.Attribute);
    }

    [Fact]
    public void ResourceTypeDisplayAttribute_BypassesLocalizer()
    {
        // [Display(Name="ResourceKey", ResourceType=typeof(TestResources))] must call
        // DisplayAttribute.GetName() directly (resource lookup is canonical localized source).
        var localizer = new ThrowingDisplayNameLocalizer();
        var service = CreateService(localizer);
        var fieldId = new FieldIdentifier(new ResourceDisplayModel(), nameof(ResourceDisplayModel.Field));

        var attrs = service.GetClientValidationAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Contains(TestResources.ResourceKey, (string)attrs["data-val-required"]);
    }

    [Fact]
    public void ErrorMessageResourceType_BypassesLocalizer()
    {
        // [Required(ErrorMessageResourceType=..., ErrorMessageResourceName=...)] must use the
        // attribute's own FormatErrorMessage (DataAnnotations does the resource lookup).
        var localizer = new ThrowingErrorMessageLocalizer();
        var service = CreateService(localizer);
        var fieldId = new FieldIdentifier(new ResourceErrorModel(), nameof(ResourceErrorModel.Field));

        var attrs = service.GetClientValidationAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, TestResources.RequiredError, "Field"), attrs["data-val-required"]);
    }

    [Fact]
    public void Localizer_Configured_RangeAttribute_LocalizedTemplateIsUsedAsIs()
    {
        // The localizer is responsible for any {0}/{1}/{2} substitution (via
        // DefaultValidationLocalizer + ValidationAttributeFormatterRegistry on the server side).
        // DefaultClientValidationService just passes the localizer's result through.
        var localizer = new RecordingValidationLocalizer
        {
            ErrorMessageResults = { ["RangeKey"] = "Age doit être entre 18 et 120" },
        };
        var service = CreateService(localizer);
        var fieldId = new FieldIdentifier(new LocalizedRangeModel(), nameof(LocalizedRangeModel.Age));

        var attrs = service.GetClientValidationAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Equal("Age doit être entre 18 et 120", attrs["data-val-range"]);
        // Min/Max parameters are still emitted with invariant culture from the attribute.
        Assert.Equal("18", attrs["data-val-range-min"]);
        Assert.Equal("120", attrs["data-val-range-max"]);
    }

    [Fact]
    public void Cache_DifferentCulturesProduceDifferentOutput()
    {
        // Regression guard for the original culture-poisoning bug. The reflection cache stores
        // ValidationAttribute[], but the rendered data-val-* values are computed per call so
        // different cultures produce different output.
        var localizer = new CultureBasedLocalizer();
        var service = CreateService(localizer);
        var fieldId = new FieldIdentifier(new LocalizedRequiredModel(), nameof(LocalizedRequiredModel.Email));

        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr");
            var frAttrs = service.GetClientValidationAttributes(fieldId);

            CultureInfo.CurrentUICulture = new CultureInfo("de");
            var deAttrs = service.GetClientValidationAttributes(fieldId);

            Assert.NotNull(frAttrs);
            Assert.NotNull(deAttrs);
            Assert.Equal("Le champ Email est requis (fr)", frAttrs["data-val-required"]);
            Assert.Equal("Das Feld Email ist erforderlich (de)", deAttrs["data-val-required"]);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void CustomAdapter_ReceivesLocalizedErrorMessage()
    {
        var localizer = new RecordingValidationLocalizer
        {
            ErrorMessageResults = { ["CustomKey"] = "Localized custom message" },
        };
        var service = CreateService(localizer);
        var fieldId = new FieldIdentifier(new LocalizedCustomAdapterModel(), nameof(LocalizedCustomAdapterModel.Value));

        var attrs = service.GetClientValidationAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Equal("Localized custom message", attrs["data-val-custom"]);
    }

    [Fact]
    public void ResourceTypeDisplayAttribute_ResolvesLocalizedDisplayNameInErrorMessage()
    {
        // No localizer configured. The [Display(ResourceType=..., Name=...)] strategy resolves
        // the display name itself via DisplayAttribute.GetName(), and that value flows into
        // attribute.FormatErrorMessage().
        var fieldId = new FieldIdentifier(new ResourceDisplayModel(), nameof(ResourceDisplayModel.Field));

        var attrs = _service.GetClientValidationAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Contains(TestResources.ResourceKey, (string)attrs["data-val-required"]);
    }

    [Fact]
    public void NoDisplayAttribute_NoLocalizer_UsesMemberNameInErrorMessage()
    {
        // Sanity check: without [Display] and without a localizer, the message uses MemberName.
        var fieldId = new FieldIdentifier(new RequiredModel(), nameof(RequiredModel.Name));

        var attrs = _service.GetClientValidationAttributes(fieldId);

        Assert.NotNull(attrs);
        Assert.Equal("The Name field is required.", attrs["data-val-required"]);
    }

    // ---- Test doubles & models for the localization tests ----

    private sealed class RecordingValidationLocalizer : IValidationLocalizer
    {
        public List<DisplayNameLocalizationContext> DisplayNameCalls { get; } = new();
        public List<ErrorMessageLocalizationContext> ErrorMessageCalls { get; } = new();
        public Dictionary<string, string?> DisplayNameResults { get; } = new();
        public Dictionary<string, string?> ErrorMessageResults { get; } = new();

        public string? ResolveDisplayName(in DisplayNameLocalizationContext context)
        {
            DisplayNameCalls.Add(context);
            return context.DisplayName is not null && DisplayNameResults.TryGetValue(context.DisplayName, out var v) ? v : null;
        }

        public string? ResolveErrorMessage(in ErrorMessageLocalizationContext context)
        {
            ErrorMessageCalls.Add(context);
            var key = context.Attribute.ErrorMessage ?? context.Attribute.GetType().Name;
            return ErrorMessageResults.TryGetValue(key, out var v) ? v : null;
        }
    }

    private sealed class ThrowingDisplayNameLocalizer : IValidationLocalizer
    {
        public string? ResolveDisplayName(in DisplayNameLocalizationContext context)
            => throw new InvalidOperationException("ResolveDisplayName must not be called for the resource-attribute path.");

        public string? ResolveErrorMessage(in ErrorMessageLocalizationContext context) => null;
    }

    private sealed class ThrowingErrorMessageLocalizer : IValidationLocalizer
    {
        public string? ResolveDisplayName(in DisplayNameLocalizationContext context) => null;

        public string? ResolveErrorMessage(in ErrorMessageLocalizationContext context)
            => throw new InvalidOperationException("ResolveErrorMessage must not be called when ErrorMessageResourceType is set.");
    }

    private sealed class CultureBasedLocalizer : IValidationLocalizer
    {
        public string? ResolveDisplayName(in DisplayNameLocalizationContext context) => null;

        public string? ResolveErrorMessage(in ErrorMessageLocalizationContext context)
            => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
            {
                "fr" => $"Le champ {context.DisplayName} est requis (fr)",
                "de" => $"Das Feld {context.DisplayName} ist erforderlich (de)",
                _ => $"The {context.DisplayName} field is required (en)",
            };
    }

    private class LocalizedRequiredModel
    {
        [Required(ErrorMessage = "RequiredKey")]
        public string Email { get; set; } = "";
    }

    private class LocalizedRangeModel
    {
        [Range(18, 120, ErrorMessage = "RangeKey")]
        public int Age { get; set; }
    }

    private class LocalizedCustomAdapterModel
    {
        [LocalizedCustomValidation(ErrorMessage = "CustomKey")]
        public string Value { get; set; } = "";
    }

    private sealed class LocalizedCustomValidationAttribute : ValidationAttribute, IClientValidationAdapter
    {
        public IEnumerable<ClientValidationRule> GetClientValidationRules(string errorMessage)
            => [new ClientValidationRule("custom", errorMessage)];
    }

    private class ResourceDisplayModel
    {
        [Required]
        [Display(Name = nameof(TestResources.ResourceKey), ResourceType = typeof(TestResources))]
        public string Field { get; set; } = "";
    }

    private class ResourceErrorModel
    {
        [Required(ErrorMessageResourceType = typeof(TestResources), ErrorMessageResourceName = nameof(TestResources.RequiredError))]
        public string Field { get; set; } = "";
    }

    private class InheritedDisplayBaseModel
    {
        [Required]
        [Display(Name = "Base Display")]
        public string BaseDisplayProp { get; set; } = "";
    }

    private class InheritedDisplayDerivedModel : InheritedDisplayBaseModel
    {
    }

    public static class TestResources
    {
        public static string ResourceKey => "Localized Display";
        public static string RequiredError => "The {0} field is REQUIRED (resourced)";
    }
}
