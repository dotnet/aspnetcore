#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;

namespace Microsoft.Extensions.Validation.GeneratorTests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Fact]
    public async Task CanValidateValidationAttributesOnClasses()
    {
        var source = """
#pragma warning disable ASP0029

using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.Run();

[ValidatableType]
[SumLimit]
public class ComplexType : IPoint
{
    [Range(0, 15)]
    public int X { get; set; } = 10;

    [Range(0, 15)]
    public int Y { get; set; } = 10;

    public NestedType ObjectProperty { get; set; } = new NestedType();

    public BasePolymorphicType PolymorphicProperty { get; set; } = new DerivedPolymorphicType();
}

// This class does not have any property-level validation attributes, but it has a class-level validation attribute.
// Therefore, its type info should still be emitted in the generator output.
[SumLimit]
public class NestedType : IPoint
{
    public int X { get; set; } = 10;

    public int Y { get; set; } = 10;
}

[ValidatableType]
[SumLimit]
public class BasePolymorphicType : IPoint
{
    public int X { get; set; } = 5;

    public int Y { get; set; } = 5;
}

[ValidatableType]
[ProductLimit]
public class DerivedPolymorphicType : BasePolymorphicType
{
    public int Z { get; set; } = 2;
}

public interface IPoint
{
    int X { get; }
    int Y { get; }
}

public class SumLimitAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is IPoint point)
        {
            if (point.X + point.Y > 20)
            {
                return new ValidationResult($"Sum is too high");
            }
        }
        return ValidationResult.Success;
    }
}

public class ProductLimitAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DerivedPolymorphicType derived)
        {
            if (derived.X * derived.Y * derived.Z > 100)
            {
                return new ValidationResult($"Product is too high");
            }
        }
        return ValidationResult.Success;
    }
}
""";
        await Verify(source, out var compilation);
        await VerifyValidatableType(compilation, "ComplexType", async (validationOptions, type) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(type, out var validatableTypeInfo));

            await InvalidPropertyAttributeCheck_ProducesError_AndShortCircuits(validatableTypeInfo);
            await ValidClassAttributeCheck_DoesNotProduceError(validatableTypeInfo);
            await InvalidClassAttributeCheck_ProducesError(validatableTypeInfo);
            await InvalidNestedClassAttributeCheck_ProducesError_AndShortCircuits(validatableTypeInfo);
            await ValidPolymorphicClassAttributeCheck_DoesNotProduceError(validatableTypeInfo);
            await InvalidPolymorphicBaseClassAttributeCheck_ProducesError(validatableTypeInfo);
            await InvalidPolymorphicDerivedClassAttributeCheck_ProducesError(validatableTypeInfo);
            await InvalidPolymorphicBothClassAttributesCheck_ProducesError(validatableTypeInfo);

            async Task InvalidPropertyAttributeCheck_ProducesError_AndShortCircuits(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                type.GetProperty("X")?.SetValue(instance, 16);
                type.GetProperty("Y")?.SetValue(instance, 0);

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.NotNull(context.ValidationErrors);
                var propertyAttributeError = Assert.Single(context.ValidationErrors);
                Assert.Equal("X", propertyAttributeError.Key);
                Assert.Equal("The field X must be between 0 and 15.", propertyAttributeError.Value.Single());
            }

            async Task ValidClassAttributeCheck_DoesNotProduceError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Null(context.ValidationErrors);
            }

            async Task InvalidClassAttributeCheck_ProducesError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                type.GetProperty("X")?.SetValue(instance, 11);
                type.GetProperty("Y")?.SetValue(instance, 12);

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.NotNull(context.ValidationErrors);
                var classAttributeError = Assert.Single(context.ValidationErrors);
                Assert.Equal(string.Empty, classAttributeError.Key);
                Assert.Equal("Sum is too high", classAttributeError.Value.Single());
            }

            async Task InvalidNestedClassAttributeCheck_ProducesError_AndShortCircuits(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                var objectPropertyInstance = type.GetProperty("ObjectProperty").GetValue(instance);
                objectPropertyInstance.GetType().GetProperty("X")?.SetValue(objectPropertyInstance, 11);
                objectPropertyInstance.GetType().GetProperty("Y")?.SetValue(objectPropertyInstance, 12);

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.NotNull(context.ValidationErrors);
                var classAttributeError = Assert.Single(context.ValidationErrors);
                Assert.Equal("ObjectProperty", classAttributeError.Key);
                Assert.Equal("Sum is too high", classAttributeError.Value.Single());
            }

            async Task ValidPolymorphicClassAttributeCheck_DoesNotProduceError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                var polymorphicPropertyInstance = type.GetProperty("PolymorphicProperty").GetValue(instance);
                // Set valid values that satisfy both base and derived validation
                polymorphicPropertyInstance.GetType().GetProperty("X")?.SetValue(polymorphicPropertyInstance, 3);
                polymorphicPropertyInstance.GetType().GetProperty("Y")?.SetValue(polymorphicPropertyInstance, 3);
                polymorphicPropertyInstance.GetType().GetProperty("Z")?.SetValue(polymorphicPropertyInstance, 2);

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Null(context.ValidationErrors);
            }

            async Task InvalidPolymorphicBaseClassAttributeCheck_ProducesError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                var polymorphicPropertyInstance = type.GetProperty("PolymorphicProperty").GetValue(instance);
                // Set values that violate base class SumLimit validation (X + Y > 20) but not ProductLimit (X * Y * Z <= 100)
                polymorphicPropertyInstance.GetType().GetProperty("X")?.SetValue(polymorphicPropertyInstance, 15);
                polymorphicPropertyInstance.GetType().GetProperty("Y")?.SetValue(polymorphicPropertyInstance, 6);
                polymorphicPropertyInstance.GetType().GetProperty("Z")?.SetValue(polymorphicPropertyInstance, 1); // Sum: 21 > 20, Product: 90 <= 100

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.NotNull(context.ValidationErrors);
                var classAttributeError = Assert.Single(context.ValidationErrors);
                Assert.Equal("PolymorphicProperty", classAttributeError.Key);
                Assert.Equal("Sum is too high", classAttributeError.Value.Single());
            }

            async Task InvalidPolymorphicDerivedClassAttributeCheck_ProducesError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                var polymorphicPropertyInstance = type.GetProperty("PolymorphicProperty").GetValue(instance);
                // Set values that violate derived class ProductLimit validation (X * Y * Z > 100) but not SumLimit (X + Y <= 20)
                polymorphicPropertyInstance.GetType().GetProperty("X")?.SetValue(polymorphicPropertyInstance, 5);
                polymorphicPropertyInstance.GetType().GetProperty("Y")?.SetValue(polymorphicPropertyInstance, 5);
                polymorphicPropertyInstance.GetType().GetProperty("Z")?.SetValue(polymorphicPropertyInstance, 5); // Sum: 10 <= 20, Product: 125 > 100

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.NotNull(context.ValidationErrors);
                var classAttributeError = Assert.Single(context.ValidationErrors);
                Assert.Equal("PolymorphicProperty", classAttributeError.Key);
                Assert.Equal("Product is too high", classAttributeError.Value.Single());
            }

            async Task InvalidPolymorphicBothClassAttributesCheck_ProducesError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                var polymorphicPropertyInstance = type.GetProperty("PolymorphicProperty").GetValue(instance);
                // Set values that violate both base and derived class validations
                polymorphicPropertyInstance.GetType().GetProperty("X")?.SetValue(polymorphicPropertyInstance, 11);
                polymorphicPropertyInstance.GetType().GetProperty("Y")?.SetValue(polymorphicPropertyInstance, 12);
                polymorphicPropertyInstance.GetType().GetProperty("Z")?.SetValue(polymorphicPropertyInstance, 5); // Sum: 23 > 20, Product: 660 > 100

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.NotNull(context.ValidationErrors);
                Assert.Collection(context.ValidationErrors, kvp =>
                {
                    Assert.Equal("PolymorphicProperty", kvp.Key);
                    Assert.Collection(kvp.Value,
                        error =>
                        {
                            Assert.Equal("Product is too high", error);
                        },
                        error =>
                        {
                            Assert.Equal("Sum is too high", error);
                        });
                });
            }
        });
    }
}
