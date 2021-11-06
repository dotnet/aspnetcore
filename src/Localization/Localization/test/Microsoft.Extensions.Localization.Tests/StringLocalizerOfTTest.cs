// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Localization;

public class StringLocalizerOfTTest
{
    [Fact]
    public void Constructor_ThrowsAnExceptionForNullFactory()
    {
        // Arrange, act and assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => new StringLocalizer<object>(factory: null!));

        Assert.Equal("factory", exception.ParamName);
    }

    [Fact]
    public void Constructor_ResolvesLocalizerFromFactory()
    {
        // Arrange
        var factory = new Mock<IStringLocalizerFactory>();

        // Act
        _ = new StringLocalizer<object>(factory.Object);

        // Assert
        factory.Verify(mock => mock.Create(typeof(object)), Times.Once());
    }

    [Fact]
    public void Indexer_ThrowsAnExceptionForNullName()
    {
        // Arrange
        var factory = new Mock<IStringLocalizerFactory>();
        var innerLocalizer = new Mock<IStringLocalizer>();
        factory.Setup(mock => mock.Create(typeof(object)))
            .Returns(innerLocalizer.Object);

        var localizer = new StringLocalizer<object>(factory.Object);

        // Act and assert
        var exception = Assert.Throws<ArgumentNullException>(() => localizer[name: null!]);

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Indexer_InvokesIndexerFromInnerLocalizer()
    {
        // Arrange
        var factory = new Mock<IStringLocalizerFactory>();
        var innerLocalizer = new Mock<IStringLocalizer>();
        factory.Setup(mock => mock.Create(typeof(object)))
            .Returns(innerLocalizer.Object);

        var localizer = new StringLocalizer<object>(factory.Object);

        // Act
        _ = localizer["Hello world"];

        // Assert
        innerLocalizer.Verify(mock => mock["Hello world"], Times.Once());
    }

    [Fact]
    public void Indexer_ThrowsAnExceptionForNullName_WithArguments()
    {
        // Arrange
        var factory = new Mock<IStringLocalizerFactory>();
        var innerLocalizer = new Mock<IStringLocalizer>();
        factory.Setup(mock => mock.Create(typeof(object)))
            .Returns(innerLocalizer.Object);

        var localizer = new StringLocalizer<object>(factory.Object);

        // Act and assert
        var exception = Assert.Throws<ArgumentNullException>(() => localizer[name: null!]);

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Indexer_InvokesIndexerFromInnerLocalizer_WithArguments()
    {
        // Arrange
        var factory = new Mock<IStringLocalizerFactory>();
        var innerLocalizer = new Mock<IStringLocalizer>();
        factory.Setup(mock => mock.Create(typeof(object)))
            .Returns(innerLocalizer.Object);

        var localizer = new StringLocalizer<object>(factory.Object);

        // Act
        _ = localizer["Welcome, {0}", "Bob"];

        // Assert
        innerLocalizer.Verify(mock => mock["Welcome, {0}", "Bob"], Times.Once());
    }

    [Fact]
    public void GetAllStrings_InvokesGetAllStringsFromInnerLocalizer()
    {
        // Arrange
        var factory = new Mock<IStringLocalizerFactory>();
        var innerLocalizer = new Mock<IStringLocalizer>();
        factory.Setup(mock => mock.Create(typeof(object)))
            .Returns(innerLocalizer.Object);

        var localizer = new StringLocalizer<object>(factory.Object);

        // Act
        localizer.GetAllStrings(includeParentCultures: true);

        // Assert
        innerLocalizer.Verify(mock => mock.GetAllStrings(true), Times.Once());
    }

    [Fact]
    public void StringLocalizer_CanBeCastToBaseType()
    {
        // Arrange and act
        IStringLocalizer<BaseType> localizer = new StringLocalizer<DerivedType>(Mock.Of<IStringLocalizerFactory>());

        // Assert
        Assert.NotNull(localizer);
    }

    private class BaseType { }
    private class DerivedType : BaseType { }
}
