#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation.Localization;
using Microsoft.Extensions.Validation.LocalizationTests.Helpers;

namespace Microsoft.Extensions.Validation.LocalizationTests;

public class DisplayNameProviderTests
{
    [Fact]
    public async Task DisplayNameProvider_NotConfigured_UsesDefaultName()
    {
        var model = new CustomerModel { Name = null, Age = 25 };
        var context = CreateContext(model);

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The Name field is required.", context.ValidationErrors["Name"].First());
    }

    [Fact]
    public async Task DisplayNameProvider_ReturnsNull_FallsThrough()
    {
        var model = new CustomerModel { Name = null, Age = 25 };
        var options = new ValidationOptions
        {
            DisplayNameProvider = _ => null
        };
        var context = CreateContext(model, options);

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The Name field is required.", context.ValidationErrors["Name"].First());
    }

    [Fact]
    public async Task DisplayNameProvider_ReturnsCustomName_UsesIt()
    {
        var model = new CustomerModel { Name = "Test", Age = 200 };
        var options = new ValidationOptions
        {
            DisplayNameProvider = ctx => ctx.Name == "Customer Age" ? "Âge du client" : null
        };
        var context = CreateContext(model, options);

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The field Âge du client must be between 18 and 120.", context.ValidationErrors["Age"].First());
    }

    [Fact]
    public async Task DisplayNameProvider_OnValidateContext_OverridesOptions()
    {
        var model = new CustomerModel { Name = null, Age = 25 };
        var options = new ValidationOptions
        {
            DisplayNameProvider = ctx => ctx.Name == "Customer Age" ? "Options Age" : null
        };
        var context = CreateContext(model, options);
        context.DisplayNameProvider = ctx => ctx.Name == "Customer Age" ? "Context Age" : null;

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.DoesNotContain("Options Age", context.ValidationErrors.Values.SelectMany(v => v));
    }

    [Fact]
    public async Task DisplayNameProvider_WithDisplayAttribute_PassesDisplayName()
    {
        DisplayNameContext? captured = null;
        var model = new CustomerModel { Name = "Test", Age = 200 };
        var options = new ValidationOptions
        {
            DisplayNameProvider = ctx =>
            {
                captured = ctx;
                return null;
            }
        };
        var context = CreateContext(model, options);

        var typeInfo = CreateCustomerTypeInfo();
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(captured);
        Assert.Equal("Customer Age", captured.Value.Name);
        Assert.Equal(typeof(CustomerModel), captured.Value.DeclaringType);
    }

    [Fact]
    public async Task DisplayNameProvider_WithDisplayResourceType_SkipsProvider()
    {
        var providerCalled = false;
        var model = new ResourceDisplayModel { Value = null };
        var displayAttr = new DisplayAttribute
        {
            Name = nameof(DisplayResources.ValueLabel),
            ResourceType = typeof(DisplayResources)
        };
        var propInfo = new TestValidatablePropertyInfo(
            typeof(ResourceDisplayModel), typeof(string), "Value",
            [new RequiredAttribute()],
            displayAttr);
        var typeInfo = new TestValidatableTypeInfo(typeof(ResourceDisplayModel), [propInfo]);

        var options = new ValidationOptions
        {
            DisplayNameProvider = _ =>
            {
                providerCalled = true;
                return "Should not be used";
            }
        };
        var context = CreateContext(model, options);
        await typeInfo.ValidateAsync(model, context, default);

        Assert.False(providerCalled);
    }

    [Fact]
    public async Task DisplayNameProvider_LocalizedNameAppearsInErrorMessage()
    {
        var model = new CustomerModel { Name = null, Age = 25 };
        var options = new ValidationOptions
        {
            DisplayNameProvider = ctx => ctx.Name switch
            {
                "Name" => "Nom",
                "Customer Age" => "Âge",
                _ => null
            }
        };
        var context = CreateContext(model, options);

        var typeInfo = CreateCustomerTypeInfoWithDisplayOnName();
        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The Nom field is required.", context.ValidationErrors["Name"].First());
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
                    new DisplayAttribute { Name = "Customer Age" }),
                new TestValidatablePropertyInfo(typeof(CustomerModel), typeof(string), "Email",
                    [new EmailAddressAttribute()])
            ]);
    }

    private static TestValidatableTypeInfo CreateCustomerTypeInfoWithDisplayOnName()
    {
        return new TestValidatableTypeInfo(
            typeof(CustomerModel),
            [
                new TestValidatablePropertyInfo(typeof(CustomerModel), typeof(string), "Name",
                    [new RequiredAttribute(), new StringLengthAttribute(100)],
                    new DisplayAttribute { Name = "Name" }),
                new TestValidatablePropertyInfo(typeof(CustomerModel), typeof(int), "Age",
                    [new RangeAttribute(18, 120)],
                    new DisplayAttribute { Name = "Customer Age" }),
                new TestValidatablePropertyInfo(typeof(CustomerModel), typeof(string), "Email",
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

    public class CustomerModel
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? Email { get; set; }
    }

    private class ResourceDisplayModel
    {
        public string? Value { get; set; }
    }

    internal static class DisplayResources
    {
        public static string ValueLabel => "The Value";
    }
}
