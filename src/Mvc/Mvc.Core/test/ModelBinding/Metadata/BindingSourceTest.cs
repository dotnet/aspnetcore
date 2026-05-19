// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class BindingSourceTest
{
    [Fact]
    public void BindingSource_CanAcceptDataFrom_ThrowsOnComposite()
    {
        // Arrange
        var expected = "The provided binding source 'Test Source' is a composite. " +
            $"'{nameof(BindingSource.CanAcceptDataFrom)}' requires that the source must represent a single type of input.";

        var bindingSource = CompositeBindingSource.Create(
            bindingSources: new BindingSource[] { BindingSource.Query, BindingSource.Form },
            displayName: "Test Source");

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => BindingSource.Query.CanAcceptDataFrom(bindingSource),
            "bindingSource",
            expected);
    }

    public static TheoryData<BindingSource> ModelBinding_MatchData
    {
        get
        {
            return new TheoryData<BindingSource>
                {
                    BindingSource.Form,
                    BindingSource.ModelBinding,
                    BindingSource.Path,
                    BindingSource.Query,
                };
        }
    }

    [Theory]
    [MemberData(nameof(ModelBinding_MatchData))]
    public void ModelBinding_CanAcceptDataFrom_Match(BindingSource bindingSource)
    {
        // Act
        var result = BindingSource.ModelBinding.CanAcceptDataFrom(bindingSource);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void BindingSource_CanAcceptDataFrom_Match()
    {
        // Act
        var result = BindingSource.Query.CanAcceptDataFrom(BindingSource.Query);

        // Assert
        Assert.True(result);
    }

    public static TheoryData<BindingSource> ModelBinding_NoMatchData
    {
        get
        {
            return new TheoryData<BindingSource>
                {
                    BindingSource.Body,
                    BindingSource.Custom,
                    BindingSource.FormFile,
                    BindingSource.Header,
                    BindingSource.Services,
                    BindingSource.Special,
                };
        }
    }

    [Theory]
    [MemberData(nameof(ModelBinding_NoMatchData))]
    public void ModelBinding_CanAcceptDataFrom_NoMatch(BindingSource bindingSource)
    {
        // Act
        var result = BindingSource.ModelBinding.CanAcceptDataFrom(bindingSource);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void BindingSource_CanAcceptDataFrom_NoMatch()
    {
        // Act
        var result = BindingSource.Query.CanAcceptDataFrom(BindingSource.Path);

        // Assert
        Assert.False(result);
    }
}
