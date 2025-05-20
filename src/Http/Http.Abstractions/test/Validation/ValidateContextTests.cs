#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Microsoft.AspNetCore.Http.Validation.Tests;

public class ValidateContextTests
{
    [Fact]
    public void FormatKey_NoJsonOptions_ReturnsSameKey()
    {
        // Arrange
        var context = new ValidateContext
        {
            ValidationOptions = new ValidationOptions(),
            ValidationContext = new ValidationContext(new object())
        };

        // Act & Assert - Use reflection to access internal method
        var method = typeof(ValidateContext).GetMethod("FormatKey", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var formattedKey = (string)method!.Invoke(context, new object[] { "PropertyName" })!;

        Assert.Equal("PropertyName", formattedKey);
    }

    [Fact]
    public void FormatKey_WithCamelCasePolicy_ReturnsCamelCaseKey()
    {
        // Arrange
        var context = new ValidateContext
        {
            ValidationOptions = new ValidationOptions(),
            ValidationContext = new ValidationContext(new object()),
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        };

        // Act & Assert - Use reflection to access internal method
        var method = typeof(ValidateContext).GetMethod("FormatKey", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var formattedKey = (string)method!.Invoke(context, new object[] { "PropertyName" })!;

        Assert.Equal("propertyName", formattedKey);
    }

    [Fact]
    public void FormatKey_WithSnakeCasePolicy_ReturnsSnakeCaseKey()
    {
        // Arrange
        var context = new ValidateContext
        {
            ValidationOptions = new ValidationOptions(),
            ValidationContext = new ValidationContext(new object()),
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            }
        };

        // Act & Assert - Use reflection to access internal method
        var method = typeof(ValidateContext).GetMethod("FormatKey", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var formattedKey = (string)method!.Invoke(context, new object[] { "PropertyName" })!;

        Assert.Equal("property_name", formattedKey);
    }

    [Fact]
    public void FormatKey_WithNestedPath_AppliesNamingPolicyToPathSegments()
    {
        // Arrange
        var context = new ValidateContext
        {
            ValidationOptions = new ValidationOptions(),
            ValidationContext = new ValidationContext(new object()),
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        };

        // Act & Assert - Use reflection to access internal method
        var method = typeof(ValidateContext).GetMethod("FormatKey", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var formattedKey = (string)method!.Invoke(context, new object[] { "Customer.Address.StreetName" })!;

        Assert.Equal("customer.address.streetName", formattedKey);
    }

    [Fact]
    public void FormatKey_WithArrayIndices_PreservesIndicesAndAppliesNamingPolicy()
    {
        // Arrange
        var context = new ValidateContext
        {
            ValidationOptions = new ValidationOptions(),
            ValidationContext = new ValidationContext(new object()),
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        };

        // Act & Assert - Use reflection to access internal method
        var method = typeof(ValidateContext).GetMethod("FormatKey", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var formattedKey = (string)method!.Invoke(context, new object[] { "Orders[0].OrderItems[2].ProductName" })!;

        Assert.Equal("orders[0].orderItems[2].productName", formattedKey);
    }

    [Fact]
    public void AddValidationError_WithNamingPolicy_FormatsKeyAccordingly()
    {
        // Arrange
        var context = new ValidateContext
        {
            ValidationOptions = new ValidationOptions(),
            ValidationContext = new ValidationContext(new object()),
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        };

        // Act
        // Use reflection to access internal method
        var method = typeof(ValidateContext).GetMethod("AddValidationError", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(context, new object[] { "FirstName", new[] { "Error message" } });

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.Single(context.ValidationErrors);
        Assert.True(context.ValidationErrors.ContainsKey("firstName"));
        Assert.Single(context.ValidationErrors["firstName"]);
        Assert.Equal("Error message", context.ValidationErrors["firstName"][0]);
    }

    [Fact]
    public void AddOrExtendValidationError_WithNamingPolicy_FormatsKeyAccordingly()
    {
        // Arrange
        var context = new ValidateContext
        {
            ValidationOptions = new ValidationOptions(),
            ValidationContext = new ValidationContext(new object()),
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        };

        // Act
        // Use reflection to access internal method
        var method = typeof(ValidateContext).GetMethod("AddOrExtendValidationError", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(context, new object[] { "FirstName", "Error message 1" });
        method!.Invoke(context, new object[] { "FirstName", "Error message 2" });

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.Single(context.ValidationErrors);
        Assert.True(context.ValidationErrors.ContainsKey("firstName"));
        Assert.Equal(2, context.ValidationErrors["firstName"].Length);
        Assert.Equal("Error message 1", context.ValidationErrors["firstName"][0]);
        Assert.Equal("Error message 2", context.ValidationErrors["firstName"][1]);
    }

    [Fact]
    public void AddOrExtendValidationErrors_WithNamingPolicy_FormatsKeyAccordingly()
    {
        // Arrange
        var context = new ValidateContext
        {
            ValidationOptions = new ValidationOptions(),
            ValidationContext = new ValidationContext(new object()),
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        };

        // Act
        // Use reflection to access internal method
        var method = typeof(ValidateContext).GetMethod("AddOrExtendValidationErrors", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(context, new object[] { "FirstName", new[] { "Error message 1", "Error message 2" } });
        method!.Invoke(context, new object[] { "FirstName", new[] { "Error message 3" } });

        // Assert
        Assert.NotNull(context.ValidationErrors);
        Assert.Single(context.ValidationErrors);
        Assert.True(context.ValidationErrors.ContainsKey("firstName"));
        Assert.Equal(3, context.ValidationErrors["firstName"].Length);
        Assert.Equal("Error message 1", context.ValidationErrors["firstName"][0]);
        Assert.Equal("Error message 2", context.ValidationErrors["firstName"][1]);
        Assert.Equal("Error message 3", context.ValidationErrors["firstName"][2]);
    }
}