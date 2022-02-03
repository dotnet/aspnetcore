// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace Microsoft.AspNetCore.Mvc.Localization.Test;

public class HtmlLocalizerOfTTest
{
    [Fact]
    public void HtmlLocalizerOfTTest_UseIndexer_ReturnsLocalizedHtmlString()
    {
        // Arrange
        var localizedString = new LocalizedHtmlString("Hello", "Bonjour");

        var htmlLocalizer = new Mock<IHtmlLocalizer>();
        htmlLocalizer.Setup(h => h["Hello"]).Returns(localizedString);

        var htmlLocalizerFactory = new Mock<IHtmlLocalizerFactory>();
        htmlLocalizerFactory.Setup(h => h.Create(typeof(TestClass)))
            .Returns(htmlLocalizer.Object);

        var htmlLocalizerOfT = new HtmlLocalizer<TestClass>(htmlLocalizerFactory.Object);

        // Act
        var actualLocalizedString = htmlLocalizerOfT["Hello"];

        // Assert
        Assert.Equal(localizedString, actualLocalizedString);
    }

    [Fact]
    public void HtmlLocalizerOfTTest_UseIndexerWithArguments_ReturnsLocalizedHtmlString()
    {
        // Arrange
        var localizedString = new LocalizedHtmlString("Hello", "Bonjour test");

        var htmlLocalizer = new Mock<IHtmlLocalizer>();
        htmlLocalizer.Setup(h => h["Hello", "test"]).Returns(localizedString);

        var htmlLocalizerFactory = new Mock<IHtmlLocalizerFactory>();
        htmlLocalizerFactory.Setup(h => h.Create(typeof(TestClass)))
            .Returns(htmlLocalizer.Object);

        var htmlLocalizerOfT = new HtmlLocalizer<TestClass>(htmlLocalizerFactory.Object);

        // Act
        var actualLocalizedString = htmlLocalizerOfT["Hello", "test"];

        // Assert
        Assert.Equal(localizedString, actualLocalizedString);
    }
}
