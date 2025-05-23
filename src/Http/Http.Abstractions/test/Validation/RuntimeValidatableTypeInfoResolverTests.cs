#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Http.Validation.Tests;

public class RuntimeValidatableTypeInfoResolverTests
{
    private readonly RuntimeValidatableTypeInfoResolver _resolver = new();

    [Fact]
    public void TryGetValidatableParameterInfo_AlwaysReturnsFalse()
    {
        var parameterInfo = typeof(RuntimeValidatableTypeInfoResolverTests).GetMethod(nameof(TestMethod))!.GetParameters()[0];

        var result = _resolver.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(DayOfWeek))] // Enum
    public void TryGetValidatableTypeInfo_WithPrimitiveTypes_ReturnsFalse(Type type)
    {
        var result = _resolver.TryGetValidatableTypeInfo(type, out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithComplexTypeWithValidationAttributes_ReturnsTrue()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PersonWithValidation), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
        Assert.IsType<RuntimeValidatableTypeInfoResolver.RuntimeValidatableTypeInfo>(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithComplexTypeWithoutValidationAttributes_ReturnsFalse()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PersonWithoutValidation), out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithNestedComplexType_ReturnsTrue()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PersonWithNestedValidation), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_CachesResults()
    {
        // Call twice with the same type
        var result1 = _resolver.TryGetValidatableTypeInfo(typeof(PersonWithValidation), out var validatableInfo1);
        var result2 = _resolver.TryGetValidatableTypeInfo(typeof(PersonWithValidation), out var validatableInfo2);

        Assert.True(result1);
        Assert.True(result2);
        Assert.Same(validatableInfo1, validatableInfo2); // Should be the same cached instance
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithCyclicReference_HandlesGracefully()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PersonWithCyclicReference), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
        // Should not throw StackOverflowException due to cycle detection
    }

    private void TestMethod(string parameter) { }

    private class PersonWithValidation
    {
        [Required]
        public string Name { get; set; } = "";

        [Range(0, 120)]
        public int Age { get; set; }
    }

    private class PersonWithoutValidation
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    private class PersonWithNestedValidation
    {
        [Required]
        public string Name { get; set; } = "";

        public AddressWithValidation Address { get; set; } = new();
    }

    private class AddressWithValidation
    {
        [Required]
        public string Street { get; set; } = "";

        [Required]
        public string City { get; set; } = "";
    }

    private class PersonWithCyclicReference
    {
        [Required]
        public string Name { get; set; } = "";

        public PersonWithCyclicReference? Friend { get; set; }
    }
}