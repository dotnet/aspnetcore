// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.Validation.Tests;

public class ValidatableParameterInfoTests
{
    [Fact]
    public async Task Validate_RequiredParameter_AddsErrorWhenNull()
    {
        // Arrange
        var paramInfo = CreateTestParameterInfo(
            name: "testParam",
            displayName: "Test Parameter",
            isNullable: true,
            isRequired: true,
            hasValidatableType: false,
            isEnumerable: false,
            validationAttributes: [new RequiredAttribute()]);

        var context = CreateValidatableContext();

        // Act
        await paramInfo.Validate(null, context);

        // Assert
        var errors = context.ValidationErrors;
        Assert.NotNull(errors);
        var error = Assert.Single(errors);
        Assert.Equal("testParam", error.Key);
        Assert.Equal("The Test Parameter field is required.", error.Value.First());
    }

    [Fact]
    public async Task Validate_SkipsValidation_WhenNullAndNotRequired()
    {
        // Arrange
        var paramInfo = CreateTestParameterInfo(
            name: "testParam",
            displayName: "Test Parameter",
            isNullable: true,
            isRequired: false,
            hasValidatableType: false,
            isEnumerable: false,
            validationAttributes: [new StringLengthAttribute(10)]);

        var context = CreateValidatableContext();

        // Act
        await paramInfo.Validate(null, context);

        // Assert
        var errors = context.ValidationErrors;
        Assert.Null(errors); // No errors added
    }

    [Fact]
    public async Task Validate_WithRangeAttribute_ValidatesCorrectly()
    {
        // Arrange
        var paramInfo = CreateTestParameterInfo(
            name: "testParam",
            displayName: "Test Parameter",
            isNullable: false,
            isRequired: false,
            hasValidatableType: false,
            isEnumerable: false,
            validationAttributes: [new RangeAttribute(10, 100)]);

        var context = CreateValidatableContext();

        // Act
        await paramInfo.Validate(5, context);

        // Assert
        var errors = context.ValidationErrors;
        Assert.NotNull(errors);
        var error = Assert.Single(errors);
        Assert.Equal("testParam", error.Key);
        Assert.Equal("The field Test Parameter must be between 10 and 100.", error.Value.First());
    }

    [Fact]
    public async Task Validate_WithDisplayNameAttribute_UsesDisplayNameInErrorMessage()
    {
        // Arrange
        var paramInfo = CreateTestParameterInfo(
            name: "testParam",
            displayName: "Custom Display Name",
            isNullable: false,
            isRequired: true,
            hasValidatableType: false,
            isEnumerable: false,
            validationAttributes: [new RequiredAttribute()]);

        var context = CreateValidatableContext();

        // Act
        await paramInfo.Validate(null, context);

        // Assert
        var errors = context.ValidationErrors;
        Assert.NotNull(errors);
        var error = Assert.Single(errors);
        Assert.Equal("testParam", error.Key);
        // The error message should use the display name
        Assert.Equal("The Custom Display Name field is required.", error.Value.First());
    }

    [Fact]
    public async Task Validate_WhenValidatableTypeHasErrors_AddsNestedErrors()
    {
        // Arrange
        var personTypeInfo = new TestValidatableTypeInfo(
            typeof(Person),
            [
                new TestValidatablePropertyInfo(
                    typeof(Person),
                    typeof(string),
                    "Name",
                    "Name",
                    false,
                    false,
                    true,
                    false,
                    [new RequiredAttribute()])
            ],
            false);

        var paramInfo = CreateTestParameterInfo(
            name: "person",
            displayName: "Person",
            isNullable: false,
            isRequired: false,
            hasValidatableType: true,
            isEnumerable: false,
            validationAttributes: Array.Empty<ValidationAttribute>());

        var typeMapping = new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(Person), personTypeInfo }
        };

        var context = CreateValidatableContext(typeMapping);
        var person = new Person(); // Name is null, so should fail validation

        // Act
        await paramInfo.Validate(person, context);

        // Assert
        var errors = context.ValidationErrors;
        Assert.NotNull(errors);
        var error = Assert.Single(errors);
        Assert.Equal("Name", error.Key);
        Assert.Equal("The Name field is required.", error.Value[0]);
    }

    [Fact]
    public async Task Validate_WithEnumerableOfValidatableType_ValidatesEachItem()
    {
        // Arrange
        var personTypeInfo = new TestValidatableTypeInfo(
            typeof(Person),
            [
                new TestValidatablePropertyInfo(
                    typeof(Person),
                    typeof(string),
                    "Name",
                    "Name",
                    false,
                    false,
                    true,
                    false,
                    [new RequiredAttribute()])
            ],
            false);

        var paramInfo = CreateTestParameterInfo(
            name: "people",
            displayName: "People",
            isNullable: false,
            isRequired: false,
            hasValidatableType: true,
            isEnumerable: true,
            validationAttributes: Array.Empty<ValidationAttribute>());

        var typeMapping = new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(Person), personTypeInfo }
        };

        var context = CreateValidatableContext(typeMapping);
        var people = new List<Person>
        {
            new() { Name = "Valid" },
            new() // Name is null, should fail
        };

        // Act
        await paramInfo.Validate(people, context);

        // Assert
        var errors = context.ValidationErrors;
        Assert.NotNull(errors);
        var error = Assert.Single(errors);
        Assert.Equal("Name", error.Key);
        Assert.Equal("The Name field is required.", error.Value[0]);
    }

    [Fact]
    public async Task Validate_MultipleErrorsOnSameParameter_CollectsAllErrors()
    {
        // Arrange
        var paramInfo = CreateTestParameterInfo(
            name: "testParam",
            displayName: "Test Parameter",
            isNullable: false,
            isRequired: false,
            hasValidatableType: false,
            isEnumerable: false,
            validationAttributes:
            [
                new RangeAttribute(10, 100) { ErrorMessage = "Range error" },
                new CustomTestValidationAttribute { ErrorMessage = "Custom error" }
            ]);

        var context = CreateValidatableContext();

        // Act
        await paramInfo.Validate(5, context);

        // Assert
        var errors = context.ValidationErrors;
        Assert.NotNull(errors);
        var error = Assert.Single(errors);
        Assert.Equal("testParam", error.Key);
        Assert.Collection(error.Value,
            e => Assert.Equal("Range error", e),
            e => Assert.Equal("Custom error", e));
    }

    [Fact]
    public async Task Validate_WithContextPrefix_AddsErrorsWithCorrectPrefix()
    {
        // Arrange
        var paramInfo = CreateTestParameterInfo(
            name: "testParam",
            displayName: "Test Parameter",
            isNullable: false,
            isRequired: false,
            hasValidatableType: false,
            isEnumerable: false,
            validationAttributes: [new RangeAttribute(10, 100)]);

        var context = CreateValidatableContext();
        context.Prefix = "parent";

        // Act
        await paramInfo.Validate(5, context);

        // Assert
        var errors = context.ValidationErrors;
        Assert.NotNull(errors);
        var error = Assert.Single(errors);
        Assert.Equal("parent.testParam", error.Key);
        Assert.Equal("The field Test Parameter must be between 10 and 100.", error.Value.First());
    }

    [Fact]
    public async Task Validate_ExceptionDuringValidation_CapturesExceptionAsError()
    {
        // Arrange
        var paramInfo = CreateTestParameterInfo(
            name: "testParam",
            displayName: "Test Parameter",
            isNullable: false,
            isRequired: false,
            hasValidatableType: false,
            isEnumerable: false,
            validationAttributes: [new ThrowingValidationAttribute()]);

        var context = CreateValidatableContext();

        // Act
        await paramInfo.Validate("test", context);

        // Assert
        var errors = context.ValidationErrors;
        Assert.NotNull(errors);
        var error = Assert.Single(errors);
        Assert.Equal("testParam", error.Key);
        Assert.Equal("Test exception", error.Value.First());
    }

    private TestValidatableParameterInfo CreateTestParameterInfo(
        string name,
        string displayName,
        bool isNullable,
        bool isRequired,
        bool hasValidatableType,
        bool isEnumerable,
        ValidationAttribute[] validationAttributes)
    {
        return new TestValidatableParameterInfo(
            name,
            displayName,
            isNullable,
            isRequired,
            hasValidatableType,
            isEnumerable,
            validationAttributes);
    }

    private ValidatableContext CreateValidatableContext(
        Dictionary<Type, ValidatableTypeInfo>? typeMapping = null)
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var validationContext = new ValidationContext(new object(), serviceProvider, null);

        return new ValidatableContext
        {
            ValidationContext = validationContext,
            ValidationOptions = new TestValidationOptions(typeMapping ?? new Dictionary<Type, ValidatableTypeInfo>())
        };
    }

    private class TestValidatableParameterInfo : ValidatableParameterInfo
    {
        private readonly ValidationAttribute[] _validationAttributes;

        public TestValidatableParameterInfo(
            string name,
            string displayName,
            bool isNullable,
            bool isRequired,
            bool hasValidatableType,
            bool isEnumerable,
            ValidationAttribute[] validationAttributes)
            : base(name, displayName, isNullable, isRequired, hasValidatableType, isEnumerable)
        {
            _validationAttributes = validationAttributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    private class TestValidatablePropertyInfo : ValidatablePropertyInfo
    {
        private readonly ValidationAttribute[] _validationAttributes;

        public TestValidatablePropertyInfo(
            Type containingType,
            Type propertyType,
            string name,
            string displayName,
            bool isEnumerable,
            bool isNullable,
            bool isRequired,
            bool hasValidatableType,
            ValidationAttribute[] validationAttributes)
            : base(containingType, propertyType, name, displayName, isEnumerable, isNullable, isRequired, hasValidatableType)
        {
            _validationAttributes = validationAttributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    private class TestValidatableTypeInfo : ValidatableTypeInfo
    {
        public TestValidatableTypeInfo(
            Type type,
            ValidatablePropertyInfo[] members,
            bool implementsIValidatableObject,
            Type[]? validatableSubTypes = null)
            : base(type, members, implementsIValidatableObject, validatableSubTypes)
        {
        }
    }

    private class TestValidationOptions : ValidationOptions
    {
        public TestValidationOptions(Dictionary<Type, ValidatableTypeInfo> typeInfoMappings)
        {
            // Create a custom resolver that uses the dictionary
            var resolver = new DictionaryBasedResolver(typeInfoMappings);

            // Add it to the resolvers collection
            Resolvers.Add(resolver);
        }

        // Private resolver implementation that uses a dictionary lookup
        private class DictionaryBasedResolver : IValidatableInfoResolver
        {
            private readonly Dictionary<Type, ValidatableTypeInfo> _typeInfoMappings;

            public DictionaryBasedResolver(Dictionary<Type, ValidatableTypeInfo> typeInfoMappings)
            {
                _typeInfoMappings = typeInfoMappings;
            }

            public ValidatableTypeInfo? GetValidatableTypeInfo(Type type)
            {
                _typeInfoMappings.TryGetValue(type, out var info);
                return info;
            }

            public ValidatableParameterInfo? GetValidatableParameterInfo(ParameterInfo parameterInfo)
            {
                // Not implemented in the test
                return null;
            }
        }
    }

    // Test data classes and validation attributes

    private class Person
    {
        public string? Name { get; set; }
    }

    private class CustomTestValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            // Always fail for testing
            return false;
        }
    }

    private class ThrowingValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            throw new InvalidOperationException("Test exception");
        }
    }
}
