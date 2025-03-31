// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Http.Validation.Tests;

public class ValidatableInfoResolverTests
{
    public delegate void TryGetValidatableTypeInfoCallback(Type type, out IValidatableInfo? validatableInfo);
    public delegate void TryGetValidatableParameterInfoCallback(ParameterInfo parameter, out IValidatableInfo? validatableInfo);

    [Fact]
    public void GetValidatableTypeInfo_ReturnsNull_ForNonValidatableType()
    {
        // Arrange
        var resolver = new Mock<IValidatableInfoResolver>();
        IValidatableInfo? validatableInfo = null;
        resolver.Setup(r => r.TryGetValidatableTypeInfo(It.IsAny<Type>(), out validatableInfo)).Returns(false);

        // Act
        var result = resolver.Object.TryGetValidatableTypeInfo(typeof(NonValidatableType), out validatableInfo);

        // Assert
        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void GetValidatableTypeInfo_ReturnsTypeInfo_ForValidatableType()
    {
        // Arrange
        var mockTypeInfo = new Mock<ValidatableTypeInfo>(
            typeof(ValidatableType),
            Array.Empty<ValidatablePropertyInfo>()).Object;

        var resolver = new Mock<IValidatableInfoResolver>();
        IValidatableInfo? validatableInfo = null;
        resolver
            .Setup(r => r.TryGetValidatableTypeInfo(typeof(ValidatableType), out validatableInfo))
            .Callback(new TryGetValidatableTypeInfoCallback((t, out info) =>
            {
                info = mockTypeInfo; // Set the out parameter to our mock
            }))
            .Returns(true);

        // Act
        var result = resolver.Object.TryGetValidatableTypeInfo(typeof(ValidatableType), out validatableInfo);

        // Assert
        Assert.True(result);
        var validatableTypeInfo = Assert.IsAssignableFrom<ValidatableTypeInfo>(validatableInfo);
        Assert.Equal(typeof(ValidatableType), validatableTypeInfo.Type);
    }

    [Fact]
    public void GetValidatableParameterInfo_ReturnsNull_ForNonValidatableParameter()
    {
        // Arrange
        var method = typeof(TestMethods).GetMethod(nameof(TestMethods.MethodWithNonValidatableParam))!;
        var parameter = method.GetParameters()[0];

        var resolver = new Mock<IValidatableInfoResolver>();
        IValidatableInfo? validatableInfo = null;
        resolver.Setup(r => r.TryGetValidatableParameterInfo(It.IsAny<ParameterInfo>(), out validatableInfo)).Returns(false);

        // Act
        var result = resolver.Object.TryGetValidatableParameterInfo(parameter, out validatableInfo);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetValidatableParameterInfo_ReturnsParameterInfo_ForValidatableParameter()
    {
        // Arrange
        var method = typeof(TestMethods).GetMethod(nameof(TestMethods.MethodWithValidatableParam))!;
        var parameter = method.GetParameters()[0];

        var mockParamInfo = new Mock<ValidatableParameterInfo>(
            typeof(string),
            "model",
            "model").Object;

        var resolver = new Mock<IValidatableInfoResolver>();

        // Setup using the same pattern as in the type info test
        resolver.Setup(r => r.TryGetValidatableParameterInfo(parameter, out It.Ref<IValidatableInfo?>.IsAny))
            .Callback(new TryGetValidatableParameterInfoCallback((ParameterInfo p, out IValidatableInfo? info) =>
            {
                info = mockParamInfo; // Set the out parameter to our mock
            }))
            .Returns(true);

        // Act
        var result = resolver.Object.TryGetValidatableParameterInfo(parameter, out var validatableInfo);

        // Assert
        Assert.True(result);
        var validatableParamInfo = Assert.IsAssignableFrom<ValidatableParameterInfo>(validatableInfo);
        Assert.Equal("model", validatableParamInfo.Name);
    }

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
    private class NonValidatableType { }

    [ValidatableType]
    private class ValidatableType
    {
        [Required]
        public string Name { get; set; } = "";
    }

    private static class TestMethods
    {
        public static void MethodWithNonValidatableParam(NonValidatableType param) { }
        public static void MethodWithValidatableParam(ValidatableType model) { }
    }

    // Test implementations
    private class TestValidatablePropertyInfo : ValidatablePropertyInfo
    {
        private readonly ValidationAttribute[] _validationAttributes;

        public TestValidatablePropertyInfo(
            Type containingType,
            Type propertyType,
            string name,
            string displayName,
            ValidationAttribute[] validationAttributes)
            : base(containingType, propertyType, name, displayName)
        {
            _validationAttributes = validationAttributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    private class TestValidatableParameterInfo : ValidatableParameterInfo
    {
        private readonly ValidationAttribute[] _validationAttributes;

        public TestValidatableParameterInfo(
            Type parameterType,
            string name,
            string displayName,
            ValidationAttribute[] validationAttributes)
            : base(parameterType, name, displayName)
        {
            _validationAttributes = validationAttributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    private class TestValidatableTypeInfo(
        Type type,
        ValidatablePropertyInfo[] members) : ValidatableTypeInfo(type, members)
    {
    }
}
