#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.Extensions.Validation.Tests;

public class ValidatableInfoResolverTests
{
    public delegate void TryGetValidatableTypeInfoCallback(Type type, out IValidatableInfo? validatableInfo);

    [Fact]
    public void ResolversChain_ProcessesInCorrectOrder()
    {
        // Arrange
        var services = new ServiceCollection();

        var resolver1 = new Mock<IValidatableInfoResolver>();
        var resolver2 = new Mock<IValidatableInfoResolver>();
        var resolver3 = new Mock<IValidatableInfoResolver>();

        // Create the object that will be returned by resolver2
        var mockTypeInfo = new Mock<ValidatableTypeInfo>(typeof(ValidatableType), Array.Empty<ValidatablePropertyInfo>()).Object;

        // Setup resolver1 to return false (doesn't handle this type)
        resolver1
            .Setup(r => r.TryGetValidatableTypeInfo(typeof(ValidatableType), out It.Ref<IValidatableInfo?>.IsAny))
            .Callback(new TryGetValidatableTypeInfoCallback((Type t, out IValidatableInfo? info) =>
            {
                info = null;
            }))
            .Returns(false);

        // Setup resolver2 to return true and set the mock type info
        resolver2
            .Setup(r => r.TryGetValidatableTypeInfo(typeof(ValidatableType), out It.Ref<IValidatableInfo?>.IsAny))
            .Callback(new TryGetValidatableTypeInfoCallback((Type t, out IValidatableInfo? info) =>
            {
                info = mockTypeInfo;
            }))
            .Returns(true);

        services.AddValidation(Options =>
        {
            Options.Resolvers.Add(resolver1.Object);
            Options.Resolvers.Add(resolver2.Object);
            Options.Resolvers.Add(resolver3.Object);
        });

        var serviceProvider = services.BuildServiceProvider();
        var validationOptions = serviceProvider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        // Act
        var result = validationOptions.TryGetValidatableTypeInfo(typeof(ValidatableType), out var validatableInfo);

        // Assert
        Assert.True(result);
        Assert.NotNull(validatableInfo);
        Assert.Equal(typeof(ValidatableType), ((ValidatableTypeInfo)validatableInfo).Type);

        // Verify that resolvers were called in the expected order
        resolver1.Verify(r => r.TryGetValidatableTypeInfo(typeof(ValidatableType), out It.Ref<IValidatableInfo?>.IsAny), Times.Once);
        resolver2.Verify(r => r.TryGetValidatableTypeInfo(typeof(ValidatableType), out It.Ref<IValidatableInfo?>.IsAny), Times.Once);
        resolver3.Verify(r => r.TryGetValidatableTypeInfo(typeof(ValidatableType), out It.Ref<IValidatableInfo?>.IsAny), Times.Never);
    }

    // Test types

    [ValidatableType]
    private class ValidatableType
    {
        [Required]
        public string Name { get; set; } = "";
    }
}
