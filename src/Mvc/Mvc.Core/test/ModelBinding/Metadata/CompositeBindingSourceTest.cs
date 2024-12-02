// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class CompositeBindingSourceTest
{
    [Fact]
    public void CompositeBindingSourceTest_CanAcceptDataFrom_ThrowsOnComposite()
    {
        // Arrange
        var composite1 = CompositeBindingSource.Create(
            bindingSources: new BindingSource[] { BindingSource.Query, BindingSource.Form },
            displayName: "Test Source1");

        var composite2 = CompositeBindingSource.Create(
          bindingSources: new BindingSource[] { BindingSource.Query, BindingSource.Form },
          displayName: "Test Source2");

        var expected = "The provided binding source 'Test Source2' is a composite. " +
            $"'{nameof(composite1.CanAcceptDataFrom)}' requires that the source must represent a single type of input.";

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => composite1.CanAcceptDataFrom(composite2),
            "bindingSource",
            expected);
    }

    [Fact]
    public void CompositeBindingSourceTest_CanAcceptDataFrom_Match()
    {
        // Arrange
        var composite = CompositeBindingSource.Create(
            bindingSources: new BindingSource[] { BindingSource.Query, BindingSource.Form },
            displayName: "Test Source1");

        // Act
        var result = composite.CanAcceptDataFrom(BindingSource.Query);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CompositeBindingSourceTest_CanAcceptDataFrom_NoMatch()
    {
        // Arrange
        var composite = CompositeBindingSource.Create(
            bindingSources: new BindingSource[] { BindingSource.Query, BindingSource.Form },
            displayName: "Test Source1");

        // Act
        var result = composite.CanAcceptDataFrom(BindingSource.Path);

        // Assert
        Assert.False(result);
    }
}
