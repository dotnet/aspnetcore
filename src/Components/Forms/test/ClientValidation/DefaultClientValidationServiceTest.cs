// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Components.Forms;

public class DefaultClientValidationServiceTest
{
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
        registry.AddAdapter<UrlAttribute>(_ => new DataTypeClientAdapter("data-val-url"));
        registry.AddAdapter<CreditCardAttribute>(_ => new DataTypeClientAdapter("data-val-creditcard"));
        registry.AddAdapter<PhoneAttribute>(_ => new DataTypeClientAdapter("data-val-phone"));
        registry.AddAdapter<CompareAttribute>(a => new CompareClientAdapter(a));

        return registry;
    }

    private static DefaultClientValidationService CreateService(
        ClientValidationAdapterRegistry? registry = null)
    {
        registry ??= CreateRegistryWithBuiltIns();
        return new DefaultClientValidationService(registry);
    }

    private static FieldIdentifier CreateFieldIdentifier<T>(T model, string fieldName) where T : class
    {
        return new FieldIdentifier(model, fieldName);
    }

    [Fact]
    public void GetValidationAttributes_ReturnsCorrectAttributes_ForRequiredField()
    {
        var service = CreateService();
        var model = new TestModel();
        var field = CreateFieldIdentifier(model, nameof(TestModel.Name));

        var result = service.GetValidationAttributes(field);

        Assert.Equal("true", result["data-val"]);
        Assert.True(result.ContainsKey("data-val-required"));
        Assert.True(result.ContainsKey("data-val-length"));
        Assert.Equal("2", result["data-val-length-min"]);
        Assert.Equal("100", result["data-val-length-max"]);
    }

    [Fact]
    public void GetValidationAttributes_ReturnsCorrectAttributes_ForEmailField()
    {
        var service = CreateService();
        var model = new TestModel();
        var field = CreateFieldIdentifier(model, nameof(TestModel.Email));

        var result = service.GetValidationAttributes(field);

        Assert.Equal("true", result["data-val"]);
        Assert.True(result.ContainsKey("data-val-required"));
        Assert.True(result.ContainsKey("data-val-email"));
    }

    [Fact]
    public void GetValidationAttributes_ReturnsEmpty_ForFieldWithNoAttributes()
    {
        var service = CreateService();
        var model = new TestModel();
        var field = CreateFieldIdentifier(model, nameof(TestModel.Optional));

        var result = service.GetValidationAttributes(field);

        Assert.Empty(result);
    }

    [Fact]
    public void GetValidationAttributes_ReturnsEmpty_ForNonExistentField()
    {
        var service = CreateService();
        var model = new TestModel();
        var field = CreateFieldIdentifier(model, "DoesNotExist");

        var result = service.GetValidationAttributes(field);

        Assert.Empty(result);
    }

    [Fact]
    public void GetValidationAttributes_UsesDisplayName_FromDisplayAttribute()
    {
        var service = CreateService();
        var model = new TestModel();
        var field = CreateFieldIdentifier(model, nameof(TestModel.Name));

        var result = service.GetValidationAttributes(field);

        // The Required error message is explicit, but StringLength uses FormatErrorMessage
        // which incorporates the display name "Full Name"
        Assert.Contains("Full Name", result["data-val-length"]);
    }

    [Fact]
    public void GetValidationAttributes_FallsBackToPropertyName_WhenNoDisplayAttribute()
    {
        var service = CreateService();
        var model = new TestModel();
        var field = CreateFieldIdentifier(model, nameof(TestModel.Email));

        var result = service.GetValidationAttributes(field);

        // Email has [Required] with no custom ErrorMessage, so FormatErrorMessage
        // uses the property name "Email" as the display name
        Assert.Contains("Email", result["data-val-required"]);
    }

    [Fact]
    public void GetValidationAttributes_UsesDisplayNameAttribute()
    {
        var service = CreateService();
        var model = new DisplayNameModel();
        var field = CreateFieldIdentifier(model, nameof(DisplayNameModel.UserName));

        var result = service.GetValidationAttributes(field);

        Assert.Contains("User Name", result["data-val-required"]);
    }

    [Fact]
    public void GetValidationAttributes_CachesResults()
    {
        var service = CreateService();
        var model = new TestModel();
        var field = CreateFieldIdentifier(model, nameof(TestModel.Name));

        var result1 = service.GetValidationAttributes(field);
        var result2 = service.GetValidationAttributes(field);

        Assert.Same(result1, result2);
    }

    [Fact]
    public void GetValidationAttributes_DifferentModelsOfSameType_ShareCache()
    {
        var service = CreateService();
        var model1 = new TestModel();
        var model2 = new TestModel();

        var result1 = service.GetValidationAttributes(CreateFieldIdentifier(model1, nameof(TestModel.Name)));
        var result2 = service.GetValidationAttributes(CreateFieldIdentifier(model2, nameof(TestModel.Name)));

        Assert.Same(result1, result2);
    }

    [Fact]
    public void GetValidationAttributes_UsesCustomAdapter()
    {
        var registry = CreateRegistryWithBuiltIns();
        registry.AddAdapter<CustomTestAttribute>(a =>
        {
            return new CustomTestAdapter();
        });

        var service = CreateService(registry);
        var model = new CustomAttributeModel();
        var field = CreateFieldIdentifier(model, nameof(CustomAttributeModel.Value));

        var result = service.GetValidationAttributes(field);

        Assert.True(result.ContainsKey("data-val-custom"));
    }

    [Fact]
    public void GetValidationAttributes_RequiredAttribute_UsesExplicitErrorMessage()
    {
        var service = CreateService();
        var model = new TestModel();
        var field = CreateFieldIdentifier(model, nameof(TestModel.Name));

        var result = service.GetValidationAttributes(field);

        Assert.Equal("Name is required.", result["data-val-required"]);
    }

    [Fact]
    public void GetValidationAttributes_RangeAttribute_EmitsMinMax()
    {
        var service = CreateService();
        var model = new RangeModel();
        var field = CreateFieldIdentifier(model, nameof(RangeModel.Age));

        var result = service.GetValidationAttributes(field);

        Assert.Equal("true", result["data-val"]);
        Assert.True(result.ContainsKey("data-val-range"));
        Assert.Equal("1", result["data-val-range-min"]);
        Assert.Equal("120", result["data-val-range-max"]);
    }

    [Fact]
    public void GetValidationAttributes_CompareAttribute_EmitsOtherWithPrefix()
    {
        var service = CreateService();
        var model = new CompareModel();
        var field = CreateFieldIdentifier(model, nameof(CompareModel.ConfirmPassword));

        var result = service.GetValidationAttributes(field);

        Assert.Equal("*.Password", result["data-val-equalto-other"]);
    }

    [Fact]
    public void GetValidationAttributes_RegexAttribute_EmitsPattern()
    {
        var service = CreateService();
        var model = new RegexModel();
        var field = CreateFieldIdentifier(model, nameof(RegexModel.ZipCode));

        var result = service.GetValidationAttributes(field);

        Assert.Equal(@"^\d{5}$", result["data-val-regex-pattern"]);
    }

    // Test models

    private class TestModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        public string Optional { get; set; } = "";
    }

    private class DisplayNameModel
    {
        [Required]
        [DisplayName("User Name")]
        public string UserName { get; set; } = "";
    }

    private class CustomAttributeModel
    {
        [CustomTest]
        public string Value { get; set; } = "";
    }

    private class CustomTestAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => ValidationResult.Success;
    }

    private class CustomTestAdapter : IClientValidationAdapter
    {
        public void AddClientValidation(in ClientValidationContext context, string errorMessage)
        {
            context.MergeAttribute("data-val", "true");
            context.MergeAttribute("data-val-custom", errorMessage);
        }
    }

    private class RangeModel
    {
        [Range(1, 120)]
        public int Age { get; set; }
    }

    private class CompareModel
    {
        public string Password { get; set; } = "";

        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = "";
    }

    private class RegexModel
    {
        [RegularExpression(@"^\d{5}$")]
        public string ZipCode { get; set; } = "";
    }
}
