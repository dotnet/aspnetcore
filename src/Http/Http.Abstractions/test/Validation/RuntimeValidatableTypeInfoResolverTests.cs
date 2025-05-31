#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Validation.Tests;

public class RuntimeValidatableTypeInfoResolverTests
{
    private readonly RuntimeValidatableTypeInfoResolver _resolver = new();
    private readonly ValidationOptions _validationOptions;

    public RuntimeValidatableTypeInfoResolverTests()
    {
        _validationOptions = new ValidationOptions();
        
        // Register our resolver in the validation options
        _validationOptions.Resolvers.Add(_resolver);
    }

    [Fact]
    public void TryGetValidatableParameterInfo_AlwaysReturnsFalse()
    {
        var parameterInfo = typeof(RuntimeValidatableTypeInfoResolverTests).GetMethod(nameof(TestMethod))!.GetParameters()[0];
        var resolver = new RuntimeValidatableTypeInfoResolver();

        var result = resolver.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(DayOfWeek))] // Enum
    public void TryGetValidatableTypeInfo_WithPrimitiveTypes_ReturnsFalse(Type type)
    {
        var result = _resolver.TryGetValidatableTypeInfo(type, out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithComplexTypeWithValidationAttributes_ReturnsTrue()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PersonWithValidation), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
        Assert.IsType<RuntimeValidatableTypeInfoResolver.RuntimeValidatableTypeInfo>(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithComplexTypeWithoutValidationAttributes_ReturnsFalse()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PersonWithoutValidation), out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithNestedComplexType_ReturnsTrue()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PersonWithNestedValidation), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_CachesResults()
    {
        // Call twice with the same type
        var result1 = _resolver.TryGetValidatableTypeInfo(typeof(PersonWithValidation), out var validatableInfo1);
        var result2 = _resolver.TryGetValidatableTypeInfo(typeof(PersonWithValidation), out var validatableInfo2);

        Assert.True(result1);
        Assert.True(result2);
        Assert.Same(validatableInfo1, validatableInfo2); // Should be the same cached instance
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithCyclicReference_HandlesGracefully()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PersonWithCyclicReference), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
        // Should not throw StackOverflowException due to cycle detection
    }

    [Fact]
    public async Task ValidateAsync_WithRequiredAndRangeValidation_ReturnsErrors()
    {
        // Arrange
        var person = new PersonWithValidation
        {
            Name = "", // Invalid - required
            Age = 150  // Invalid - range is 0-120
        };

        var validationResult = await ValidateInstanceAsync(person);

        // Assert
        Assert.Equal(2, validationResult.Count);
        Assert.Contains(validationResult, e => e.Key == "Name");
        Assert.Contains(validationResult, e => e.Key == "Age");
        Assert.Contains("required", validationResult["Name"][0]);
        Assert.Contains("between", validationResult["Age"][0]);
        Assert.Contains("0", validationResult["Age"][0]);
        Assert.Contains("120", validationResult["Age"][0]);
    }

    [Fact]
    public async Task ValidateAsync_WithDisplayAttribute_UsesDisplayNameInError()
    {
        // Arrange
        var personWithDisplayName = new PersonWithDisplayName
        {
            FirstName = "" // Invalid - required
        };

        var validationResult = await ValidateInstanceAsync(personWithDisplayName);

        // Assert
        Assert.Single(validationResult);
        // Check that the error message contains "First Name" (the display name) rather than "FirstName"
        Assert.Contains("First Name", validationResult["FirstName"][0]);
    }

    [Fact]
    public async Task ValidateAsync_WithNestedValidation_ValidatesNestedProperties()
    {
        // Arrange
        var personWithNested = new PersonWithNestedValidation
        {
            Name = "Valid Name",
            Address = new AddressWithValidation
            {
                Street = "", // Invalid - required
                City = ""    // Invalid - required
            }
        };

        var validationResult = await ValidateInstanceAsync(personWithNested);

        // Assert
        Assert.Equal(2, validationResult.Count);
        foreach (var entry in validationResult)
        {
            if (entry.Key == "Address.Street") 
            { 
                Assert.Contains("Street field is required", entry.Value[0]); 
            }
            else if (entry.Key == "Address.City")
            {
                Assert.Contains("City field is required", entry.Value[0]);
            }
            else
            {
                Assert.Fail($"Unexpected validation error key: {entry.Key}");
            }
        }
    }

    [Fact]
    public async Task ValidateAsync_WithListOfValidatableTypes_ValidatesEachItem()
    {
        // Arrange
        var personWithList = new PersonWithList
        {
            Name = "Valid Name",
            Addresses = new List<AddressWithValidation>
            {
                new AddressWithValidation { Street = "Valid Street", City = "Valid City" }, // Valid
                new AddressWithValidation { Street = "", City = "" }, // Invalid
                new AddressWithValidation { Street = "Another Valid Street", City = "" } // Invalid City only
            }
        };

        var validationResult = await ValidateInstanceAsync(personWithList);

        // Assert
        Assert.Equal(3, validationResult.Count);
        foreach (var entry in validationResult)
        {
            if (entry.Key == "Addresses[1].Street") 
            { 
                Assert.Contains("Street field is required", entry.Value[0]); 
            }
            else if (entry.Key == "Addresses[1].City")
            {
                Assert.Contains("City field is required", entry.Value[0]);
            }
            else if (entry.Key == "Addresses[2].City")
            {
                Assert.Contains("City field is required", entry.Value[0]);
            }
            else
            {
                Assert.Fail($"Unexpected validation error key: {entry.Key}");
            }
        }
    }

    [Fact]
    public async Task ValidateAsync_WithParsableType_ValidatesCorrectly()
    {
        // Arrange
        var personWithParsable = new PersonWithParsableProperty
        {
            Name = "Valid Name",
            Email = "invalid-email" // Invalid - not an email
        };

        var validationResult = await ValidateInstanceAsync(personWithParsable);

        // Assert
        Assert.Single(validationResult);
        Assert.Contains(validationResult, e => e.Key == "Email");
        Assert.Contains("not a valid e-mail address", validationResult["Email"][0]);
    }

    [Fact]
    public async Task ValidateAsync_WithPolymorphicType_ValidatesDerivedTypes()
    {
        // Arrange
        var person = new PersonWithPolymorphicProperty
        {
            Name = "Valid Name",
            Contact = new BusinessContact // Invalid business contact with missing company name
            {
                Email = "business@example.com",
                Phone = "555-1234",
                CompanyName = "" // Invalid - required
            }
        };

        var validationResult = await ValidateInstanceAsync(person);

        // Assert
        Assert.Single(validationResult);
        Assert.Contains(validationResult, e => e.Key == "Contact.CompanyName");
        Assert.Contains("required", validationResult["Contact.CompanyName"][0]);
    }

    [Fact]
    public async Task ValidateAsync_WithValidInput_HasNoErrors()
    {
        // Arrange
        var person = new PersonWithValidation
        {
            Name = "Valid Name",
            Age = 30
        };

        var validationResult = await ValidateInstanceAsync(person);

        // Assert
        Assert.Empty(validationResult);
    }

    private async Task<Dictionary<string, string[]>> ValidateInstanceAsync<T>(T instance)
    {
        if (!_validationOptions.TryGetValidatableTypeInfo(typeof(T), out var validatableInfo))
        {
            return new Dictionary<string, string[]>();
        }

        var validateContext = new ValidateContext
        {
            ValidationContext = new System.ComponentModel.DataAnnotations.ValidationContext(instance!),
            ValidationOptions = _validationOptions
        };

        await validatableInfo.ValidateAsync(instance, validateContext, CancellationToken.None);

        return validateContext.ValidationErrors ?? new Dictionary<string, string[]>();
    }

    private void TestMethod(string parameter) { }

    private class PersonWithValidation
    {
        [Required]
        public string Name { get; set; } = "";

        [Range(0, 120)]
        public int Age { get; set; }
    }

    private class PersonWithDisplayName
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";
    }

    private class PersonWithoutValidation
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    private class PersonWithNestedValidation
    {
        [Required]
        public string Name { get; set; } = "";

        public AddressWithValidation Address { get; set; } = new();
    }

    private class PersonWithList
    {
        [Required]
        public string Name { get; set; } = "";

        public List<AddressWithValidation> Addresses { get; set; } = new();
    }

    private class PersonWithParsableProperty
    {
        [Required]
        public string Name { get; set; } = "";

        [EmailAddress]
        public string Email { get; set; } = "";
    }

    private class AddressWithValidation
    {
        [Required]
        public string Street { get; set; } = "";

        [Required]
        public string City { get; set; } = "";
    }

    private class PersonWithCyclicReference
    {
        [Required]
        public string Name { get; set; } = "";

        public PersonWithCyclicReference? Friend { get; set; }
    }

    private class PersonWithPolymorphicProperty
    {
        [Required]
        public string Name { get; set; } = "";

        public Contact Contact { get; set; } = null!;
    }
    
    [JsonDerivedType(typeof(PersonalContact), typeDiscriminator: "personal")]
    [JsonDerivedType(typeof(BusinessContact), typeDiscriminator: "business")]

    private abstract class Contact
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Phone]
        public string Phone { get; set; } = "";
    }

    private class PersonalContact : Contact
    {
        [Required]
        public string FirstName { get; set; } = "";

        [Required]
        public string LastName { get; set; } = "";
    }

    private class BusinessContact : Contact
    {
        [Required]
        public string CompanyName { get; set; } = "";
    }
}