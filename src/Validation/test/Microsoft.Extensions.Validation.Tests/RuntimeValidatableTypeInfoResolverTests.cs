#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.Validation.Tests;

public class RuntimeValidatableTypeInfoResolverTests
{
    private readonly RuntimeValidatableTypeInfoResolver _resolver = new();

    [Fact]
    public void TryGetValidatableParameterInfo_AlwaysReturnsFalse()
    {
        var methodInfo = typeof(RuntimeValidatableTypeInfoResolverTests).GetMethod(nameof(SampleMethod), BindingFlags.NonPublic | BindingFlags.Instance);
        var parameterInfo = methodInfo!.GetParameters()[0];

        var result = _resolver.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithPrimitiveType_ReturnsFalse()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(int), out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithString_ReturnsFalse()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(string), out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithEnum_ReturnsFalse()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(SampleEnum), out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public async Task TryGetValidatableTypeInfo_WithSimplePocoWithValidationAttributes_ReturnsTrue_AndValidatesCorrectly()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(SimplePocoWithValidation), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
        Assert.IsType<RuntimeValidatableTypeInfoResolver.RuntimeValidatableTypeInfo>(validatableInfo);

        // Test validation with invalid data
        var invalidPoco = new SimplePocoWithValidation
        {
            Name = "", // Required but empty
            Age = 150 // Out of range (0-100)
        };

        var validationOptions = new ValidationOptions();
        validationOptions.Resolvers.Add(_resolver);

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(invalidPoco)
        };

        await validatableInfo.ValidateAsync(invalidPoco, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Collection(context.ValidationErrors,
            kvp =>
            {
                Assert.Equal("Name", kvp.Key);
                Assert.Equal("The Name field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("Age", kvp.Key);
                Assert.Equal("The field Age must be between 0 and 100.", kvp.Value.First());
            });

        // Test validation with valid data
        var validPoco = new SimplePocoWithValidation
        {
            Name = "John Doe",
            Age = 25
        };

        var validContext = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(validPoco)
        };

        await validatableInfo.ValidateAsync(validPoco, validContext, default);

        Assert.Null(validContext.ValidationErrors); // No validation errors for valid data
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithSimplePocoWithoutValidation_ReturnsFalse()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(SimplePocoWithoutValidation), out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public async Task TryGetValidatableTypeInfo_WithNestedComplexType_ReturnsTrue_AndValidatesCorrectly()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PocoWithNestedType), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);

        // Test validation with invalid nested data
        var invalidPoco = new PocoWithNestedType
        {
            Name = "", // Required but empty
            NestedPoco = new SimplePocoWithValidation
            {
                Name = "", // Required but empty in nested object
                Age = -5 // Out of range (0-100) in nested object
            }
        };

        var validationOptions = new ValidationOptions();
        validationOptions.Resolvers.Add(_resolver);

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(invalidPoco)
        };

        await validatableInfo.ValidateAsync(invalidPoco, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Collection(context.ValidationErrors,
            kvp =>
            {
                Assert.Equal("Name", kvp.Key);
                Assert.Equal("The Name field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("NestedPoco.Name", kvp.Key);
                Assert.Equal("The Name field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("NestedPoco.Age", kvp.Key);
                Assert.Equal("The field Age must be between 0 and 100.", kvp.Value.First());
            });

        // Test validation with valid nested data
        var validPoco = new PocoWithNestedType
        {
            Name = "John Doe",
            NestedPoco = new SimplePocoWithValidation
            {
                Name = "Jane Smith",
                Age = 30
            }
        };

        var validContext = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(validPoco)
        };

        await validatableInfo.ValidateAsync(validPoco, validContext, default);

        Assert.Null(validContext.ValidationErrors); // No validation errors for valid data
    }

    [Fact]
    public async Task TryGetValidatableTypeInfo_WithCyclicReference_DoesNotCauseStackOverflow_AndValidatesCorrectly()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(CyclicTypeA), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);

        // Test validation with invalid data in cyclic structure
        var cyclicA = new CyclicTypeA
        {
            Name = "", // Required but empty
            TypeB = new CyclicTypeB
            {
                Value = "", // Required but empty
                TypeA = new CyclicTypeA
                {
                    Name = "Valid Name", // This one is valid
                    TypeB = null // No further nesting
                }
            }
        };

        var validationOptions = new ValidationOptions();
        validationOptions.Resolvers.Add(_resolver);

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(cyclicA)
        };

        await validatableInfo.ValidateAsync(cyclicA, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Collection(context.ValidationErrors,
            kvp =>
            {
                Assert.Equal("Name", kvp.Key);
                Assert.Equal("The Name field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("TypeB.Value", kvp.Key);
                Assert.Equal("The Value field is required.", kvp.Value.First());
            });

        // Test validation with valid cyclic data
        var validCyclicA = new CyclicTypeA
        {
            Name = "Valid A",
            TypeB = new CyclicTypeB
            {
                Value = "Valid B",
                TypeA = new CyclicTypeA
                {
                    Name = "Valid Nested A",
                    TypeB = null // Stop the cycle
                }
            }
        };

        var validContext = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(validCyclicA)
        };

        await validatableInfo.ValidateAsync(validCyclicA, validContext, default);

        Assert.Null(validContext.ValidationErrors); // No validation errors for valid data
    }

    [Fact]
    public async Task TryGetValidatableTypeInfo_WithCollectionOfComplexTypes_ReturnsTrue_AndValidatesCorrectly()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PocoWithCollection), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);

        // Test validation with invalid data in collection
        var invalidPoco = new PocoWithCollection
        {
            Name = "", // Required but empty
            Items = new List<SimplePocoWithValidation>
            {
                new SimplePocoWithValidation { Name = "Valid Item", Age = 25 }, // Valid item
                new SimplePocoWithValidation { Name = "", Age = 150 }, // Invalid: empty name and out of range age
                new SimplePocoWithValidation { Name = "Another Valid", Age = 30 } // Valid item
            }
        };

        var validationOptions = new ValidationOptions();
        validationOptions.Resolvers.Add(_resolver);

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(invalidPoco)
        };

        await validatableInfo.ValidateAsync(invalidPoco, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Collection(context.ValidationErrors,
            kvp =>
            {
                Assert.Equal("Name", kvp.Key);
                Assert.Equal("The Name field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("Items[1].Name", kvp.Key);
                Assert.Equal("The Name field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("Items[1].Age", kvp.Key);
                Assert.Equal("The field Age must be between 0 and 100.", kvp.Value.First());
            });

        // Test validation with valid collection data
        var validPoco = new PocoWithCollection
        {
            Name = "Collection Owner",
            Items = new List<SimplePocoWithValidation>
            {
                new SimplePocoWithValidation { Name = "Item 1", Age = 25 },
                new SimplePocoWithValidation { Name = "Item 2", Age = 30 }
            }
        };

        var validContext = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(validPoco)
        };

        await validatableInfo.ValidateAsync(validPoco, validContext, default);

        Assert.Null(validContext.ValidationErrors); // No validation errors for valid data
    }

    [Fact]
    public void TryGetValidatableTypeInfo_UsesCaching()
    {
        // First call
        var result1 = _resolver.TryGetValidatableTypeInfo(typeof(SimplePocoWithValidation), out var validatableInfo1);
        
        // Second call
        var result2 = _resolver.TryGetValidatableTypeInfo(typeof(SimplePocoWithValidation), out var validatableInfo2);

        Assert.True(result1);
        Assert.True(result2);
        Assert.Same(validatableInfo1, validatableInfo2); // Should be the same cached instance
    }

    [Fact]
    public async Task TryGetValidatableTypeInfo_WithReadOnlyProperty_IgnoresReadOnlyProperty_AndValidatesCorrectly()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PocoWithReadOnlyProperty), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
        
        var typeInfo = Assert.IsType<RuntimeValidatableTypeInfoResolver.RuntimeValidatableTypeInfo>(validatableInfo);

        // Test validation with invalid writable property (read-only property should be ignored)
        var invalidPoco = new PocoWithReadOnlyProperty
        {
            Name = "" // Required but empty (ReadOnlyValue should be ignored)
        };

        var validationOptions = new ValidationOptions();
        validationOptions.Resolvers.Add(_resolver);

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(invalidPoco)
        };

        await validatableInfo.ValidateAsync(invalidPoco, context, default);

        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("Name", error.Key);
        Assert.Equal("The Name field is required.", error.Value.First());
        // ReadOnlyValue should not generate validation errors even though it has [Required]

        // Test validation with valid writable property
        var validPoco = new PocoWithReadOnlyProperty
        {
            Name = "Valid Name" // ReadOnlyValue is always "ReadOnly" and should be ignored
        };

        var validContext = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(validPoco)
        };

        await validatableInfo.ValidateAsync(validPoco, validContext, default);

        Assert.Null(validContext.ValidationErrors); // No validation errors for valid data
    }

    // Helper method for parameter test
    private void SampleMethod(string parameter) { }

    // Test classes
    public enum SampleEnum
    {
        Value1,
        Value2
    }

    public class SimplePocoWithValidation
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(0, 100)]
        public int Age { get; set; }
    }

    public class SimplePocoWithoutValidation
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public class PocoWithNestedType
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public SimplePocoWithValidation NestedPoco { get; set; } = new();
    }

    public class CyclicTypeA
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public CyclicTypeB? TypeB { get; set; }
    }

    public class CyclicTypeB
    {
        [Required]
        public string Value { get; set; } = string.Empty;

        public CyclicTypeA? TypeA { get; set; }
    }

    public class PocoWithCollection
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public List<SimplePocoWithValidation> Items { get; set; } = new();
    }

    public class PocoWithReadOnlyProperty
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string ReadOnlyValue { get; } = "ReadOnly";
    }
}