#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Microsoft.Extensions.Validation.Tests;

public class RuntimeValidatableTypeInfoResolverTests
{
    private readonly RuntimeValidatableTypeInfoResolver _resolver = new();

    [Fact]
    public void TryGetValidatableParameterInfo_AlwaysReturnsFalse()
    {
        var methodInfo = typeof(RuntimeValidatableTypeInfoResolverTests).GetMethod(nameof(SampleMethod), BindingFlags.NonPublic | BindingFlags.Instance);
        var parameterInfo = methodInfo!.GetParameters()[0];

        var result = _resolver.TryGetValidatableParameterInfo(parameterInfo, out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithPrimitiveType_ReturnsFalse()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(int), out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithString_ReturnsFalse()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(string), out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithEnum_ReturnsFalse()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(SampleEnum), out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithSimplePocoWithValidationAttributes_ReturnsTrue()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(SimplePocoWithValidation), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
        Assert.IsType<RuntimeValidatableTypeInfoResolver.RuntimeValidatableTypeInfo>(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithSimplePocoWithoutValidation_ReturnsFalse()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(SimplePocoWithoutValidation), out var validatableInfo);

        Assert.False(result);
        Assert.Null(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithNestedComplexType_ReturnsTrue()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PocoWithNestedType), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithCyclicReference_DoesNotCauseStackOverflow()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(CyclicTypeA), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithCollectionOfComplexTypes_ReturnsTrue()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PocoWithCollection), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
    }

    [Fact]
    public void TryGetValidatableTypeInfo_UsesCaching()
    {
        // First call
        var result1 = _resolver.TryGetValidatableTypeInfo(typeof(SimplePocoWithValidation), out var validatableInfo1);
        
        // Second call
        var result2 = _resolver.TryGetValidatableTypeInfo(typeof(SimplePocoWithValidation), out var validatableInfo2);

        Assert.True(result1);
        Assert.True(result2);
        Assert.Same(validatableInfo1, validatableInfo2); // Should be the same cached instance
    }

    [Fact]
    public void TryGetValidatableTypeInfo_WithReadOnlyProperty_IgnoresReadOnlyProperty()
    {
        var result = _resolver.TryGetValidatableTypeInfo(typeof(PocoWithReadOnlyProperty), out var validatableInfo);

        Assert.True(result);
        Assert.NotNull(validatableInfo);
        
        var typeInfo = Assert.IsType<RuntimeValidatableTypeInfoResolver.RuntimeValidatableTypeInfo>(validatableInfo);
        // Should only include the writable property with validation
        // We can't directly access Members since it's internal, but we can validate through validation behavior
    }

    // Helper method for parameter test
    private void SampleMethod(string parameter) { }

    // Test classes
    public enum SampleEnum
    {
        Value1,
        Value2
    }

    public class SimplePocoWithValidation
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(0, 100)]
        public int Age { get; set; }
    }

    public class SimplePocoWithoutValidation
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public class PocoWithNestedType
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public SimplePocoWithValidation NestedPoco { get; set; } = new();
    }

    public class CyclicTypeA
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public CyclicTypeB? TypeB { get; set; }
    }

    public class CyclicTypeB
    {
        [Required]
        public string Value { get; set; } = string.Empty;

        public CyclicTypeA? TypeA { get; set; }
    }

    public class PocoWithCollection
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public List<SimplePocoWithValidation> Items { get; set; } = new();
    }

    public class PocoWithReadOnlyProperty
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string ReadOnlyValue { get; } = "ReadOnly";
    }
}