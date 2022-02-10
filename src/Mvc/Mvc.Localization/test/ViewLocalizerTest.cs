// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Localization;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Localization.Test;

public class ViewLocalizerTest
{
    [Theory]
    [InlineData("TestApplication", "Views/Home/Index.cshtml", "Views/Home/Index.cshtml", "TestApplication.Views.Home.Index")]
    [InlineData("TestApplication", "/Views/Home/Index.cshtml", "/Views/Home/Index.cshtml", "TestApplication.Views.Home.Index")]
    [InlineData("TestApplication", "\\Views\\Home\\Index.cshtml", "\\Views\\Home\\Index.cshtml", "TestApplication.Views.Home.Index")]
    [InlineData("TestApplication.Web", "Views/Home/Index.cshtml", "Views/Home/Index.cshtml", "TestApplication.Web.Views.Home.Index")]
    [InlineData("TestApplication", "Views/Home/Index.cshtml", "Views/Shared/_Layout.cshtml", "TestApplication.Views.Shared._Layout")]
    [InlineData("TestApplication", "Views/Home/Index.cshtml", "Views/Shared/_MyPartial.cshtml", "TestApplication.Views.Shared._MyPartial")]
    [InlineData("TestApplication", "Views/Home/Index.cshtml", "Views/Home/_HomePartial.cshtml", "TestApplication.Views.Home._HomePartial")]
    [InlineData("TestApplication", "Views/Home/Index.cshtml", null, "TestApplication.Views.Home.Index")]
    [InlineData("TestApplication", "Views/Home/Index.txt", null, "TestApplication.Views.Home.Index")]
    [InlineData("TestApplication", "Views/Home/Index.cshtml", "", "TestApplication.Views.Home.Index")]
    [InlineData("TestApplication", "Views/Home/Index.txt", "", "TestApplication.Views.Home.Index")]
    public void ViewLocalizer_LooksForCorrectResourceBaseNameLocation(string appName, string viewPath, string executingPath, string expectedBaseName)
    {
        // Arrange
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.Setup(a => a.ApplicationName).Returns(appName);
        var htmlLocalizerFactory = new Mock<IHtmlLocalizerFactory>(MockBehavior.Loose);
        var view = new Mock<IView>();
        view.Setup(v => v.Path).Returns(viewPath);
        var viewContext = new ViewContext();
        viewContext.ExecutingFilePath = executingPath;
        viewContext.View = view.Object;
        var viewLocalizer = new ViewLocalizer(htmlLocalizerFactory.Object, hostingEnvironment.Object);

        // Act
        viewLocalizer.Contextualize(viewContext);

        // Assert
        htmlLocalizerFactory.Verify(h => h.Create(
            It.Is<string>(baseName => baseName == expectedBaseName),
            It.Is<string>(location => location == appName)
        ));
    }

    [Fact]
    public void ViewLocalizer_UseIndexer_ReturnsLocalizedHtmlString()
    {
        // Arrange
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");

        var localizedString = new LocalizedHtmlString("Hello", "Bonjour");

        var htmlLocalizer = new Mock<IHtmlLocalizer>();
        htmlLocalizer.Setup(h => h["Hello"]).Returns(localizedString);

        var htmlLocalizerFactory = new Mock<IHtmlLocalizerFactory>();
        htmlLocalizerFactory.Setup(h => h.Create("TestApplication.example", "TestApplication"))
            .Returns(htmlLocalizer.Object);

        var viewLocalizer = new ViewLocalizer(htmlLocalizerFactory.Object, hostingEnvironment.Object);

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
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");

        var localizedString = new LocalizedHtmlString("Hello", "Bonjour test");

        var htmlLocalizer = new Mock<IHtmlLocalizer>();
        htmlLocalizer.Setup(h => h["Hello", "test"]).Returns(localizedString);

        var htmlLocalizerFactory = new Mock<IHtmlLocalizerFactory>();
        htmlLocalizerFactory.Setup(
            h => h.Create("TestApplication.example", "TestApplication")).Returns(htmlLocalizer.Object);

        var viewLocalizer = new ViewLocalizer(htmlLocalizerFactory.Object, hostingEnvironment.Object);

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
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");
        var viewLocalizer = new ViewLocalizer(new TestHtmlLocalizerFactory(), hostingEnvironment.Object);

        var view = new Mock<IView>();
        view.Setup(v => v.Path).Returns("example");
        var viewContext = new ViewContext();
        viewContext.View = view.Object;

        viewLocalizer.Contextualize(viewContext);

        // Act
        var allLocalizedStrings = viewLocalizer.GetAllStrings(includeParentCultures: false).ToList();

        // Assert
        Assert.Single(allLocalizedStrings);
        Assert.Equal("World", allLocalizedStrings.First().Value);
    }

    [Fact]
    public void ViewLocalizer_GetAllStringsIncludeParentCulture_ReturnsLocalizedString()
    {
        // Arrange
        var stringLocalizer = new TestStringLocalizer();
        var htmlLocalizer = new HtmlLocalizer(stringLocalizer);
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");
        var viewLocalizer = new ViewLocalizer(new TestHtmlLocalizerFactory(), hostingEnvironment.Object);

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
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");
        var viewLocalizer = new ViewLocalizer(new TestHtmlLocalizerFactory(), hostingEnvironment.Object);

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
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");
        var viewLocalizer = new ViewLocalizer(new TestHtmlLocalizerFactory(), hostingEnvironment.Object);

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
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");
        var viewLocalizer = new ViewLocalizer(new TestHtmlLocalizerFactory(), hostingEnvironment.Object);

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
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");
        var viewLocalizer = new ViewLocalizer(new TestHtmlLocalizerFactory(), hostingEnvironment.Object);

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
        var stringLocalizer = new TestStringLocalizer(new CultureInfo("fr"));
        var htmlLocalizer = new HtmlLocalizer(stringLocalizer);
        var hostingEnvironment = new Mock<IWebHostEnvironment>();
        hostingEnvironment.Setup(a => a.ApplicationName).Returns("TestApplication");
        var viewLocalizer = new ViewLocalizer(new TestHtmlLocalizerFactory(), hostingEnvironment.Object);

        var view = new Mock<IView>();
        view.Setup(v => v.Path).Returns("example");
        var viewContext = new ViewContext();
        viewContext.View = view.Object;

        viewLocalizer.Contextualize(viewContext);

        // Act
        var actualLocalizedString = htmlLocalizer["John"];

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

        [Obsolete("This method is obsolete. Use `CurrentCulture` and `CurrentUICulture` instead.")]
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
