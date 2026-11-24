// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using static Microsoft.Extensions.Validation.Tests.ValidationTestBase;

namespace Microsoft.Extensions.Validation.GeneratorTests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    private const string SharedModelTypes = """
namespace SharedModels
{
    [ValidatableType]
    public class BaseModel
    {
        [Required]
        public string? Name { get; set; }
    }
}
""";

    private const string SharedModelsLibrarySource = $$"""
#nullable enable

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Validation;

{{SharedModelTypes}}

namespace SharedModels.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddValidationForSharedModels(this IServiceCollection services)
            => services.AddValidation();
    }
}
""";

    private const string CustomBaseModelResolverSource = """
// Provides a custom rule for BaseModel.Name that replaces the generated one.
public sealed class CustomBaseModelResolver : IValidatableInfoResolver
{
    public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableTypeInfo? validatableTypeInfo)
    {
        validatableTypeInfo = type == typeof(BaseModel) ? new CustomBaseModelInfo() : null;
        return validatableTypeInfo is not null;
    }

    public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableParameterInfo? validatableParameterInfo)
    {
        validatableParameterInfo = null;
        return false;
    }
}

public sealed class CustomBaseModelInfo : IValidatableTypeInfo
{
    public void Validate(object? value, ValidateContext context)
    {
        if (value is not null && TryFindProperty("Name", context.ValidationOptions, out var name))
        {
            name.Validate(value, context);
        }
    }

    public Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken)
    {
        Validate(value, context);
        return Task.CompletedTask;
    }

    public bool TryFindProperty(string propertyName, ValidationOptions validationOptions, [NotNullWhen(true)] out IValidatablePropertyInfo? validatablePropertyInfo)
    {
        validatablePropertyInfo = propertyName == "Name" ? new CustomNameRule() : null;
        return validatablePropertyInfo is not null;
    }
}

public sealed class CustomNameRule : IValidatablePropertyInfo
{
    public void Validate(object containingObject, ValidateContext context)
        => context.AddValidationError(new ValidationError { Name = "Name", Path = "Name", ErrorMessage = "Custom Name rule", Container = containingObject });

    public Task ValidateAsync(object containingObject, ValidateContext context, CancellationToken cancellationToken)
    {
        Validate(containingObject, context);
        return Task.CompletedTask;
    }
}
""";

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task InheritedMembersUseGeneratedMetadataWhenAnotherResolverProvidesBaseType(bool useAsync, bool baseInReferencedAssembly)
    {
        var additionalReferences = baseInReferencedAssembly
            ? new[] { CompileAndLoadReferencedAssembly(SharedModelsLibrarySource) }
            : [];

        var compilation = RunGenerator($$"""
#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Validation;
using SharedModels;
{{(baseInReferencedAssembly ? "using SharedModels.Extensions;" : "")}}

var builder = WebApplication.CreateBuilder();

{{(baseInReferencedAssembly ? "builder.Services.AddValidationForSharedModels();" : "")}}
builder.Services.AddValidation(options => options.Resolvers.Insert(0, new CustomBaseModelResolver()));

var app = builder.Build();

app.Run();

[ValidatableType]
public class DerivedModel : BaseModel
{
    [Range(1, 10)]
    public int Quantity { get; set; }
}

{{CustomBaseModelResolverSource}}

{{(baseInReferencedAssembly ? "" : SharedModelTypes)}}
""", additionalReferences);

        await VerifyValidatableType(compilation, "DerivedModel", async (validationOptions, type) =>
        {
            // The custom resolver still wins for the base type itself.
            Assert.True(validationOptions.TryGetValidatableTypeInfo(type.BaseType, out var baseTypeInfo));
            Assert.Equal("CustomBaseModelInfo", baseTypeInfo.GetType().Name);

            Assert.True(validationOptions.TryGetValidatableTypeInfo(type, out var typeInfo));
            var instance = Activator.CreateInstance(type);
            var context = new ValidateContext { ValidationOptions = validationOptions, ServiceProvider = null };

            await ValidateAsync(typeInfo, instance, context, useAsync, CancellationToken.None);

            Assert.NotNull(context.ValidationErrors);
            Assert.Equal(new[] { "Name", "Quantity" }, context.ValidationErrors.Keys.Order().ToArray());
            Assert.Equal("The Name field is required.", Assert.Single(context.ValidationErrors["Name"]).ErrorMessage);

            // Field-level validation, as used by Blazor forms, resolves the same rule.
            Assert.True(typeInfo.TryFindProperty("Name", validationOptions, out var nameInfo));
            var fieldContext = new ValidateContext { ValidationOptions = validationOptions, ServiceProvider = null };
            await ValidateAsync(nameInfo, instance, fieldContext, useAsync, CancellationToken.None);
            Assert.NotNull(fieldContext.ValidationErrors);
            Assert.Equal("The Name field is required.", Assert.Single(fieldContext.ValidationErrors["Name"]).ErrorMessage);
        });
    }
}
