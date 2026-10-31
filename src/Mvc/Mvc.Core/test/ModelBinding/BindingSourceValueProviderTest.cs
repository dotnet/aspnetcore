// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class BindingSourceValueProviderTest
{
    [Fact]
    public void BindingSourceValueProvider_ThrowsOnNonGreedySource()
    {
        // Arrange
        var expected =
            "The provided binding source 'Test Source' is a greedy data source. " +
            $"'{nameof(BindingSourceValueProvider)}' does not support greedy data sources.";

        var bindingSource = new BindingSource(
            "Test",
            displayName: "Test Source",
            isGreedy: true,
            isFromRequest: true);

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => new TestableBindingSourceValueProvider(bindingSource),
            "bindingSource",
            expected);
    }

    [Fact]
    public void BindingSourceValueProvider_ThrowsOnCompositeSource()
    {
        // Arrange
        var expected = $"The provided binding source 'Test Source' is a composite. '{nameof(BindingSourceValueProvider)}' " +
            "requires that the source must represent a single type of input.";

        var bindingSource = CompositeBindingSource.Create(
            bindingSources: new BindingSource[] { BindingSource.Query, BindingSource.Form },
            displayName: "Test Source");

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => new TestableBindingSourceValueProvider(bindingSource),
            "bindingSource",
            expected);
    }

    [Fact]
    public void BindingSourceValueProvider_ReturnsNull_WithNonMatchingSource()
    {
        // Arrange
        var valueProvider = new TestableBindingSourceValueProvider(BindingSource.Query);

        // Act
        var result = valueProvider.Filter(BindingSource.Body);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void BindingSourceValueProvider_ReturnsSelf_WithMatchingSource()
    {
        // Arrange
        var valueProvider = new TestableBindingSourceValueProvider(BindingSource.Query);

        // Act
        var result = valueProvider.Filter(BindingSource.Query);

        // Assert
        Assert.Same(valueProvider, result);
    }

    private class TestableBindingSourceValueProvider : BindingSourceValueProvider
    {
        public TestableBindingSourceValueProvider(BindingSource source)
            : base(source)
        {
        }

        public override bool ContainsPrefix(string prefix)
        {
            throw new NotImplementedException();
        }

        public override ValueProviderResult GetValue(string key)
        {
            throw new NotImplementedException();
        }
    }
}
