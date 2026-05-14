// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms.ClientValidation;

namespace Microsoft.AspNetCore.Components.Forms;

public class DefaultClientValidationServiceTest
{
    private readonly DefaultClientValidationService _service = new();

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
    public void CachesResultsForSameProperty()
    {
        var model1 = new RequiredModel();
        var model2 = new RequiredModel();
        var fieldId1 = new FieldIdentifier(model1, nameof(RequiredModel.Name));
        var fieldId2 = new FieldIdentifier(model2, nameof(RequiredModel.Name));

        var attrs1 = _service.GetClientValidationAttributes(fieldId1);
        var attrs2 = _service.GetClientValidationAttributes(fieldId2);

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
}
