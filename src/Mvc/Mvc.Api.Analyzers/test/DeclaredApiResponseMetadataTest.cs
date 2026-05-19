// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers;

public class DeclaredApiResponseMetadataTest
{
    private readonly IReturnOperation ReturnExpression = Mock.Of<IReturnOperation>();
    private readonly AttributeData AttributeData = new TestAttributeData();

    [Fact]
    public void Matches_ReturnsTrue_IfDeclaredMetadataIsImplicit_AndActualMetadataIsDefaultResponse()
    {
        // Arrange
        var declaredMetadata = DeclaredApiResponseMetadata.ImplicitResponse;
        var actualMetadata = new ActualApiResponseMetadata(ReturnExpression, null);

        // Act
        var matches = declaredMetadata.Matches(actualMetadata);

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void Matches_ReturnsTrue_IfDeclaredMetadataIsImplicit_AndActualMetadataReturns200()
    {
        // Arrange
        var declaredMetadata = DeclaredApiResponseMetadata.ImplicitResponse;
        var actualMetadata = new ActualApiResponseMetadata(ReturnExpression, 200, null);

        // Act
        var matches = declaredMetadata.Matches(actualMetadata);

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void Matches_ReturnsTrue_IfDeclaredMetadataIs200_AndActualMetadataIsDefaultResponse()
    {
        // Arrange
        var declaredMetadata = DeclaredApiResponseMetadata.ForProducesResponseType(200, AttributeData, Mock.Of<IMethodSymbol>());
        var actualMetadata = new ActualApiResponseMetadata(ReturnExpression, null);

        // Act
        var matches = declaredMetadata.Matches(actualMetadata);

        // Assert
        Assert.True(matches);
    }

    /// <example>
    /// [ProducesResponseType(201)]
    /// public IActionResult SomeAction => new Model();
    /// </example>
    [Fact]
    public void Matches_ReturnsTrue_IfDeclaredMetadataIs201_AndActualMetadataIsDefault()
    {
        // Arrange
        var declaredMetadata = DeclaredApiResponseMetadata.ForProducesResponseType(201, AttributeData, Mock.Of<IMethodSymbol>());
        var actualMetadata = new ActualApiResponseMetadata(ReturnExpression, null);

        // Act
        var matches = declaredMetadata.Matches(actualMetadata);

        // Assert
        Assert.True(matches);
    }

    /// <example>
    /// [ProducesResponseType(201)]
    /// public IActionResult SomeAction => Ok(new Model());
    /// </example>
    [Fact]
    public void Matches_ReturnsFalse_IfDeclaredMetadataIs201_AndActualMetadataIs200()
    {
        // Arrange
        var declaredMetadata = DeclaredApiResponseMetadata.ForProducesResponseType(201, AttributeData, Mock.Of<IMethodSymbol>());
        var actualMetadata = new ActualApiResponseMetadata(ReturnExpression, 200, null);

        // Act
        var matches = declaredMetadata.Matches(actualMetadata);

        // Assert
        Assert.False(matches);
    }

    [Fact]
    public void Matches_ReturnsTrue_IfDeclaredMetadataAndActualMetadataHaveSameStatusCode()
    {
        // Arrange
        var declaredMetadata = DeclaredApiResponseMetadata.ForProducesResponseType(302, AttributeData, Mock.Of<IMethodSymbol>());
        var actualMetadata = new ActualApiResponseMetadata(ReturnExpression, 302, null);

        // Act
        var matches = declaredMetadata.Matches(actualMetadata);

        // Assert
        Assert.True(matches);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(409)]
    [InlineData(500)]
    public void Matches_ReturnsTrue_IfDeclaredMetadataIsDefault_AndActualMetadataIsErrorStatusCode(int actualStatusCode)
    {
        // Arrange
        var declaredMetadata = DeclaredApiResponseMetadata.ForProducesDefaultResponse(AttributeData, Mock.Of<IMethodSymbol>());
        var actualMetadata = new ActualApiResponseMetadata(ReturnExpression, actualStatusCode, null);

        // Act
        var matches = declaredMetadata.Matches(actualMetadata);

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void Matches_ReturnsFalse_IfDeclaredMetadataIsDefault_AndActualMetadataIsNotErrorStatusCode()
    {
        // Arrange
        var declaredMetadata = DeclaredApiResponseMetadata.ForProducesDefaultResponse(AttributeData, Mock.Of<IMethodSymbol>());
        var actualMetadata = new ActualApiResponseMetadata(ReturnExpression, 204, null);

        // Act
        var matches = declaredMetadata.Matches(actualMetadata);

        // Assert
        Assert.False(matches);
    }

    [Fact]
    public void Matches_ReturnsFalse_IfDeclaredMetadataIsDefault_AndActualMetadataIsDefaultResponse()
    {
        // Arrange
        var declaredMetadata = DeclaredApiResponseMetadata.ForProducesDefaultResponse(AttributeData, Mock.Of<IMethodSymbol>());
        var actualMetadata = new ActualApiResponseMetadata(ReturnExpression, null);

        // Act
        var matches = declaredMetadata.Matches(actualMetadata);

        // Assert
        Assert.False(matches);
    }

    private class TestAttributeData : AttributeData
    {
        protected override INamedTypeSymbol CommonAttributeClass => throw new System.NotImplementedException();

        protected override IMethodSymbol CommonAttributeConstructor => throw new System.NotImplementedException();

        protected override SyntaxReference CommonApplicationSyntaxReference => throw new System.NotImplementedException();

        protected override ImmutableArray<TypedConstant> CommonConstructorArguments => throw new System.NotImplementedException();

        protected override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments => throw new System.NotImplementedException();
    }
}
