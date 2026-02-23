#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation.Localization;
using Microsoft.Extensions.Validation.LocalizationTests.Helpers;

namespace Microsoft.Extensions.Validation.LocalizationTests;

public class ErrorMessageProviderTests
{
    [Fact]
    public async Task ErrorMessageProvider_NotConfigured_UsesDefaultMessages()
    {
        var customer = new TestCustomer { Name = null, Age = 25 };
        var context = CreateContext(customer);

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(customer, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.True(context.ValidationErrors.ContainsKey("Name"));
        Assert.Equal("The Name field is required.", context.ValidationErrors["Name"].First());
    }

    [Fact]
    public async Task ErrorMessageProvider_ReturnsNull_FallsThrough()
    {
        var customer = new TestCustomer { Name = null, Age = 25 };
        var options = new ValidationOptions
        {
            ErrorMessageProvider = (in _) => null
        };
        var context = CreateContext(customer, options);

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(customer, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.True(context.ValidationErrors.ContainsKey("Name"));
        Assert.Equal("The Name field is required.", context.ValidationErrors["Name"].First());
    }

    [Fact]
    public async Task ErrorMessageProvider_ReturnsCustomMessage_UsesIt()
    {
        var customer = new TestCustomer { Name = null, Age = 25 };
        var options = new ValidationOptions
        {
            ErrorMessageProvider = (in ctx) => $"Custom: {ctx.MemberName} is invalid"
        };
        var context = CreateContext(customer, options);

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(customer, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.True(context.ValidationErrors.ContainsKey("Name"));
        Assert.Equal("Custom: Name is invalid", context.ValidationErrors["Name"].First());
    }

    [Fact]
    public async Task ErrorMessageProvider_OnValidateContext_OverridesOptions()
    {
        var customer = new TestCustomer { Name = null, Age = 25 };
        var options = new ValidationOptions
        {
            ErrorMessageProvider = (in _) => "Options-level message"
        };
        var context = CreateContext(customer, options);
        context.ErrorMessageProvider = (in _) => "Context-level message";

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(customer, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Context-level message", context.ValidationErrors["Name"].First());
    }

    [Fact]
    public async Task ErrorMessageProvider_SkippedWhenErrorMessageResourceTypeIsSet()
    {
        var providerCalled = false;
        var options = new ValidationOptions
        {
            ErrorMessageProvider = (in _) =>
            {
                providerCalled = true;
                return "Should not be used";
            }
        };

        var model = new ResourceTypeModel { Value = null };
        var requiredAttr = new RequiredAttribute
        {
            ErrorMessageResourceType = typeof(TestResources),
            ErrorMessageResourceName = nameof(TestResources.RequiredError)
        };

        var propInfo = new TestValidatablePropertyInfo(
            typeof(ResourceTypeModel), typeof(string), "Value",
            [requiredAttr]);
        var typeInfo = new TestValidatableTypeInfo(typeof(ResourceTypeModel), [propInfo]);

        var context = CreateContext(model, options);
        await typeInfo.ValidateAsync(model, context, default);

        Assert.False(providerCalled);
    }

    [Fact]
    public async Task ErrorMessageProvider_ReceivesCorrectErrorMessageContext()
    {
        ErrorMessageProviderContext? captured = null;
        var customer = new TestCustomer { Name = null, Age = 25 };
        var options = new ValidationOptions
        {
            ErrorMessageProvider = (in ctx) =>
            {
                captured = ctx;
                return null;
            }
        };
        var context = CreateContext(customer, options);

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(customer, context, default);

        Assert.NotNull(captured);
        Assert.IsType<RequiredAttribute>(captured.Value.Attribute);
        Assert.Equal("Name", captured.Value.MemberName);
        Assert.Equal(typeof(TestCustomer), captured.Value.DeclaringType);
        Assert.NotNull(captured.Value.Services);
    }

    [Fact]
    public async Task ErrorMessageProvider_ReceivesDefaultTemplateWhenErrorMessageNotSet()
    {
        ErrorMessageProviderContext? captured = null;
        var customer = new TestCustomer { Name = null, Age = 25 };
        var options = new ValidationOptions
        {
            ErrorMessageProvider = (in ctx) =>
            {
                captured = ctx;
                return null;
            }
        };
        var context = CreateContext(customer, options);

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(customer, context, default);

        Assert.NotNull(captured);
        Assert.Equal("The Name field is required.", context.ValidationErrors!["Name"][0]);
    }

    [Fact]
    public async Task ErrorMessageProvider_ReceivesCustomErrorMessageWhenSet()
    {
        ErrorMessageProviderContext? captured = null;
        var model = new SimpleModel { Name = null };
        var requiredAttr = new RequiredAttribute { ErrorMessage = "Please fill in {0}" };
        var propInfo = new TestValidatablePropertyInfo(
            typeof(SimpleModel), typeof(string), "Name",
            [requiredAttr]);
        var typeInfo = new TestValidatableTypeInfo(typeof(SimpleModel), [propInfo]);

        var options = new ValidationOptions
        {
            ErrorMessageProvider = (in ctx) =>
            {
                captured = ctx;
                return null;
            }
        };
        var context = CreateContext(model, options);
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(captured);
        Assert.Equal("Please fill in Name", context.ValidationErrors!["Name"][0]);
    }

    [Fact]
    public async Task ErrorMessageProvider_WorksWithRequiredAttribute()
    {
        var customer = new TestCustomer { Name = null, Age = 25 };
        var options = new ValidationOptions
        {
            ErrorMessageProvider = (in ctx) =>
            {
                if (ctx.Attribute is RequiredAttribute)
                {
                    return $"{ctx.DisplayName} is mandatory";
                }
                return null;
            }
        };
        var context = CreateContext(customer, options);

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(customer, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Name is mandatory", context.ValidationErrors["Name"].First());
    }

    [Fact]
    public async Task ErrorMessageProvider_WorksWithRangeAttribute()
    {
        var customer = new TestCustomer { Name = "Test", Age = 200 };
        var options = new ValidationOptions
        {
            ErrorMessageProvider = (in ctx) =>
            {
                if (ctx.Attribute is RangeAttribute range)
                {
                    return $"{ctx.DisplayName} must be {range.Minimum}-{range.Maximum}";
                }
                return null;
            }
        };
        var context = CreateContext(customer, options);

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(customer, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Customer Age must be 18-120", context.ValidationErrors["Age"].First());
    }

    [Fact]
    public async Task ErrorMessageProvider_WorksWithCustomAttribute()
    {
        var model = new CustomAttrModel { Code = "abc123" };
        var customAttr = new AlphaOnlyAttribute();
        var propInfo = new TestValidatablePropertyInfo(
            typeof(CustomAttrModel), typeof(string), "Code",
            [customAttr]);
        var typeInfo = new TestValidatableTypeInfo(typeof(CustomAttrModel), [propInfo]);

        var options = new ValidationOptions
        {
            ErrorMessageProvider = (in ctx) => $"Localized: {ctx.MemberName} format error"
        };
        var context = CreateContext(model, options);
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Localized: Code format error", context.ValidationErrors["Code"].First());
    }

    [Fact]
    public async Task ErrorMessageProvider_WorksWithTypeLevelAttributes()
    {
        var model = new TypeValidatedModel { Start = 10, End = 5 };
        var rangeCheckAttr = new StartLessThanEndAttribute();
        var typeInfo = new TestValidatableTypeInfo(
            typeof(TypeValidatedModel),
            [
                new TestValidatablePropertyInfo(typeof(TypeValidatedModel), typeof(int), "Start", []),
                new TestValidatablePropertyInfo(typeof(TypeValidatedModel), typeof(int), "End", [])
            ],
            validationAttributes: [rangeCheckAttr]);

        var options = new ValidationOptions
        {
            ErrorMessageProvider = (in ctx) => $"Localized type error: {ctx.MemberName}"
        };
        var context = CreateContext(model, options);
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        var errorEntry = context.ValidationErrors.Values.First();
        Assert.Contains("Localized type error:", errorEntry.First());
    }

    [Fact]
    public async Task ErrorMessageProvider_StringLengthWithMinimum_UsesAlternateTemplate()
    {
        ErrorMessageProviderContext? captured = null;
        var model = new SimpleModel { Name = "ab" };
        var stringLengthAttr = new StringLengthAttribute(100) { MinimumLength = 3 };
        var propInfo = new TestValidatablePropertyInfo(
            typeof(SimpleModel), typeof(string), "Name",
            [stringLengthAttr]);
        var typeInfo = new TestValidatableTypeInfo(typeof(SimpleModel), [propInfo]);

        var options = new ValidationOptions
        {
            ErrorMessageProvider = (in ctx) =>
            {
                captured = ctx;
                return null;
            }
        };
        var context = CreateContext(model, options);
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(captured);
        Assert.Equal(
            "The field Name must be a string with a minimum length of 3 and a maximum length of 100.",
            context.ValidationErrors!["Name"][0]);
    }

    [Fact]
    public async Task ErrorMessageProvider_StringLengthWithoutMinimum_UsesStandardTemplate()
    {
        ErrorMessageProviderContext? captured = null;
        var model = new LongNameModel { Name = new string('a', 101) };
        var stringLengthAttr = new StringLengthAttribute(100);
        var propInfo = new TestValidatablePropertyInfo(
            typeof(LongNameModel), typeof(string), "Name",
            [stringLengthAttr]);
        var typeInfo = new TestValidatableTypeInfo(typeof(LongNameModel), [propInfo]);

        var options = new ValidationOptions
        {
            ErrorMessageProvider = (in ctx) =>
            {
                captured = ctx;
                return null;
            }
        };
        var context = CreateContext(model, options);
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(captured);
        Assert.Equal(
            "The field Name must be a string with a maximum length of 100.",
            context.ValidationErrors!["Name"][0]);
    }

    private static TestValidatableTypeInfo CreateCustomerTypeInfo()
    {
        return new TestValidatableTypeInfo(
            typeof(TestCustomer),
            [
                new TestValidatablePropertyInfo(typeof(TestCustomer), typeof(string), "Name",
                    [new RequiredAttribute(), new StringLengthAttribute(100)]),
                new TestValidatablePropertyInfo(typeof(TestCustomer), typeof(int), "Age",
                    [new RangeAttribute(18, 120)],
                    displayName: "Customer Age"),
                new TestValidatablePropertyInfo(typeof(TestCustomer), typeof(string), "Email",
                    [new EmailAddressAttribute()])
            ]);
    }

    private static ValidateContext CreateContext(object model, ValidationOptions? options = null)
    {
        options ??= new ValidationOptions();
        return new ValidateContext
        {
            ValidationOptions = options,
            ValidationContext = new ValidationContext(model)
        };
    }

    public class TestCustomer
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? Email { get; set; }
    }

    private class SimpleModel
    {
        public string? Name { get; set; }
    }

    private class LongNameModel
    {
        public string? Name { get; set; }
    }

    private class ResourceTypeModel
    {
        public string? Value { get; set; }
    }

    private class CustomAttrModel
    {
        public string? Code { get; set; }
    }

    private class TypeValidatedModel
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    internal static class TestResources
    {
        public static string RequiredError => "This field is required.";
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
            if (value is TypeValidatedModel model && model.Start >= model.End)
            {
                return new ValidationResult(
                    ErrorMessage ?? "Start must be less than End.",
                    [nameof(TypeValidatedModel.Start)]);
            }
            return ValidationResult.Success;
        }
    }
}
