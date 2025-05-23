// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Http.Validation;

public class ValidateContextTests
{
    [Fact]
    public void AddValidationError_FormatsCamelCaseKeys_WithSerializerOptions()
    {
        // Arrange
        var context = CreateValidateContext();
        context.SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        context.AddValidationError("PropertyName", ["Error"]);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.True(context.ValidationErrors.ContainsKey("propertyName"));
    }

    [Fact]
    public void AddValidationError_FormatsSimpleKeys_WithSerializerOptions()
    {
        // Arrange
        var context = CreateValidateContext();
        context.SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        context.AddValidationError("ThisIsAProperty", ["Error"]);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.True(context.ValidationErrors.ContainsKey("thisIsAProperty"));
    }

    [Fact]
    public void FormatComplexKey_FormatsNestedProperties_WithDots()
    {
        // Arrange
        var context = CreateValidateContext();
        context.SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        context.AddValidationError("Customer.Address.Street", ["Error"]);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.True(context.ValidationErrors.ContainsKey("customer.address.street"));
    }

    [Fact]
    public void FormatComplexKey_PreservesArrayIndices()
    {
        // Arrange
        var context = CreateValidateContext();
        context.SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        context.AddValidationError("Items[0].ProductName", ["Error"]);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.True(context.ValidationErrors.ContainsKey("items[0].productName"));
        Assert.False(context.ValidationErrors.ContainsKey("items[0].ProductName"));
    }

    [Fact]
    public void FormatComplexKey_HandlesMultipleArrayIndices()
    {
        // Arrange
        var context = CreateValidateContext();
        context.SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        context.AddValidationError("Orders[0].Items[1].ProductName", ["Error"]);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.True(context.ValidationErrors.ContainsKey("orders[0].items[1].productName"));
    }

    [Fact]
    public void FormatComplexKey_HandlesNestedArraysWithoutProperties()
    {
        // Arrange
        var context = CreateValidateContext();
        context.SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        context.AddValidationError("Matrix[0][1]", ["Error"]);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.True(context.ValidationErrors.ContainsKey("matrix[0][1]"));
    }

    [Fact]
    public void FormatKey_ReturnsOriginalKey_WhenSerializerOptionsIsNull()
    {
        // Arrange
        var context = CreateValidateContext();
        context.SerializerOptions = null;

        // Act
        context.AddValidationError("PropertyName", ["Error"]);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.True(context.ValidationErrors.ContainsKey("PropertyName"));
    }

    [Fact]
    public void FormatKey_ReturnsOriginalKey_WhenPropertyNamingPolicyIsNull()
    {
        // Arrange
        var context = CreateValidateContext();
        context.SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        };

        // Act
        context.AddValidationError("PropertyName", ["Error"]);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.True(context.ValidationErrors.ContainsKey("PropertyName"));
    }

    [Fact]
    public void FormatKey_AppliesKebabCaseNamingPolicy()
    {
        // Arrange
        var context = CreateValidateContext();
        context.SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new KebabCaseNamingPolicy()
        };

        // Act
        context.AddValidationError("ProductName", ["Error"]);
        context.AddValidationError("OrderItems[0].ProductName", ["Error"]);

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.True(context.ValidationErrors.ContainsKey("product-name"));
        Assert.True(context.ValidationErrors.ContainsKey("order-items[0].product-name"));
    }

    private static ValidateContext CreateValidateContext()
    {
        var serviceProvider = new EmptyServiceProvider();
        var options = new ValidationOptions();
        var validationContext = new ValidationContext(new object(), serviceProvider, null);
        
        return new ValidateContext
        {
            ValidationContext = validationContext,
            ValidationOptions = options
        };
    }

    private class KebabCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var result = string.Empty;

            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]))
                {
                    result += "-";
                }

                result += char.ToLower(name[i], CultureInfo.InvariantCulture);
            }

            return result;
        }
    }

    private class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}