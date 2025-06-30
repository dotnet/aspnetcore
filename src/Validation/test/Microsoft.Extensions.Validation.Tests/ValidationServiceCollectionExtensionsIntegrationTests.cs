#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Validation.Tests;

public class ValidationServiceCollectionExtensionsIntegrationTests
{
    [Fact]
    public void AddValidation_RegistersRuntimeValidatableTypeInfoResolver()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddValidation();
        var serviceProvider = services.BuildServiceProvider();
        var validationOptions = serviceProvider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        // Assert
        Assert.NotEmpty(validationOptions.Resolvers);
        Assert.Contains(validationOptions.Resolvers, r => r is RuntimeValidatableTypeInfoResolver);
        Assert.Contains(validationOptions.Resolvers, r => r is RuntimeValidatableParameterInfoResolver);
    }

    [Fact]
    public void AddValidation_RuntimeTypeResolverCanResolveComplexTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddValidation();
        var serviceProvider = services.BuildServiceProvider();
        var validationOptions = serviceProvider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        // Act
        var result = validationOptions.TryGetValidatableTypeInfo(typeof(TestPoco), out var validatableInfo);

        // Assert
        Assert.True(result);
        Assert.NotNull(validatableInfo);
        Assert.IsType<RuntimeValidatableTypeInfoResolver.RuntimeValidatableTypeInfo>(validatableInfo);
    }

    [Fact]
    public void AddValidation_RuntimeTypeResolverReturnsNullForPrimitiveTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddValidation();
        var serviceProvider = services.BuildServiceProvider();
        var validationOptions = serviceProvider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        // Act
        var result = validationOptions.TryGetValidatableTypeInfo(typeof(int), out var validatableInfo);

        // Assert
        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void AddValidation_ResolversAreInCorrectOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddValidation();
        var serviceProvider = services.BuildServiceProvider();
        var validationOptions = serviceProvider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        // Assert - RuntimeValidatableParameterInfoResolver should come first, then RuntimeValidatableTypeInfoResolver
        var parameterResolverIndex = -1;
        var typeResolverIndex = -1;

        for (int i = 0; i < validationOptions.Resolvers.Count; i++)
        {
            if (validationOptions.Resolvers[i] is RuntimeValidatableParameterInfoResolver)
            {
                parameterResolverIndex = i;
            }
            else if (validationOptions.Resolvers[i] is RuntimeValidatableTypeInfoResolver)
            {
                typeResolverIndex = i;
            }
        }

        Assert.True(parameterResolverIndex >= 0, "RuntimeValidatableParameterInfoResolver should be registered");
        Assert.True(typeResolverIndex >= 0, "RuntimeValidatableTypeInfoResolver should be registered");
        Assert.True(parameterResolverIndex < typeResolverIndex, "Parameter resolver should come before type resolver");
    }

    public class TestPoco
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(0, 100)]
        public int Age { get; set; }
    }
}