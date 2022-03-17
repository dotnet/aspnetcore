// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Razor;

public class RazorPageCreateTagHelperTest
{
    [Fact]
    public void CreateTagHelper_CreatesProvidedTagHelperType()
    {
        // Arrange
        var instance = CreateTestRazorPage();

        // Act
        var tagHelper = instance.CreateTagHelper<NoServiceTagHelper>();

        // Assert
        Assert.NotNull(tagHelper);
    }

    [Fact]
    public void CreateTagHelper_ActivatesProvidedTagHelperType()
    {
        // Arrange
        var instance = CreateTestRazorPage();

        // Act
        var tagHelper = instance.CreateTagHelper<ServiceTagHelper>();

        // Assert
        Assert.NotNull(tagHelper.ActivatedService);
    }

    [Fact]
    public void CreateTagHelper_ProvidesTagHelperTypeWithViewContext()
    {
        // Arrange
        var instance = CreateTestRazorPage();

        // Act
        var tagHelper = instance.CreateTagHelper<ViewContextTagHelper>();

        // Assert
        Assert.NotNull(tagHelper.ViewContext);
    }

    private static TestRazorPage CreateTestRazorPage()
    {
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var modelExpressionProvider = new ModelExpressionProvider(modelMetadataProvider);
        var activator = new RazorPageActivator(
            modelMetadataProvider,
            new UrlHelperFactory(),
            Mock.Of<IJsonHelper>(),
            new DiagnosticListener("Microsoft.AspNetCore"),
            new HtmlTestEncoder(),
            modelExpressionProvider);

        var serviceProvider = new Mock<IServiceProvider>();
        var tagHelperActivator = new DefaultTagHelperActivator();
        var myService = new MyService();
        serviceProvider.Setup(mock => mock.GetService(typeof(MyService)))
                       .Returns(myService);
        serviceProvider.Setup(mock => mock.GetService(typeof(ITagHelperFactory)))
            .Returns(new DefaultTagHelperFactory(tagHelperActivator));
        serviceProvider.Setup(mock => mock.GetService(typeof(ITagHelperActivator)))
                       .Returns(tagHelperActivator);
        serviceProvider.Setup(mock => mock.GetService(It.Is<Type>(serviceType =>
            serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))))
            .Returns<Type>(serviceType =>
            {
                var enumerableType = serviceType.GetGenericArguments().First();
                return typeof(Enumerable).GetMethod("Empty").MakeGenericMethod(enumerableType).Invoke(null, null);
            });
        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(c => c.RequestServices)
                   .Returns(serviceProvider.Object);

        var actionContext = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        var viewContext = new ViewContext(
            actionContext,
            Mock.Of<IView>(),
            viewData,
            Mock.Of<ITempDataDictionary>(),
            TextWriter.Null,
            new HtmlHelperOptions());

        return new TestRazorPage
        {
            ViewContext = viewContext
        };
    }

    private class TestRazorPage : RazorPage<dynamic>
    {
        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }

    private class NoServiceTagHelper : TagHelper
    {
    }

    private class ServiceTagHelper : TagHelper
    {
        public ServiceTagHelper(MyService service)
        {
            ActivatedService = service;
        }

        [HtmlAttributeNotBound]
        public MyService ActivatedService { get; }
    }

    private class ViewContextTagHelper : TagHelper
    {
        [ViewContext]
        public ViewContext ViewContext { get; set; }
    }

    private class MyService
    {
    }
}
