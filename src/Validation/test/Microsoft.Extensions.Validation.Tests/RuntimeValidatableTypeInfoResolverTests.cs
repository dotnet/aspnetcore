#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Microsoft.Extensions.Validation.Tests;

public class RuntimeValidatableTypeInfoResolverTests
{
    private readonly RuntimeValidatableInfoResolver _resolver = new();

    [Fact]
    public void TryGetValidatableParameterInfo_WithStringParameterNoAttributes_ReturnsFalse()
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
        Assert.IsType<RuntimeValidatableInfoResolver.RuntimeValidatableTypeInfo>(validatableInfo);

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

        var typeInfo = Assert.IsType<RuntimeValidatableInfoResolver.RuntimeValidatableTypeInfo>(validatableInfo);

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

    [Fact]
    public async Task TryGetValidatableTypeInfo_WithRecord_ReturnsTrue_AndValidatesCorrectly()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(SimpleRecordWithValidation), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);

        // Test validation with invalid record data
        var invalidRecord = new SimpleRecordWithValidation("", 150); // Empty name, out of range age

        var validationOptions = new ValidationOptions();
        validationOptions.Resolvers.Add(_resolver);

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(invalidRecord)
        };

        await validatableInfo.ValidateAsync(invalidRecord, context, default);

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

        // Test validation with valid record data
        var validRecord = new SimpleRecordWithValidation("John Doe", 25);

        var validContext = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(validRecord)
        };

        await validatableInfo.ValidateAsync(validRecord, validContext, default);

        Assert.Null(validContext.ValidationErrors);
    }

    [Fact]
    public async Task TryGetValidatableTypeInfo_WithRecordContainingComplexProperty_ReturnsTrue_AndValidatesCorrectly()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(RecordWithComplexProperty), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);

        // Test validation with invalid nested data in record
        var invalidRecord = new RecordWithComplexProperty(
            "",
            new SimplePocoWithValidation { Name = "", Age = 150 });

        var validationOptions = new ValidationOptions();
        validationOptions.Resolvers.Add(_resolver);

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(invalidRecord)
        };

        await validatableInfo.ValidateAsync(invalidRecord, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Collection(context.ValidationErrors,
            kvp =>
            {
                Assert.Equal("Name", kvp.Key);
                Assert.Equal("The Name field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("ComplexProperty.Name", kvp.Key);
                Assert.Equal("The Name field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("ComplexProperty.Age", kvp.Key);
                Assert.Equal("The field Age must be between 0 and 100.", kvp.Value.First());
            });
    }

    [Fact]
    public async Task TryGetValidatableTypeInfo_WithIValidatableObject_ReturnsTrue_AndValidatesCorrectly()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(ValidatableObject), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);

        // Test attribute validation fails first, then IValidatableObject.Validate is called
        var invalidObject = new ValidatableObject
        {
            Name = "", // Required but empty - attribute validation
            Value = 150 // Out of range - attribute validation
        };

        var validationOptions = new ValidationOptions();
        validationOptions.Resolvers.Add(_resolver);

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(invalidObject)
        };

        await validatableInfo.ValidateAsync(invalidObject, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Collection(context.ValidationErrors,
            kvp =>
            {
                Assert.Equal("Name", kvp.Key);
                Assert.Equal("The Name field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("Value", kvp.Key);
                Assert.Equal("The field Value must be between 0 and 100.", kvp.Value.First());
            });

        // Test IValidatableObject.Validate custom logic
        var customInvalidObject = new ValidatableObject
        {
            Name = "Invalid", // Triggers custom validation
            Value = 25
        };

        var customContext = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(customInvalidObject)
        };

        await validatableInfo.ValidateAsync(customInvalidObject, customContext, default);

        Assert.NotNull(customContext.ValidationErrors);
        var error = Assert.Single(customContext.ValidationErrors);
        Assert.Equal("Name", error.Key);
        Assert.Equal("Name cannot be 'Invalid'", error.Value.First());

        // Test complex IValidatableObject logic with multiple properties
        var multiPropertyInvalidObject = new ValidatableObject
        {
            Name = "Joe", // Valid but short (< 5 chars)
            Value = 75    // Valid range but > 50, triggers multi-property validation
        };

        var multiContext = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(multiPropertyInvalidObject)
        };

        await validatableInfo.ValidateAsync(multiPropertyInvalidObject, multiContext, default);

        Assert.NotNull(multiContext.ValidationErrors);
        Assert.Equal(2, multiContext.ValidationErrors.Count);
        Assert.True(multiContext.ValidationErrors.ContainsKey("Name"));
        Assert.True(multiContext.ValidationErrors.ContainsKey("Value"));
        Assert.Equal("When Value > 50, Name must be at least 5 characters", multiContext.ValidationErrors["Name"].First());
        Assert.Equal("When Value > 50, Name must be at least 5 characters", multiContext.ValidationErrors["Value"].First());
    }

    [Fact]
    public async Task TryGetValidatableTypeInfo_WithNestedIValidatableObject_ReturnsTrue_AndValidatesCorrectly()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PocoWithValidatableObject), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);

        var invalidObject = new PocoWithValidatableObject
        {
            Title = "", // Required but empty
            ValidatableProperty = new ValidatableObject
            {
                Name = "Invalid", // Triggers custom validation
                Value = 25
            }
        };

        var validationOptions = new ValidationOptions();
        validationOptions.Resolvers.Add(_resolver);

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(invalidObject)
        };

        await validatableInfo.ValidateAsync(invalidObject, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Collection(context.ValidationErrors,
            kvp =>
            {
                Assert.Equal("Title", kvp.Key);
                Assert.Equal("The Title field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("ValidatableProperty.Name", kvp.Key);
                Assert.Equal("Name cannot be 'Invalid'", kvp.Value.First());
            });
    }

    [Fact]
    public async Task TryGetValidatableTypeInfo_WithCustomValidationAttribute_ReturnsTrue_AndValidatesCorrectly()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PocoWithCustomValidation), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);

        var invalidObject = new PocoWithCustomValidation
        {
            Name = "", // Required but empty
            EvenValue = 3, // Odd number - custom validation fails
            MultipleAttributesValue = -1 // Odd number and out of range (-1 is not in range 1-100)
        };

        var validationOptions = new ValidationOptions();
        validationOptions.Resolvers.Add(_resolver);

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(invalidObject)
        };

        await validatableInfo.ValidateAsync(invalidObject, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Collection(context.ValidationErrors,
            kvp =>
            {
                Assert.Equal("Name", kvp.Key);
                Assert.Equal("The Name field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("EvenValue", kvp.Key);
                Assert.Equal("The field EvenValue must be an even number.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("MultipleAttributesValue", kvp.Key);
                Assert.Equal(2, kvp.Value.Count());
                Assert.Contains(kvp.Value, error => error == "The field MultipleAttributesValue must be between 1 and 100.");
                Assert.Contains(kvp.Value, error => error == "The field MultipleAttributesValue must be an even number.");
            });

        // Test valid custom validation
        var validObject = new PocoWithCustomValidation
        {
            Name = "Valid Name",
            EvenValue = 4,
            MultipleAttributesValue = 50
        };

        var validContext = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(validObject)
        };

        await validatableInfo.ValidateAsync(validObject, validContext, default);

        Assert.Null(validContext.ValidationErrors);
    }

    [Fact]
    public async Task TryGetValidatableTypeInfo_WithStringValidationAttributes_ReturnsTrue_AndValidatesCorrectly()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PocoWithStringValidation), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);

        var invalidObject = new PocoWithStringValidation
        {
            Name = "AB", // Too short (min 3, max 10)
            Email = "invalid-email",
            Website = "not-a-url",
            Phone = "123-456" // Invalid format
        };

        var validationOptions = new ValidationOptions();
        validationOptions.Resolvers.Add(_resolver);

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(invalidObject)
        };

        await validatableInfo.ValidateAsync(invalidObject, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Collection(context.ValidationErrors,
            kvp =>
            {
                Assert.Equal("Name", kvp.Key);
                Assert.Contains("length", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("Email", kvp.Key);
                Assert.Contains("valid e-mail", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("Website", kvp.Key);
                Assert.Contains("valid", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("Phone", kvp.Key);
                Assert.Equal("Phone must be in format XXX-XXX-XXXX", kvp.Value.First());
            });

        // Test valid string validation
        var validObject = new PocoWithStringValidation
        {
            Name = "Valid Name",
            Email = "test@example.com",
            Website = "https://example.com",
            Phone = "123-456-7890"
        };

        var validContext = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(validObject)
        };

        await validatableInfo.ValidateAsync(validObject, validContext, default);

        Assert.Null(validContext.ValidationErrors);
    }

    [Fact]
    public async Task TryGetValidatableTypeInfo_WithRangeValidationDifferentTypes_ReturnsTrue_AndValidatesCorrectly()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PocoWithRangeValidation), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);

        var invalidObject = new PocoWithRangeValidation
        {
            DecimalValue = 150.75m, // Out of range (0.1 - 100.5)
            DateValue = new DateTime(2024, 6, 1), // Out of range (2023 only)
            DateOnlyValue = new DateOnly(2024, 6, 1) // Out of range (2023 only)
        };

        var validationOptions = new ValidationOptions();
        validationOptions.Resolvers.Add(_resolver);

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(invalidObject)
        };

        await validatableInfo.ValidateAsync(invalidObject, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal(3, context.ValidationErrors.Count);
        Assert.All(context.ValidationErrors, kvp => Assert.Contains("must be between", kvp.Value.First()));

        // Test valid range validation
        var validObject = new PocoWithRangeValidation
        {
            DecimalValue = 50.25m,
            DateValue = new DateTime(2023, 6, 1),
            DateOnlyValue = new DateOnly(2023, 6, 1)
        };

        var validContext = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(validObject)
        };

        await validatableInfo.ValidateAsync(validObject, validContext, default);

        Assert.Null(validContext.ValidationErrors);
    }

    [Fact]
    public async Task TryGetValidatableTypeInfo_WithDisplayAttributes_ReturnsTrue_AndUsesDisplayNamesInErrors()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PocoWithDisplayAttributes), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);

        var invalidObject = new PocoWithDisplayAttributes
        {
            Name = "", // Required but empty
            Age = 150 // Out of range
        };

        var validationOptions = new ValidationOptions();
        validationOptions.Resolvers.Add(_resolver);

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(invalidObject)
        };

        await validatableInfo.ValidateAsync(invalidObject, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Collection(context.ValidationErrors,
            kvp =>
            {
                Assert.Equal("Name", kvp.Key);
                Assert.Equal("The Full Name field is required.", kvp.Value.First());
            },
            kvp =>
            {
                Assert.Equal("Age", kvp.Key);
                Assert.Equal("The field User Age must be between 0 and 100.", kvp.Value.First());
            });
    }

    [Fact]
    public async Task TryGetValidatableTypeInfo_WithCustomValidationMethod_ReturnsTrue_AndValidatesCorrectly()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PocoWithCustomValidationMethod), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);

        var invalidObject = new PocoWithCustomValidationMethod
        {
            FirstName = "John",
            LastName = "Doe",
            FullName = "Jane Smith" // Doesn't match FirstName + LastName
        };

        var validationOptions = new ValidationOptions();
        validationOptions.Resolvers.Add(_resolver);

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(invalidObject)
        };

        await validatableInfo.ValidateAsync(invalidObject, context, default);

        Assert.NotNull(context.ValidationErrors);
        var error = Assert.Single(context.ValidationErrors);
        Assert.Equal("FullName", error.Key);
        Assert.Equal("FullName must be 'John Doe'", error.Value.First());

        // Test valid custom validation method
        var validObject = new PocoWithCustomValidationMethod
        {
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe"
        };

        var validContext = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(validObject)
        };

        await validatableInfo.ValidateAsync(validObject, validContext, default);

        Assert.Null(validContext.ValidationErrors);
    }

    [Fact]
    public async Task TryGetValidatableTypeInfo_WithArrayValidation_ReturnsTrue_AndValidatesCorrectly()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PocoWithArrayValidation), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);

        var invalidObject = new PocoWithArrayValidation
        {
            Name = "", // Required but empty
            Items = new[]
            {
                new SimplePocoWithValidation { Name = "Valid", Age = 25 },
                new SimplePocoWithValidation { Name = "", Age = 150 }, // Invalid item
                new SimplePocoWithValidation { Name = "Another Valid", Age = 30 }
            }
        };

        var validationOptions = new ValidationOptions();
        validationOptions.Resolvers.Add(_resolver);

        var context = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(invalidObject)
        };

        await validatableInfo.ValidateAsync(invalidObject, context, default);

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

        // Test valid array validation
        var validObject = new PocoWithArrayValidation
        {
            Name = "Valid Name",
            Items = new[]
            {
                new SimplePocoWithValidation { Name = "Item 1", Age = 25 },
                new SimplePocoWithValidation { Name = "Item 2", Age = 30 }
            }
        };

        var validContext = new ValidateContext
        {
            ValidationOptions = validationOptions,
            ValidationContext = new ValidationContext(validObject)
        };

        await validatableInfo.ValidateAsync(validObject, validContext, default);

        Assert.Null(validContext.ValidationErrors);
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

    // Test record types
    public record SimpleRecordWithValidation(
        [Required] string Name,
        [Range(0, 100)] int Age);

    public record RecordWithComplexProperty(
        [Required] string Name,
        SimplePocoWithValidation ComplexProperty);

    // Test IValidatableObject implementations
    public class ValidatableObject : IValidatableObject
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(0, 100)]
        public int Value { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Name == "Invalid")
            {
                yield return new ValidationResult("Name cannot be 'Invalid'", new[] { nameof(Name) });
            }

            if (Value > 50 && Name?.Length < 5)
            {
                yield return new ValidationResult("When Value > 50, Name must be at least 5 characters", new[] { nameof(Name), nameof(Value) });
            }
        }
    }

    public class PocoWithValidatableObject
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        public ValidatableObject ValidatableProperty { get; set; } = new();
    }

    // Test custom validation attributes
    public class EvenNumberAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is int number)
            {
                return number % 2 == 0;
            }
            return true;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The field {name} must be an even number.";
        }
    }

    public class PocoWithCustomValidation
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [EvenNumber]
        public int EvenValue { get; set; }

        [Range(1, 100), EvenNumber]
        public int MultipleAttributesValue { get; set; }
    }

    // Test string-specific validation attributes
    public class PocoWithStringValidation
    {
        [Required]
        [StringLength(10, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [Url]
        public string? Website { get; set; }

        [RegularExpression(@"^\d{3}-\d{3}-\d{4}$", ErrorMessage = "Phone must be in format XXX-XXX-XXXX")]
        public string? Phone { get; set; }
    }

    // Test range validation with different data types
    public class PocoWithRangeValidation
    {
        [Range(0.1, 100.5)]
        public decimal DecimalValue { get; set; }

        [Range(typeof(DateTime), "2023-01-01", "2023-12-31")]
        public DateTime DateValue { get; set; }

        [Range(typeof(DateOnly), "2023-01-01", "2023-12-31")]
        public DateOnly DateOnlyValue { get; set; }
    }

    // Test Display attribute handling
    public class PocoWithDisplayAttributes
    {
        [Required]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Range(0, 100)]
        [Display(Name = "User Age", Description = "Age in years")]
        public int Age { get; set; }
    }

    // Test CustomValidation attribute
    public class PocoWithCustomValidationMethod
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [CustomValidation(typeof(PocoWithCustomValidationMethod), nameof(ValidateFullName))]
        public string FullName { get; set; } = string.Empty;

        public static ValidationResult? ValidateFullName(string fullName, ValidationContext context)
        {
            if (context.ObjectInstance is PocoWithCustomValidationMethod instance)
            {
                var expectedFullName = $"{instance.FirstName} {instance.LastName}";
                if (fullName != expectedFullName)
                {
                    return new ValidationResult($"FullName must be '{expectedFullName}'", new[] { context.MemberName! });
                }
            }
            return ValidationResult.Success;
        }
    }

    // Test array validation
    public class PocoWithArrayValidation
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public SimplePocoWithValidation[]? Items { get; set; }
    }

    // Test service-like type that should be excluded from validation
    public class TestService
    {
        [Range(10, 100)]
        public int Value { get; set; } = 4;

        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
