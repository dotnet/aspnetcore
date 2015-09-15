// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.Localization;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.WebEncoders.Testing;
using Moq;
using Xunit;
using Microsoft.AspNet.Mvc.ViewEngines;

namespace Microsoft.AspNet.Mvc.Localization.Test
{
    public class ViewLocalizerTest
    {
        [Fact]
        public void ViewLocalizer_UseIndexer_ReturnsLocalizedString()
        {
            // Arrange
            var applicationEnvironment = new Mock<IApplicationEnvironment>();
            applicationEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");

            var localizedString = new LocalizedString("Hello", "Bonjour");

            var htmlLocalizer = new Mock<IHtmlLocalizer>();
            htmlLocalizer.Setup(h => h["Hello"]).Returns(localizedString);

            var htmlLocalizerFactory = new Mock<IHtmlLocalizerFactory>();
            htmlLocalizerFactory.Setup(h => h.Create("example", "TestApplication"))
                .Returns(htmlLocalizer.Object);

            var viewLocalizer = new ViewLocalizer(htmlLocalizerFactory.Object, applicationEnvironment.Object);

            var view = new Mock<IView>();
            view.Setup(v => v.Path).Returns("example");
            var viewContext = new ViewContext();
            viewContext.View = view.Object;

            viewLocalizer.Contextualize(viewContext);

            // Act
            var actualLocalizedString = viewLocalizer["Hello"];

            // Assert
            Assert.Equal(localizedString, actualLocalizedString);
        }

        [Fact]
        public void ViewLocalizer_UseIndexerWithArguments_ReturnsLocalizedString()
        {
            // Arrange
            var applicationEnvironment = new Mock<IApplicationEnvironment>();
            applicationEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");

            var localizedString = new LocalizedString("Hello", "Bonjour test");

            var htmlLocalizer = new Mock<IHtmlLocalizer>();
            htmlLocalizer.Setup(h => h["Hello", "test"]).Returns(localizedString);

            var htmlLocalizerFactory = new Mock<IHtmlLocalizerFactory>();
            htmlLocalizerFactory.Setup(
                h => h.Create("example", "TestApplication")).Returns(htmlLocalizer.Object);

            var viewLocalizer = new ViewLocalizer(htmlLocalizerFactory.Object, applicationEnvironment.Object);

            var view = new Mock<IView>();
            view.Setup(v => v.Path).Returns("example");
            var viewContext = new ViewContext();
            viewContext.View = view.Object;

            viewLocalizer.Contextualize(viewContext);

            // Act
            var actualLocalizedString = viewLocalizer["Hello", "test"];

            // Assert
            Assert.Equal(localizedString, actualLocalizedString);
        }
    }
}
