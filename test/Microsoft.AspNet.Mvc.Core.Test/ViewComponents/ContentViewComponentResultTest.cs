// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
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
            Assert.Equal("&lt;Test /&gt;", result.EncodedContent.ToString());
            Assert.Equal("&lt;Test /&gt;", new StreamReader(buffer).ReadToEnd());
        }

        [Fact]
        public void Execute_WritesData_PreEncoded()
        {
            // Arrange
            var buffer = new MemoryStream();
            var viewComponentContext = GetViewComponentContext(Mock.Of<IView>(), buffer);

            var result = new ContentViewComponentResult(new HtmlString("<Test />"));

            // Act
            result.Execute(viewComponentContext);
            buffer.Position = 0;

            // Assert
            Assert.Equal("<Test />", result.Content);
            Assert.Equal("<Test />", new StreamReader(buffer).ReadToEnd());
        }

        private static ViewComponentContext GetViewComponentContext(IView view, Stream stream)
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var viewContext = new ViewContext(actionContext, view, viewData, null, TextWriter.Null);
            var writer = new StreamWriter(stream) { AutoFlush = true };
            var viewComponentContext = new ViewComponentContext(typeof(object).GetTypeInfo(), viewContext, writer);
            return viewComponentContext;
        }
    }
}