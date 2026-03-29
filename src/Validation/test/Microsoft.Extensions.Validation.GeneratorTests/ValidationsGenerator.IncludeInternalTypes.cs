// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.GeneratorTests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    #region Internal Types

    [Fact]
    public async Task InternalType_NotValidated_ByDefault()
    {
        var source = """
#pragma warning disable ASP0029

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();
builder.Services.AddValidation();
var app = builder.Build();
app.Run();

internal class InternalClass
{
    [Required]
    internal string Property { get; set; } = string.Empty;
}
""";
        await Verify(source, out var compilation);
        await VerifyValidatableType(compilation, "InternalClass", async (validationOptions, type) =>
        {
            Assert.False(validationOptions.TryGetValidatableTypeInfo(type, out _));
        });
    }

    [Fact]
    public async Task InternalType_Validated_With_IncludeInternalTypes()
    {
        var source = """
#pragma warning disable ASP0029

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();
builder.Services.AddValidation(options => options.IncludeInternalTypes());
var app = builder.Build();
app.Run();

[ValidatableType]
internal class InternalClass
{
    [Required]
    internal string Property { get; set; } = string.Empty;
}
""";
        await Verify(source, out var compilation);
        await VerifyValidatableType(compilation, "InternalClass", async (validationOptions, type) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(type, out var validatableTypeInfo));

            var instance = Activator.CreateInstance(type);
            type.GetProperty("Property")?.SetValue(instance, null);

            var context = new ValidateContext
            {
                ValidationOptions = validationOptions,
                ValidationContext = new ValidationContext(instance)
            };

            await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

            Assert.Collection(context.ValidationErrors,
                kvp =>
                {
                    Assert.Equal("Property", kvp.Key);
                    Assert.Equal("The Property field is required.", kvp.Value.Single());
                });
        });
    }

    #endregion

    #region Internal Properties

    [Fact]
    public async Task InternalProperty_Validated_With_IncludeInternalTypes()
    {
        var source = """
#pragma warning disable ASP0029

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();
builder.Services.AddValidation(options => options.IncludeInternalTypes());
var app = builder.Build();
app.Run();

[ValidatableType]
public class ClassWithInternalProperty
{
    [Required]
    public string PublicProperty { get; set; } = string.Empty;

    [Required]
    internal string InternalProperty { get; set; } = string.Empty;
}
""";
        await Verify(source, out var compilation);
        await VerifyValidatableType(compilation, "ClassWithInternalProperty", async (validationOptions, type) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(type, out var validatableTypeInfo));

            var instance = Activator.CreateInstance(type);
            type.GetProperty("InternalProperty")?.SetValue(instance, null);

            var context = new ValidateContext
            {
                ValidationOptions = validationOptions,
                ValidationContext = new ValidationContext(instance)
            };

            await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

            Assert.Contains(context.ValidationErrors, kvp => kvp.Key == "InternalProperty");
        });
    }

    [Fact]
    public async Task InternalProperty_NotValidated_ByDefault()
    {
        var source = """
#pragma warning disable ASP0029

using System;
using System.ComponentModel.DataAnnotations;
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
public class ClassWithInternalProperty
{
    [Required]
    internal string InternalProperty { get; set; } = string.Empty;

    [Required]
    public string PublicProperty { get; set; } = string.Empty;
}
""";
        await Verify(source, out var compilation);
        await VerifyValidatableType(compilation, "ClassWithInternalProperty", async (validationOptions, type) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(type, out var validatableTypeInfo));

            var instance = Activator.CreateInstance(type);
            type.GetProperty("InternalProperty")?.SetValue(instance, null);
            type.GetProperty("PublicProperty")?.SetValue(instance, null);

            var context = new ValidateContext
            {
                ValidationOptions = validationOptions,
                ValidationContext = new ValidationContext(instance)
            };

            await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

            Assert.DoesNotContain(context.ValidationErrors, kvp => kvp.Key == "InternalProperty");
        });
    }

    #endregion

    #region Private and Protected Properties

    [Fact]
    public async Task PrivateProperty_Never_Validated()
    {
        var source = """
#pragma warning disable ASP0029

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();
builder.Services.AddValidation(options => options.IncludeInternalTypes());
var app = builder.Build();
app.Run();

[ValidatableType]
public class ClassWithPrivateProperty
{
    [Required]
    public string PublicProperty {get; set; } = string.Empty;

    [Required]
    private string PrivateProperty { get; set; } = string.Empty;
}
""";
        await Verify(source, out var compilation);
        await VerifyValidatableType(compilation, "ClassWithPrivateProperty", async (validationOptions, type) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(type, out var validatableTypeInfo));

            var instance = Activator.CreateInstance(type);

            var context = new ValidateContext
            {
                ValidationOptions = validationOptions,
                ValidationContext = new ValidationContext(instance)
            };

            await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

            // Private property should not produce validation errors
            Assert.DoesNotContain(context.ValidationErrors, kvp => kvp.Key == "PrivateProperty");
        });
    }

    [Fact]
    public async Task ProtectedProperty_Never_Validated()
    {
        var source = """
#pragma warning disable ASP0029

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();
builder.Services.AddValidation(options => options.IncludeInternalTypes());
var app = builder.Build();
app.Run();

[ValidatableType]
public class ClassWithProtectedProperty
{
    [Required]
    public string PublicProperty {get; set; } = string.Empty;

    [Required]
    protected string ProtectedProperty { get; set; } = string.Empty;
}
""";
        await Verify(source, out var compilation);
        await VerifyValidatableType(compilation, "ClassWithProtectedProperty", async (validationOptions, type) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(type, out var validatableTypeInfo));

            var instance = Activator.CreateInstance(type);

            var context = new ValidateContext
            {
                ValidationOptions = validationOptions,
                ValidationContext = new ValidationContext(instance)
            };

            await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

            // Protected property should not produce validation errors
            Assert.DoesNotContain(context.ValidationErrors, kvp => kvp.Key == "ProtectedProperty");
        });
    }

    #endregion

    #region Internal Nested Types

    [Fact]
    public async Task InternalNestedType_Validated_With_IncludeInternalType()
    {
        var source = """
#pragma warning disable ASP0029

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();
builder.Services.AddValidation(options => options.IncludeInternalTypes());
var app = builder.Build();
app.Run();

[ValidatableType]
public class OuterClass
{
    [Required]
    internal InternalNestedClass NestedProperty { get; set; } = new InternalNestedClass();
}

[ValidatableType]
internal class InternalNestedClass
{
    [Required]
    public string RequiredProperty { get; set; } = string.Empty;
}
""";
        await Verify(source, out var compilation);
        await VerifyValidatableType(compilation, "OuterClass", async (validationOptions, type) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(type, out var validatableTypeInfo));

            var instance = Activator.CreateInstance(type);
            var nestedProperty = type.GetProperty("NestedProperty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var nestedInstance = Activator.CreateInstance(nestedProperty.PropertyType);
            nestedProperty.PropertyType.GetProperty("RequiredProperty")?.SetValue(nestedInstance, null);
            nestedProperty?.SetValue(instance, nestedInstance);

            var context = new ValidateContext
            {
                ValidationOptions = validationOptions,
                ValidationContext = new ValidationContext(instance)
            };

            await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

            Assert.Collection(context.ValidationErrors, kvp =>
            {
                Assert.Equal("NestedProperty.RequiredProperty", kvp.Key);
                Assert.Equal("The RequiredProperty field is required.", kvp.Value.Single());
            });
        });
    }

    #endregion

    #region Configuration Syntax

    [Fact]
    public async Task IncludeInternalTypes_BlockLambda_Supported()
    {
        // IncludeInternalTypes=true works with block lambda syntax
        var source = """
#pragma warning disable ASP0029

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation(options =>
{
    options.IncludeInternalTypes();
});

var app = builder.Build();
app.Run();

[ValidatableType]
public class ClassWithInternalProperty
{
    [Required]
    public string PublicProperty { get; set; } = string.Empty;

    [Required]
    internal string InternalProperty { get; set; } = string.Empty;
}
""";
        await Verify(source, out var compilation);
        await VerifyValidatableType(compilation, "ClassWithInternalProperty", async (validationOptions, type) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(type, out var validatableTypeInfo));

            var instance = Activator.CreateInstance(type);
            type.GetProperty("PublicProperty")?.SetValue(instance, null);
            type.GetProperty("InternalProperty")?.SetValue(instance, null);

            var context = new ValidateContext
            {
                ValidationOptions = validationOptions,
                ValidationContext = new ValidationContext(instance)
            };

            await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

            // Both public and internal properties produce errors
            Assert.Collection(context.ValidationErrors,
                kvp =>
                {
                    Assert.Equal("PublicProperty", kvp.Key);
                    Assert.Equal("The PublicProperty field is required.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("InternalProperty", kvp.Key);
                    Assert.Equal("The InternalProperty field is required.", kvp.Value.Single());
                });
        });
    }

    [Fact]
    public async Task IncludeInternalTypes_ExpressionLambda_Supported()
    {
        // IncludeInternalTypes=true works with expression lambda syntax
        var source = """
#pragma warning disable ASP0029

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();
builder.Services.AddValidation(options => options.IncludeInternalTypes());
var app = builder.Build();
app.Run();

[ValidatableType]
public class ClassWithInternalProperty
{
    [Required]
    internal string InternalProperty { get; set; } = string.Empty;
}
""";
        await Verify(source, out var compilation);
        await VerifyValidatableType(compilation, "ClassWithInternalProperty", async (validationOptions, type) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(type, out var validatableTypeInfo));

            var instance = Activator.CreateInstance(type);
            type.GetProperty("InternalProperty")?.SetValue(instance, null);

            var context = new ValidateContext
            {
                ValidationOptions = validationOptions,
                ValidationContext = new ValidationContext(instance)
            };

            await validatableTypeInfo.ValidateAsync(instance, context, CancellationToken.None);

            Assert.Collection(context.ValidationErrors,
                kvp =>
                {
                    Assert.Equal("InternalProperty", kvp.Key);
                    Assert.Equal("The InternalProperty field is required.", kvp.Value.Single());
                });
        });
    }

    #endregion
}
