// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.Validation;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator.Tests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Fact]
    public async Task CanValidateTypesWithAttribute()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.Run();

[ValidatableType]
public class ComplexType
{
    [Range(10, 100)]
    public int IntegerWithRange { get; set; } = 10;

    [Range(10, 100), Display(Name = "Valid identifier")]
    public int IntegerWithRangeAndDisplayName { get; set; } = 50;

    [Required]
    public SubType PropertyWithMemberAttributes { get; set; } = new SubType();

    public SubType PropertyWithoutMemberAttributes { get; set; } = new SubType();

    public SubTypeWithInheritance PropertyWithInheritance { get; set; } = new SubTypeWithInheritance();

    public List<SubType> ListOfSubTypes { get; set; } = [];

    [CustomValidation(ErrorMessage = "Value must be an even number")]
    public int IntegerWithCustomValidationAttribute { get; set; }

    [CustomValidation, Range(10, 100)]
    public int PropertyWithMultipleAttributes { get; set; } = 10;
}

public class CustomValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is int number && number % 2 == 0;
}

public class SubType
{
    [Required]
    public string RequiredProperty { get; set; } = "some-value";

    [StringLength(10)]
    public string? StringWithLength { get; set; }
}

public class SubTypeWithInheritance : SubType
{
    [EmailAddress]
    public string? EmailString { get; set; }
}
""";
        await Verify(source, out var compilation);
        VerifyValidatableType(compilation, "ComplexType", async (validationOptions, type) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(type, out var validatableTypeInfo));

            await InvalidIntegerWithRangeProducesError(validatableTypeInfo);
            await InvalidIntegerWithRangeAndDisplayNameProducesError(validatableTypeInfo);
            await MissingRequiredSubtypePropertyProducesError(validatableTypeInfo);
            await InvalidRequiredSubtypePropertyProducesError(validatableTypeInfo);
            await InvalidSubTypeWithInheritancePropertyProducesError(validatableTypeInfo);
            await InvalidListOfSubTypesProducesError(validatableTypeInfo);
            await InvalidPropertyWithDerivedValidationAttributeProducesError(validatableTypeInfo);
            await InvalidPropertyWithMultipleAttributesProducesError(validatableTypeInfo);
            await InvalidPropertyWithCustomValidationProducesError(validatableTypeInfo);
            await ValidInputProducesNoWarnings(validatableTypeInfo);

            async Task InvalidIntegerWithRangeProducesError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                type.GetProperty("IntegerWithRange")?.SetValue(instance, 5);
                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Collection(context.ValidationErrors, kvp =>
                {
                    Assert.Equal("IntegerWithRange", kvp.Key);
                    Assert.Equal("The field IntegerWithRange must be between 10 and 100.", kvp.Value.Single());
                });
            }

            async Task InvalidIntegerWithRangeAndDisplayNameProducesError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                type.GetProperty("IntegerWithRangeAndDisplayName")?.SetValue(instance, 5);
                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Collection(context.ValidationErrors, kvp =>
                {
                    Assert.Equal("IntegerWithRangeAndDisplayName", kvp.Key);
                    Assert.Equal("The field Valid identifier must be between 10 and 100.", kvp.Value.Single());
                });
            }

            async Task MissingRequiredSubtypePropertyProducesError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                type.GetProperty("PropertyWithMemberAttributes")?.SetValue(instance, null);
                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Collection(context.ValidationErrors, kvp =>
                {
                    Assert.Equal("PropertyWithMemberAttributes", kvp.Key);
                    Assert.Equal("The PropertyWithMemberAttributes field is required.", kvp.Value.Single());
                });
            }

            async Task InvalidRequiredSubtypePropertyProducesError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                var subType = Activator.CreateInstance(type.Assembly.GetType("SubType")!);
                subType.GetType().GetProperty("RequiredProperty")?.SetValue(subType, "");
                subType.GetType().GetProperty("StringWithLength")?.SetValue(subType, "way-too-long");
                type.GetProperty("PropertyWithMemberAttributes")?.SetValue(instance, subType);
                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Collection(context.ValidationErrors,
                    kvp =>
                    {
                        Assert.Equal("PropertyWithMemberAttributes.RequiredProperty", kvp.Key);
                        Assert.Equal("The RequiredProperty field is required.", kvp.Value.Single());
                    },
                    kvp =>
                    {
                        Assert.Equal("PropertyWithMemberAttributes.StringWithLength", kvp.Key);
                        Assert.Equal("The field StringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
                    });
            }

            async Task InvalidSubTypeWithInheritancePropertyProducesError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                var inheritanceType = Activator.CreateInstance(type.Assembly.GetType("SubTypeWithInheritance")!);
                inheritanceType.GetType().GetProperty("RequiredProperty")?.SetValue(inheritanceType, "");
                inheritanceType.GetType().GetProperty("StringWithLength")?.SetValue(inheritanceType, "way-too-long");
                inheritanceType.GetType().GetProperty("EmailString")?.SetValue(inheritanceType, "not-an-email");
                type.GetProperty("PropertyWithInheritance")?.SetValue(instance, inheritanceType);
                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Collection(context.ValidationErrors,
                    kvp =>
                    {
                        Assert.Equal("PropertyWithInheritance.EmailString", kvp.Key);
                        Assert.Equal("The EmailString field is not a valid e-mail address.", kvp.Value.Single());
                    },
                    kvp =>
                    {
                        Assert.Equal("PropertyWithInheritance.RequiredProperty", kvp.Key);
                        Assert.Equal("The RequiredProperty field is required.", kvp.Value.Single());
                    },
                    kvp =>
                    {
                        Assert.Equal("PropertyWithInheritance.StringWithLength", kvp.Key);
                        Assert.Equal("The field StringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
                    });
            }

            async Task InvalidListOfSubTypesProducesError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                var subTypeList = Activator.CreateInstance(typeof(List<>).MakeGenericType(type.Assembly.GetType("SubType")!));

                // Create first invalid item
                var subType1 = Activator.CreateInstance(type.Assembly.GetType("SubType")!);
                subType1.GetType().GetProperty("RequiredProperty")?.SetValue(subType1, "");
                subType1.GetType().GetProperty("StringWithLength")?.SetValue(subType1, "way-too-long");

                // Create second invalid item
                var subType2 = Activator.CreateInstance(type.Assembly.GetType("SubType")!);
                subType2.GetType().GetProperty("RequiredProperty")?.SetValue(subType2, "valid");
                subType2.GetType().GetProperty("StringWithLength")?.SetValue(subType2, "way-too-long");

                // Create valid item
                var subType3 = Activator.CreateInstance(type.Assembly.GetType("SubType")!);
                subType3.GetType().GetProperty("RequiredProperty")?.SetValue(subType3, "valid");
                subType3.GetType().GetProperty("StringWithLength")?.SetValue(subType3, "valid");

                // Add to list
                subTypeList.GetType().GetMethod("Add")?.Invoke(subTypeList, [subType1]);
                subTypeList.GetType().GetMethod("Add")?.Invoke(subTypeList, [subType2]);
                subTypeList.GetType().GetMethod("Add")?.Invoke(subTypeList, [subType3]);

                type.GetProperty("ListOfSubTypes")?.SetValue(instance, subTypeList);
                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Collection(context.ValidationErrors,
                    kvp =>
                    {
                        Assert.Equal("ListOfSubTypes[0].RequiredProperty", kvp.Key);
                        Assert.Equal("The RequiredProperty field is required.", kvp.Value.Single());
                    },
                    kvp =>
                    {
                        Assert.Equal("ListOfSubTypes[0].StringWithLength", kvp.Key);
                        Assert.Equal("The field StringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
                    },
                    kvp =>
                    {
                        Assert.Equal("ListOfSubTypes[1].StringWithLength", kvp.Key);
                        Assert.Equal("The field StringWithLength must be a string with a maximum length of 10.", kvp.Value.Single());
                    });
            }

            async Task InvalidPropertyWithDerivedValidationAttributeProducesError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                type.GetProperty("IntegerWithCustomValidationAttribute")?.SetValue(instance, 5); // Odd number, should fail
                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Collection(context.ValidationErrors, kvp =>
                {
                    Assert.Equal("IntegerWithCustomValidationAttribute", kvp.Key);
                    Assert.Equal("Value must be an even number", kvp.Value.Single());
                });
            }

            async Task InvalidPropertyWithMultipleAttributesProducesError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                type.GetProperty("PropertyWithMultipleAttributes")?.SetValue(instance, 5);
                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Collection(context.ValidationErrors, kvp =>
                {
                    Assert.Equal("PropertyWithMultipleAttributes", kvp.Key);
                    Assert.Collection(kvp.Value,
                        error =>
                        {
                            Assert.Equal("The field PropertyWithMultipleAttributes is invalid.", error);
                        },
                        error =>
                        {
                            Assert.Equal("The field PropertyWithMultipleAttributes must be between 10 and 100.", error);
                        });
                });
            }

            async Task InvalidPropertyWithCustomValidationProducesError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                type.GetProperty("IntegerWithCustomValidationAttribute")?.SetValue(instance, 3); // Odd number should fail
                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Collection(context.ValidationErrors, kvp =>
                {
                    Assert.Equal("IntegerWithCustomValidationAttribute", kvp.Key);
                    Assert.Equal("Value must be an even number", kvp.Value.Single());
                });
            }

            async Task ValidInputProducesNoWarnings(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);

                // Set all properties with valid values
                type.GetProperty("IntegerWithRange")?.SetValue(instance, 50);
                type.GetProperty("IntegerWithRangeAndDisplayName")?.SetValue(instance, 50);

                // Create and set PropertyWithMemberAttributes
                var subType1 = Activator.CreateInstance(type.Assembly.GetType("SubType")!);
                subType1.GetType().GetProperty("RequiredProperty")?.SetValue(subType1, "valid");
                subType1.GetType().GetProperty("StringWithLength")?.SetValue(subType1, "valid");
                type.GetProperty("PropertyWithMemberAttributes")?.SetValue(instance, subType1);

                // Create and set PropertyWithoutMemberAttributes
                var subType2 = Activator.CreateInstance(type.Assembly.GetType("SubType")!);
                subType2.GetType().GetProperty("RequiredProperty")?.SetValue(subType2, "valid");
                subType2.GetType().GetProperty("StringWithLength")?.SetValue(subType2, "valid");
                type.GetProperty("PropertyWithoutMemberAttributes")?.SetValue(instance, subType2);

                // Create and set PropertyWithInheritance
                var inheritanceType = Activator.CreateInstance(type.Assembly.GetType("SubTypeWithInheritance")!);
                inheritanceType.GetType().GetProperty("RequiredProperty")?.SetValue(inheritanceType, "valid");
                inheritanceType.GetType().GetProperty("StringWithLength")?.SetValue(inheritanceType, "valid");
                inheritanceType.GetType().GetProperty("EmailString")?.SetValue(inheritanceType, "test@example.com");
                type.GetProperty("PropertyWithInheritance")?.SetValue(instance, inheritanceType);

                // Create empty list for ListOfSubTypes
                var emptyList = Activator.CreateInstance(typeof(List<>).MakeGenericType(type.Assembly.GetType("SubType")!));
                type.GetProperty("ListOfSubTypes")?.SetValue(instance, emptyList);

                // Set custom validation attributes
                type.GetProperty("IntegerWithCustomValidationAttribute")?.SetValue(instance, 2); // Even number should pass
                type.GetProperty("PropertyWithMultipleAttributes")?.SetValue(instance, 12);

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Null(context.ValidationErrors);
            }
        });
    }
}