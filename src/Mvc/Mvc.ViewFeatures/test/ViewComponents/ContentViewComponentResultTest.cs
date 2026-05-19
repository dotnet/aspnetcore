// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class ContentViewComponentResultTest
{
    [Fact]
    public void Execute_WritesData_Encoded()
    {
        // Arrange
        var buffer = new MemoryStream();
        var result = new ContentViewComponentResult("<Test />");

        var viewComponentContext = GetViewComponentContext(Mock.Of<IView>(), buffer);

        // Act
        result.Execute(viewComponentContext);
        buffer.Position = 0;

        // Assert
        Assert.Equal("HtmlEncode[[<Test />]]", new StreamReader(buffer).ReadToEnd());
    }

    private static ViewComponentContext GetViewComponentContext(IView view, Stream stream)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
        var viewContext = new ViewContext(
            actionContext,
            view,
            viewData,
            new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
            TextWriter.Null,
            new HtmlHelperOptions());

        var writer = new StreamWriter(stream) { AutoFlush = true };

        var viewComponentDescriptor = new ViewComponentDescriptor()
        {
            TypeInfo = typeof(object).GetTypeInfo(),
        };

        var viewComponentContext = new ViewComponentContext(
            viewComponentDescriptor,
            new Dictionary<string, object>(),
            new HtmlTestEncoder(),
            viewContext,
            writer);

        return viewComponentContext;
    }
}
