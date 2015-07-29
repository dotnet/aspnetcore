// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.Localization;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Localization.Test
{
    public class HtmlLocalizerOfTTest
    {
        [Fact]
        public void HtmlLocalizerOfTTest_UseIndexer_ReturnsLocalizedString()
        {
            // Arrange
            var localizedString = new LocalizedString("Hello", "Bonjour");

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
        public void HtmlLocalizerOfTTest_UseIndexerWithArguments_ReturnsLocalizedString()
        {
            // Arrange
            var applicationEnvironment = new Mock<IApplicationEnvironment>();
            applicationEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");

            var localizedString = new LocalizedString("Hello", "Bonjour test");

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
}
