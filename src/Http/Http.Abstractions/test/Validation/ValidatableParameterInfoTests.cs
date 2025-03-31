// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
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
            parameterType: typeof(string),
            name: "testParam",
            displayName: "Test Parameter",
            validationAttributes: [new RequiredAttribute()]);

        var context = CreateValidatableContext();

        // Act
        await paramInfo.ValidateAsync(null, context, default);

        // Assert
        var errors = context.ValidationErrors;
        Assert.NotNull(errors);
        var error = Assert.Single(errors);
        Assert.Equal("testParam", error.Key);
        Assert.Equal("The Test Parameter field is required.", error.Value.Single());
    }

    [Fact]
    public async Task Validate_RequiredParameter_ShortCircuitsOtherValidations()
    {
        // Arrange
        var paramInfo = CreateTestParameterInfo(
            parameterType: typeof(string),
            name: "testParam",
            displayName: "Test Parameter",
            // Most ValidationAttributes skip validation if the value is null
            // so we use a custom one that always fails to assert on the behavior here
            validationAttributes: [new RequiredAttribute(), new CustomTestValidationAttribute()]);

        var context = CreateValidatableContext();

        // Act
        await paramInfo.ValidateAsync(null, context, default);

        // Assert
        var errors = context.ValidationErrors;
        Assert.NotNull(errors);
        var error = Assert.Single(errors);
        Assert.Equal("testParam", error.Key);
        Assert.Equal("The Test Parameter field is required.", error.Value.Single());
    }

    [Fact]
    public async Task Validate_SkipsValidation_WhenNullAndNotRequired()
    {
        // Arrange
        var paramInfo = CreateTestParameterInfo(
            parameterType: typeof(string),
            name: "testParam",
            displayName: "Test Parameter",
            validationAttributes: [new StringLengthAttribute(10)]);

        var context = CreateValidatableContext();

        // Act
        await paramInfo.ValidateAsync(null, context, default);

        // Assert
        var errors = context.ValidationErrors;
        Assert.Null(errors); // No errors added
    }

    [Fact]
    public async Task Validate_WithRangeAttribute_ValidatesCorrectly()
    {
        // Arrange
        var paramInfo = CreateTestParameterInfo(
            parameterType: typeof(int),
            name: "testParam",
            displayName: "Test Parameter",
            validationAttributes: [new RangeAttribute(10, 100)]);

        var context = CreateValidatableContext();

        // Act
        await paramInfo.ValidateAsync(5, context, default);

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
            parameterType: typeof(string),
            name: "testParam",
            displayName: "Custom Display Name",
            validationAttributes: [new RequiredAttribute()]);

        var context = CreateValidatableContext();

        // Act
        await paramInfo.ValidateAsync(null, context, default);

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
                    [new RequiredAttribute()])
            ]);

        var paramInfo = CreateTestParameterInfo(
            parameterType: typeof(Person),
            name: "person",
            displayName: "Person",
            validationAttributes: []);

        var typeMapping = new Dictionary<Type, ValidatableTypeInfo>
        {
            { typeof(Person), personTypeInfo }
        };

        var context = CreateValidatableContext(typeMapping);
        var person = new Person(); // Name is null, so should fail validation

        // Act
        await paramInfo.ValidateAsync(person, context, default);

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
                    [new RequiredAttribute()])
            ]);

        var paramInfo = CreateTestParameterInfo(
            parameterType: typeof(IEnumerable<Person>),
            name: "people",
            displayName: "People",
            validationAttributes: []);

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
        await paramInfo.ValidateAsync(people, context, default);

        // Assert
        var errors = context.ValidationErrors;
        Assert.NotNull(errors);
        var error = Assert.Single(errors);
        Assert.Equal("people[1].Name", error.Key);
        Assert.Equal("The Name field is required.", error.Value[0]);
    }

    [Fact]
    public async Task Validate_MultipleErrorsOnSameParameter_CollectsAllErrors()
    {
        // Arrange
        var paramInfo = CreateTestParameterInfo(
            parameterType: typeof(int),
            name: "testParam",
            displayName: "Test Parameter",
            validationAttributes:
            [
                new RangeAttribute(10, 100) { ErrorMessage = "Range error" },
                new CustomTestValidationAttribute { ErrorMessage = "Custom error" }
            ]);

        var context = CreateValidatableContext();

        // Act
        await paramInfo.ValidateAsync(5, context, default);

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
            parameterType: typeof(int),
            name: "testParam",
            displayName: "Test Parameter",
            validationAttributes: [new RangeAttribute(10, 100)]);

        var context = CreateValidatableContext();
        context.CurrentValidationPath = "parent";

        // Act
        await paramInfo.ValidateAsync(5, context, default);

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
            parameterType: typeof(string),
            name: "testParam",
            displayName: "Test Parameter",
            validationAttributes: [new ThrowingValidationAttribute()]);

        var context = CreateValidatableContext();

        // Act
        await paramInfo.ValidateAsync("test", context, default);

        // Assert
        var errors = context.ValidationErrors;
        Assert.NotNull(errors);
        var error = Assert.Single(errors);
        Assert.Equal("testParam", error.Key);
        Assert.Equal("Test exception", error.Value.First());
    }

    private TestValidatableParameterInfo CreateTestParameterInfo(
        Type parameterType,
        string name,
        string displayName,
        ValidationAttribute[] validationAttributes)
    {
        return new TestValidatableParameterInfo(
            parameterType,
            name,
            displayName,
            validationAttributes);
    }

    private ValidateContext CreateValidatableContext(
        Dictionary<Type, ValidatableTypeInfo>? typeMapping = null)
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var validationContext = new ValidationContext(new object(), serviceProvider, null);

        return new ValidateContext
        {
            ValidationContext = validationContext,
            ValidationOptions = new TestValidationOptions(typeMapping ?? new Dictionary<Type, ValidatableTypeInfo>())
        };
    }

    private class TestValidatableParameterInfo : ValidatableParameterInfo
    {
        private readonly ValidationAttribute[] _validationAttributes;

        public TestValidatableParameterInfo(
            Type parameterType,
            string name,
            string displayName,
            ValidationAttribute[] validationAttributes)
            : base(parameterType, name, displayName)
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
            ValidationAttribute[] validationAttributes)
            : base(containingType, propertyType, name, displayName)
        {
            _validationAttributes = validationAttributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    private class TestValidatableTypeInfo(
        Type type,
        ValidatablePropertyInfo[] members) : ValidatableTypeInfo(type, members)
    {
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

            public ValidatableTypeInfo? TryGetValidatableTypeInfo(Type type)
            {
                _typeInfoMappings.TryGetValue(type, out var info);
                return info;
            }

            public ValidatableParameterInfo? GetValidatableParameterInfo(ParameterInfo parameterInfo)
            {
                // Not implemented in the test
                return null;
            }

            public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
            {
                if (_typeInfoMappings.TryGetValue(type, out var validatableTypeInfo))
                {
                    validatableInfo = validatableTypeInfo;
                    return true;
                }
                validatableInfo = null;
                return false;
            }

            public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
            {
                validatableInfo = null;
                return false;
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
