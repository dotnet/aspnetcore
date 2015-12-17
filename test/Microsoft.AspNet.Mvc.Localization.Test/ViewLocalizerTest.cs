// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.PlatformAbstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Localization.Test
{
    public class ViewLocalizerTest
    {
        [Fact]
        public void ViewLocalizer_UseIndexer_ReturnsLocalizedHtmlString()
        {
            // Arrange
            var applicationEnvironment = new Mock<IApplicationEnvironment>();
            applicationEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");

            var localizedString = new LocalizedHtmlString("Hello", "Bonjour");

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
        public void ViewLocalizer_UseIndexerWithArguments_ReturnsLocalizedHtmlString()
        {
            // Arrange
            var applicationEnvironment = new Mock<IApplicationEnvironment>();
            applicationEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");

            var localizedString = new LocalizedHtmlString("Hello", "Bonjour test");

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

        [Fact]
        public void ViewLocalizer_GetAllStrings_ReturnsLocalizedString()
        {
            // Arrange
            var stringLocalizer = new TestStringLocalizer();
            var htmlLocalizer = new HtmlLocalizer(stringLocalizer);
            var applicationEnvironment = new Mock<IApplicationEnvironment>();
            applicationEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");
            var viewLocalizer = new ViewLocalizer(new TestHtmlLocalizerFactory(), applicationEnvironment.Object);

            var view = new Mock<IView>();
            view.Setup(v => v.Path).Returns("example");
            var viewContext = new ViewContext();
            viewContext.View = view.Object;

            viewLocalizer.Contextualize(viewContext);

            // Act
            var allLocalizedStrings = viewLocalizer.GetAllStrings(includeParentCultures: false).ToList();

            // Assert
            Assert.Equal(1, allLocalizedStrings.Count);
            Assert.Equal("World", allLocalizedStrings.First().Value);
        }

        [Fact]
        public void ViewLocalizer_GetAllStringsIncludeParentCulture_ReturnsLocalizedString()
        {
            // Arrange
            var stringLocalizer = new TestStringLocalizer();
            var htmlLocalizer = new HtmlLocalizer(stringLocalizer);
            var applicationEnvironment = new Mock<IApplicationEnvironment>();
            applicationEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");
            var viewLocalizer = new ViewLocalizer(new TestHtmlLocalizerFactory(), applicationEnvironment.Object);

            var view = new Mock<IView>();
            view.Setup(v => v.Path).Returns("example");
            var viewContext = new ViewContext();
            viewContext.View = view.Object;

            viewLocalizer.Contextualize(viewContext);

            // Act
            var allLocalizedStrings = viewLocalizer.GetAllStrings(includeParentCultures: true).ToList();

            // Assert
            Assert.Equal(2, allLocalizedStrings.Count);
            Assert.Equal("World", allLocalizedStrings[0].Value);
            Assert.Equal("Bar", allLocalizedStrings[1].Value);
        }

        [Fact]
        public void ViewLocalizer_GetString_ReturnsLocalizedString()
        {
            // Arrange
            var stringLocalizer = new TestStringLocalizer();
            var htmlLocalizer = new HtmlLocalizer(stringLocalizer);
            var applicationEnvironment = new Mock<IApplicationEnvironment>();
            applicationEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");
            var viewLocalizer = new ViewLocalizer(new TestHtmlLocalizerFactory(), applicationEnvironment.Object);

            var view = new Mock<IView>();
            view.Setup(v => v.Path).Returns("example");
            var viewContext = new ViewContext();
            viewContext.View = view.Object;

            viewLocalizer.Contextualize(viewContext);

            // Act
            var actualLocalizedString = viewLocalizer.GetString("John");

            // Assert
            Assert.Equal("Hello John", actualLocalizedString.Value);
        }

        [Fact]
        public void ViewLocalizer_GetStringWithArguments_ReturnsLocalizedString()
        {
            // Arrange
            var stringLocalizer = new TestStringLocalizer();
            var htmlLocalizer = new HtmlLocalizer(stringLocalizer);
            var applicationEnvironment = new Mock<IApplicationEnvironment>();
            applicationEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");
            var viewLocalizer = new ViewLocalizer(new TestHtmlLocalizerFactory(), applicationEnvironment.Object);

            var view = new Mock<IView>();
            view.Setup(v => v.Path).Returns("example");
            var viewContext = new ViewContext();
            viewContext.View = view.Object;

            viewLocalizer.Contextualize(viewContext);

            // Act
            var actualLocalizedString = viewLocalizer.GetString("John", "Doe");

            // Assert
            Assert.Equal("Hello John Doe", actualLocalizedString.Value);
        }

        [Fact]
        public void ViewLocalizer_Html_ReturnsLocalizedHtmlString()
        {
            // Arrange
            var stringLocalizer = new TestStringLocalizer();
            var htmlLocalizer = new HtmlLocalizer(stringLocalizer);
            var applicationEnvironment = new Mock<IApplicationEnvironment>();
            applicationEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");
            var viewLocalizer = new ViewLocalizer(new TestHtmlLocalizerFactory(), applicationEnvironment.Object);

            var view = new Mock<IView>();
            view.Setup(v => v.Path).Returns("example");
            var viewContext = new ViewContext();
            viewContext.View = view.Object;

            viewLocalizer.Contextualize(viewContext);

            // Act
            var actualLocalizedString = viewLocalizer.GetHtml("John");

            // Assert
            Assert.Equal("Hello John", actualLocalizedString.Value);
        }

        [Fact]
        public void ViewLocalizer_HtmlWithArguments_ReturnsLocalizedHtmlString()
        {
            // Arrange
            var stringLocalizer = new TestStringLocalizer();
            var htmlLocalizer = new HtmlLocalizer(stringLocalizer);
            var applicationEnvironment = new Mock<IApplicationEnvironment>();
            applicationEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");
            var viewLocalizer = new ViewLocalizer(new TestHtmlLocalizerFactory(), applicationEnvironment.Object);

            var view = new Mock<IView>();
            view.Setup(v => v.Path).Returns("example");
            var viewContext = new ViewContext();
            viewContext.View = view.Object;

            viewLocalizer.Contextualize(viewContext);

            // Act
            var actualLocalizedString = viewLocalizer.GetHtml("John", "Doe");

            // Assert
            Assert.Equal("Hello John Doe", actualLocalizedString.Value);
        }

        [Fact]
        public void ViewLocalizer_WithCulture_ReturnsLocalizedHtmlString()
        {
            // Arrange
            var stringLocalizer = new TestStringLocalizer();
            var htmlLocalizer = new HtmlLocalizer(stringLocalizer);
            var applicationEnvironment = new Mock<IApplicationEnvironment>();
            applicationEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");
            var viewLocalizer = new ViewLocalizer(new TestHtmlLocalizerFactory(), applicationEnvironment.Object);

            var view = new Mock<IView>();
            view.Setup(v => v.Path).Returns("example");
            var viewContext = new ViewContext();
            viewContext.View = view.Object;

            viewLocalizer.Contextualize(viewContext);

            // Act
            var actualLocalizedString = viewLocalizer.WithCulture(new CultureInfo("fr"))["John"];

            // Assert
            Assert.Equal("Bonjour John", actualLocalizedString.Value);
        }

        private class TestHtmlLocalizer : IHtmlLocalizer
        {
            private IStringLocalizer _stringLocalizer { get; set; }

            public TestHtmlLocalizer(IStringLocalizer stringLocalizer)
            {
                _stringLocalizer = stringLocalizer;
            }

            public LocalizedHtmlString this[string name]
            {
                get
                {
                    var localizedString = _stringLocalizer.GetString(name);
                    return new LocalizedHtmlString(
                        localizedString.Name,
                        localizedString.Value,
                        isResourceNotFound: false);
                }
            }

            public LocalizedHtmlString this[string name, params object[] arguments]
            {
                get
                {
                    var localizedString = _stringLocalizer.GetString(name, arguments);
                    return new LocalizedHtmlString(
                        localizedString.Name,
                        localizedString.Value,
                        isResourceNotFound: false,
                        arguments: arguments);
                }
            }

            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            {
                return _stringLocalizer.GetAllStrings(includeParentCultures);
            }

            public IHtmlLocalizer WithCulture(CultureInfo culture)
            {
                return new TestHtmlLocalizer(new TestStringLocalizer(culture));
            }

            public LocalizedString GetString(string name)
            {
                return _stringLocalizer.GetString(name);
            }

            public LocalizedString GetString(string name, params object[] arguments)
            {
                return _stringLocalizer.GetString(name, arguments);
            }
        }

        private class TestHtmlLocalizerFactory : IHtmlLocalizerFactory
        {
            public IHtmlLocalizer Create(Type resourceSource)
            {
                return new TestHtmlLocalizer(new TestStringLocalizer());
            }

            public IHtmlLocalizer Create(string baseName, string location)
            {
                return new TestHtmlLocalizer(new TestStringLocalizer());
            }
        }
    }
}
