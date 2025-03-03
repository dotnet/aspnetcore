// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Http.Tests;

public class ValidatableInfoResolverTests
{
    [Fact]
    public void GetValidatableTypeInfo_ReturnsNull_ForNonValidatableType()
    {
        // Arrange
        var resolver = new Mock<IValidatableInfoResolver>();
        resolver.Setup(r => r.GetValidatableTypeInfo(It.IsAny<Type>())).Returns((Type t) => null);

        // Act
        var result = resolver.Object.GetValidatableTypeInfo(typeof(NonValidatableType));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetValidatableTypeInfo_ReturnsTypeInfo_ForValidatableType()
    {
        // Arrange
        var mockTypeInfo = new Mock<ValidatableTypeInfo>(
            typeof(ValidatableType),
            Array.Empty<ValidatablePropertyInfo>(),
            false,
            null).Object;

        var resolver = new Mock<IValidatableInfoResolver>();
        resolver.Setup(r => r.GetValidatableTypeInfo(typeof(ValidatableType))).Returns(mockTypeInfo);

        // Act
        var result = resolver.Object.GetValidatableTypeInfo(typeof(ValidatableType));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(ValidatableType), result.Type);
    }

    [Fact]
    public void GetValidatableParameterInfo_ReturnsNull_ForNonValidatableParameter()
    {
        // Arrange
        var method = typeof(TestMethods).GetMethod(nameof(TestMethods.MethodWithNonValidatableParam))!;
        var parameter = method.GetParameters()[0];

        var resolver = new Mock<IValidatableInfoResolver>();
        resolver.Setup(r => r.GetValidatableParameterInfo(It.IsAny<ParameterInfo>())).Returns((ParameterInfo p) => null);

        // Act
        var result = resolver.Object.GetValidatableParameterInfo(parameter);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetValidatableParameterInfo_ReturnsParameterInfo_ForValidatableParameter()
    {
        // Arrange
        var method = typeof(TestMethods).GetMethod(nameof(TestMethods.MethodWithValidatableParam))!;
        var parameter = method.GetParameters()[0];

        var mockParamInfo = new Mock<ValidatableParameterInfo>(
            "model",
            "model",
            false,
            false,
            true,
            false).Object;

        var resolver = new Mock<IValidatableInfoResolver>();
        resolver.Setup(r => r.GetValidatableParameterInfo(parameter)).Returns(mockParamInfo);

        // Act
        var result = resolver.Object.GetValidatableParameterInfo(parameter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("model", result.Name);
        Assert.True(result.HasValidatableType);
    }

    [Fact]
    public void ResolversChain_ProcessesInCorrectOrder()
    {
        // Arrange
        var services = new ServiceCollection();

        var resolver1 = new Mock<IValidatableInfoResolver>();
        var resolver2 = new Mock<IValidatableInfoResolver>();
        var resolver3 = new Mock<IValidatableInfoResolver>();

        resolver1.Setup(r => r.GetValidatableTypeInfo(typeof(ValidatableType))).Returns((Type t) => null);
        resolver2.Setup(r => r.GetValidatableTypeInfo(typeof(ValidatableType))).Returns(
            new Mock<ValidatableTypeInfo>(typeof(ValidatableType), Array.Empty<ValidatablePropertyInfo>(), false, null).Object);

        services.AddSingleton(resolver1.Object);
        services.AddSingleton(resolver2.Object);
        services.AddSingleton(resolver3.Object);

        var serviceProvider = services.BuildServiceProvider();
        var resolvers = serviceProvider.GetServices<IValidatableInfoResolver>().ToList();

        // Act
        var result = resolvers.Select(r => r.GetValidatableTypeInfo(typeof(ValidatableType)))
            .FirstOrDefault(info => info != null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(typeof(ValidatableType), result.Type);
        resolver1.Verify(r => r.GetValidatableTypeInfo(typeof(ValidatableType)), Times.Once);
        resolver2.Verify(r => r.GetValidatableTypeInfo(typeof(ValidatableType)), Times.Once);
        resolver3.Verify(r => r.GetValidatableTypeInfo(typeof(ValidatableType)), Times.Never);
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
            bool isEnumerable,
            bool isNullable,
            bool isRequired,
            bool hasValidatableType,
            ValidationAttribute[] validationAttributes)
            : base(containingType, propertyType, name, displayName, isEnumerable, isNullable, isRequired, hasValidatableType)
        {
            _validationAttributes = validationAttributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    private class TestValidatableParameterInfo : ValidatableParameterInfo
    {
        private readonly ValidationAttribute[] _validationAttributes;

        public TestValidatableParameterInfo(
            string name,
            string displayName,
            bool isNullable,
            bool isRequired,
            bool hasValidatableType,
            bool isEnumerable,
            ValidationAttribute[] validationAttributes)
            : base(name, displayName, isNullable, isRequired, hasValidatableType, isEnumerable)
        {
            _validationAttributes = validationAttributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;
    }

    private class TestValidatableTypeInfo : ValidatableTypeInfo
    {
        public TestValidatableTypeInfo(
            Type type,
            ValidatablePropertyInfo[] members,
            bool implementsIValidatableObject,
            Type[]? validatableSubTypes = null)
            : base(type, members, implementsIValidatableObject, validatableSubTypes)
        {
        }
    }
}
