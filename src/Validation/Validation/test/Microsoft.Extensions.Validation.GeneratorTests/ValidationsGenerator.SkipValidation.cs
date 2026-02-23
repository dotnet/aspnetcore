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
    public async Task DoesNotEmit_ForSkipValidationAttribute_OnClassProperties()
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
public class ComplexType
{
    [SkipValidation]
    [Range(10, 100)]
    public int IntegerWithRange { get; set; } = 10;

    [SkipValidation]
    public NestedType SkippedObjectProperty { get; set; } = new NestedType();

    public NestedType ObjectProperty { get; set; } = new NestedType();

    [SkipValidation]
    public List<NestedType> SkippedListOfNestedTypes { get; set; } = [];

    public List<NestedType> ListOfNestedTypes { get; set; } = [];

    [SkipValidation]
    public NonSkippedBaseType SkippedBaseTypeProperty { get; set; } = new NonSkippedBaseType();

    public NonSkippedSubType NonSkippedSubTypeProperty { get; set; } = new NonSkippedSubType();

    public AlwaysSkippedType AlwaysSkippedProperty { get; set; } = new AlwaysSkippedType();

    public SubTypeOfSkippedBase SubTypeOfSkippedBaseProperty { get; set; } = new SubTypeOfSkippedBase();
}

public class NestedType
{
    [Range(10, 100)]
    public int IntegerWithRange { get; set; } = 10;
}

public class NonSkippedBaseType
{
    [Range(10, 100)]
    public int IntegerWithRange1 { get; set; } = 10;
}

public class NonSkippedSubType : NonSkippedBaseType
{
    [Range(10, 100)]
    public int IntegerWithRange2 { get; set; } = 10;
}

[SkipValidation]
public class AlwaysSkippedType
{
    public NestedType ObjectProperty { get; set; } = new NestedType();
}

[SkipValidation]
public class SkippedBaseType
{
    [Range(10, 100)]
    public int IntegerWithRange1 { get; set; } = 10;
}

public class SubTypeOfSkippedBase : SkippedBaseType
{
    [Range(10, 100)]
    public int IntegerWithRange2 { get; set; } = 10;
}
""";
        await Verify(source, out var compilation);
        await VerifyValidatableType(compilation, "ComplexType", async (validationOptions, type) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(type, out var validatableTypeInfo));

            await InvalidSkippedInteger_DoesNotProduceError(validatableTypeInfo);
            await InvalidNestedInteger_ProducesError(validatableTypeInfo);
            await InvalidSkippedNestedInteger_DoesNotProduceError(validatableTypeInfo);
            await InvalidList_ProducesError(validatableTypeInfo);
            await InvalidSkippedList_DoesNotProduceError(validatableTypeInfo);
            await InvalidSubTypeNestedIntegers_ProduceErrors(validatableTypeInfo);
            await InvalidAlwaysSkippedType_DoesNotProduceError(validatableTypeInfo);

            async Task InvalidSkippedInteger_DoesNotProduceError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                var intProperty = type.GetProperty("IntegerWithRange");
                intProperty?.SetValue(instance, 5); // Set invalid value

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Null(context.ValidationErrors);
            }

            async Task InvalidNestedInteger_ProducesError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                var objectPropertyInstance = type.GetProperty("ObjectProperty").GetValue(instance);
                var nestedIntProperty = objectPropertyInstance.GetType().GetProperty("IntegerWithRange");
                nestedIntProperty?.SetValue(objectPropertyInstance, 5); // Set invalid value

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Collection(context.ValidationErrors, kvp =>
                {
                    Assert.Equal("ObjectProperty.IntegerWithRange", kvp.Key);
                    Assert.Equal("The field IntegerWithRange must be between 10 and 100.", kvp.Value.Single());
                });
            }

            async Task InvalidSkippedNestedInteger_DoesNotProduceError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                var objectPropertyInstance = type.GetProperty("SkippedObjectProperty").GetValue(instance);
                var nestedIntProperty = objectPropertyInstance.GetType().GetProperty("IntegerWithRange");
                nestedIntProperty?.SetValue(objectPropertyInstance, 5); // Set invalid value

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Null(context.ValidationErrors);
            }

            async Task InvalidList_ProducesError(IValidatableInfo validatableInfo)
            {
                var rootInstance = Activator.CreateInstance(type);
                var listInstance = Activator.CreateInstance(typeof(List<>).MakeGenericType(type.Assembly.GetType("NestedType")!));

                // Create invalid item
                var nestedTypeInstance = Activator.CreateInstance(type.Assembly.GetType("NestedType")!);
                nestedTypeInstance.GetType().GetProperty("IntegerWithRange")?.SetValue(nestedTypeInstance, 5);

                // Add to list
                listInstance.GetType().GetMethod("Add")?.Invoke(listInstance, [nestedTypeInstance]);

                type.GetProperty("ListOfNestedTypes")?.SetValue(rootInstance, listInstance);
                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(rootInstance)
                };

                await validatableTypeInfo.ValidateAsync(rootInstance, context, CancellationToken.None);

                Assert.Collection(context.ValidationErrors, kvp =>
                {
                    Assert.Equal("ListOfNestedTypes[0].IntegerWithRange", kvp.Key);
                    Assert.Equal("The field IntegerWithRange must be between 10 and 100.", kvp.Value.Single());
                });
            }

            async Task InvalidSkippedList_DoesNotProduceError(IValidatableInfo validatableInfo)
            {
                var rootInstance = Activator.CreateInstance(type);
                var listInstance = Activator.CreateInstance(typeof(List<>).MakeGenericType(type.Assembly.GetType("NestedType")!));

                // Create invalid item
                var nestedTypeInstance = Activator.CreateInstance(type.Assembly.GetType("NestedType")!);
                nestedTypeInstance.GetType().GetProperty("IntegerWithRange")?.SetValue(nestedTypeInstance, 5);

                // Add to list
                listInstance.GetType().GetMethod("Add")?.Invoke(listInstance, [nestedTypeInstance]);

                type.GetProperty("SkippedListOfNestedTypes")?.SetValue(rootInstance, listInstance);
                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(rootInstance)
                };

                await validatableTypeInfo.ValidateAsync(rootInstance, context, CancellationToken.None);

                Assert.Null(context.ValidationErrors);
            }

            async Task InvalidSubTypeNestedIntegers_ProduceErrors(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                var objectPropertyInstance = type.GetProperty("NonSkippedSubTypeProperty").GetValue(instance);
                var nestedIntProperty1 = objectPropertyInstance.GetType().GetProperty("IntegerWithRange1");
                nestedIntProperty1?.SetValue(objectPropertyInstance, 5); // Set invalid value
                var nestedIntProperty2 = objectPropertyInstance.GetType().GetProperty("IntegerWithRange2");
                nestedIntProperty2?.SetValue(objectPropertyInstance, 6); // Set invalid value

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                // Errors are (currently) reported in the order from derived to base type.
                Assert.Collection(context.ValidationErrors,
                    kvp =>
                    {
                        Assert.Equal("NonSkippedSubTypeProperty.IntegerWithRange2", kvp.Key);
                        Assert.Equal("The field IntegerWithRange2 must be between 10 and 100.", kvp.Value.Single());
                    },
                    kvp =>
                    {
                        Assert.Equal("NonSkippedSubTypeProperty.IntegerWithRange1", kvp.Key);
                        Assert.Equal("The field IntegerWithRange1 must be between 10 and 100.", kvp.Value.Single());
                    });
            }

            async Task InvalidAlwaysSkippedType_DoesNotProduceError(IValidatableInfo validatableInfo)
            {
                var instance = Activator.CreateInstance(type);
                var objectPropertyInstance = type.GetProperty("AlwaysSkippedProperty").GetValue(instance);
                var nestedIntProperty = objectPropertyInstance.GetType().GetProperty("IntegerWithRange");
                nestedIntProperty?.SetValue(objectPropertyInstance, 5); // Set invalid value

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Null(context.ValidationErrors);
            }
        });
    }

    [Fact]
    public async Task DoesNotEmit_ForSkipValidationAttribute_OnRecordProperties()
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

static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddValidation();
        var app = builder.Build();
        app.Run();
    }
}

[ValidatableType]
public record ComplexType(
    [Range(10, 100)][SkipValidation] int IntegerWithRange,
    NestedType ObjectProperty,
    [SkipValidation] NestedType SkippedObjectProperty
);

public record NestedType
{
    [Range(10, 100)]
    public int IntegerWithRange { get; set; } = 10;
}

[SkipValidation]
public record AlwaysSkippedType
{
    public NestedType ObjectProperty { get; set; } = new NestedType();
}
""";
        await Verify(source, out var compilation);
        await VerifyValidatableType(compilation, "ComplexType", async (validationOptions, type) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(type, out var validatableTypeInfo));

            await InvalidNestedIntegerWithRangeProducesError(validatableTypeInfo);
            await InvalidSkippedNestedIntegerWithRangeDoesNotProduceProduceError(validatableTypeInfo);
            await InvalidSkippedIntegerWithRangeDoesNotProduceError(validatableTypeInfo);

            async Task InvalidNestedIntegerWithRangeProducesError(IValidatableInfo validatableInfo)
            {
                var objectProperty = type.GetProperty("ObjectProperty");
                var nestedType = objectProperty.PropertyType;
                var nestedTypeInstance = Activator.CreateInstance(nestedType);
                var skippedNestedTypeInstance = Activator.CreateInstance(nestedType);
                nestedTypeInstance.GetType().GetProperty("IntegerWithRange")?.SetValue(nestedTypeInstance, 5); // Set invalid value
                var instance = Activator.CreateInstance(type, 10, nestedTypeInstance, skippedNestedTypeInstance);

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Collection(context.ValidationErrors, kvp =>
                {
                    Assert.Equal("ObjectProperty.IntegerWithRange", kvp.Key);
                    Assert.Equal("The field IntegerWithRange must be between 10 and 100.", kvp.Value.Single());
                });
            }

            async Task InvalidSkippedNestedIntegerWithRangeDoesNotProduceProduceError(IValidatableInfo validatableInfo)
            {
                var objectProperty = type.GetProperty("ObjectProperty");
                var nestedType = objectProperty.PropertyType;
                var nestedTypeInstance = Activator.CreateInstance(nestedType);
                var skippedNestedTypeInstance = Activator.CreateInstance(nestedType);
                skippedNestedTypeInstance.GetType().GetProperty("IntegerWithRange")?.SetValue(skippedNestedTypeInstance, 5); // Set invalid value
                var instance = Activator.CreateInstance(type, 10, nestedTypeInstance, skippedNestedTypeInstance);

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Null(context.ValidationErrors);
            }

            async Task InvalidSkippedIntegerWithRangeDoesNotProduceError(IValidatableInfo validatableInfo)
            {
                var objectProperty = type.GetProperty("ObjectProperty");
                var nestedType = objectProperty.PropertyType;
                var nestedTypeInstance = Activator.CreateInstance(nestedType);
                var instance = Activator.CreateInstance(type, 5, nestedTypeInstance, nestedTypeInstance); // Create with invalid value

                var context = new ValidateContext
                {
                    ValidationOptions = validationOptions,
                    ValidationContext = new ValidationContext(instance)
                };

                await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

                Assert.Null(context.ValidationErrors);
            }
        });
    }

    [Fact]
    public async Task DoesNotEmit_ForSkipValidationAttribute_OnEndpointParameters()
    {
        var source = """
#pragma warning disable ASP0029

using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddValidation();
        var app = builder.Build();

        app.MapPost("/simple-params", (
            [Range(10, 100)] int intParam,
            [SkipValidation][Range(10, 100)] int skippedIntParam) => "OK");

        app.MapPost("/non-skipped-complex-type", (ComplexType objectParam) => "OK");

        app.MapPost("/skipped-complex-type", ([SkipValidation] ComplexType objectParam) => "OK");

        app.MapPost("/always-skipped-type", (AlwaysSkippedType objectParam) => "OK");

        app.Run();
    }
}

// This should have generated validation code
public class ComplexType
{
    [Range(10, 100)]
    public int IntegerWithRange { get; set; } = 10;
}

// This should have generated validation code
[SkipValidation]
public class AlwaysSkippedType
{
    public ComplexType ObjectProperty { get; set; } = new ComplexType();
}
""";
        await Verify(source, out var compilation);

        await VerifyEndpoint(compilation, "/simple-params", async (endpoint, serviceProvider) =>
        {
            var context = CreateHttpContext(serviceProvider);
            context.Request.QueryString = new QueryString("?intParam=5&skippedIntParam=5");
            await endpoint.RequestDelegate(context);
            var problemDetails = await AssertBadRequest(context);

            Assert.Collection(problemDetails.Errors,
                error =>
                {
                    Assert.Equal("intParam", error.Key);
                    Assert.Equal("The field intParam must be between 10 and 100.", error.Value.Single());
                }
            );
        });

        await VerifyEndpoint(compilation, "/non-skipped-complex-type", async (endpoint, serviceProvider) =>
        {
            var payload = """
                    {
                        "IntegerWithRange": 5
                    }
                    """;

            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);
            var problemDetails = await AssertBadRequest(context);

            Assert.Collection(problemDetails.Errors,
                error =>
                {
                    Assert.Equal("IntegerWithRange", error.Key);
                    Assert.Equal("The field IntegerWithRange must be between 10 and 100.", error.Value.Single());
                }
            );
        });

        await VerifyEndpoint(compilation, "/skipped-complex-type", async (endpoint, serviceProvider) =>
        {
            var payload = """
                    {
                        "IntegerWithRange": 5
                    }
                    """;

            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        });

        await VerifyEndpoint(compilation, "/always-skipped-type", async (endpoint, serviceProvider) =>
        {
            var payload = """
                    {
                        "IntegerWithRange": 5
                    }
                    """;

            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        });
    }
}
