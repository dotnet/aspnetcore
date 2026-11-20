// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static Microsoft.Extensions.Validation.Tests.ValidationTestBase;

namespace Microsoft.Extensions.Validation.GeneratorTests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task CanValidateInheritedPropertyFromBaseTypeInReferencedAssembly(bool useAsync, bool registerLibraryValidationFirst)
    {
        var libraryReference = CompileAndLoadReferencedAssembly("""
#nullable enable

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Validation;

namespace ValidatableTypesAssembly
{
    [ValidatableType]
    public class LibraryBaseModel
    {
        [Required]
        public string? Name { get; set; }
    }
}

namespace ValidatableTypesAssembly.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddValidationForLibraryTypes(this IServiceCollection services)
        {
            return services.AddValidation();
        }
    }
}
""");

        var registrations = registerLibraryValidationFirst
            ? """
builder.Services.AddValidationForLibraryTypes();
builder.Services.AddValidation();
"""
            : """
builder.Services.AddValidation();
builder.Services.AddValidationForLibraryTypes();
""";

        var source = $$"""
#nullable enable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Validation;
using ValidatableTypesAssembly;
using ValidatableTypesAssembly.Extensions;

var builder = WebApplication.CreateBuilder();

{{registrations}}

var app = builder.Build();

app.Run();

[ValidatableType]
public class DerivedModel : LibraryBaseModel
{
    [Range(1, 10)]
    public int Quantity { get; set; }
}
""";
        await Verify(source, out var compilation, additionalReferences: [libraryReference]);
        await VerifyValidatableType(compilation, "DerivedModel", async (validationOptions, type) =>
        {
            Assert.True(validationOptions.TryGetValidatableTypeInfo(type, out var validatableTypeInfo));

            var instance = Activator.CreateInstance(type);
            var context = new ValidateContext
            {
                ValidationOptions = validationOptions,
                ServiceProvider = null,
            };

            await ValidateAsync(validatableTypeInfo, instance, context, useAsync, CancellationToken.None);

            Assert.NotNull(context.ValidationErrors);
            Assert.Collection(context.ValidationErrors,
                kvp =>
                {
                    Assert.Equal("Quantity", kvp.Key);
                    Assert.Equal("The field Quantity must be between 1 and 10.", kvp.Value.Select(e => e.ErrorMessage).Single());
                },
                kvp =>
                {
                    Assert.Equal("Name", kvp.Key);
                    Assert.Equal("The Name field is required.", kvp.Value.Select(e => e.ErrorMessage).Single());
                });
        });
    }
}
