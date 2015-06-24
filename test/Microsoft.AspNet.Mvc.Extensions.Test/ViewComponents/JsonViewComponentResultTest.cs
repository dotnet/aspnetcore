// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewComponents;
using Microsoft.AspNet.Routing;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class JsonViewComponentResultTest
    {
        [Fact]
        public void Execute_UsesFormatter_WithSpecifiedSerializerSettings()
        {
            // Arrange
            var view = Mock.Of<IView>();
            var buffer = new MemoryStream();
            var viewComponentContext = GetViewComponentContext(view, buffer);

            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.Formatting = Formatting.Indented;

            var result = new JsonViewComponentResult(new { foo = "abcd" }, serializerSettings);
            viewComponentContext.ViewContext.HttpContext.Response.Body = buffer;

            // Act
            result.Execute(viewComponentContext);

            // Assert
            Assert.Equal("{\r\n  \"foo\": \"abcd\"\r\n}", Encoding.UTF8.GetString(buffer.ToArray()));
        }

        private static ViewComponentContext GetViewComponentContext(IView view, Stream stream)
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var viewContext = new ViewContext(
                actionContext,
                view,
                viewData,
                null,
                TextWriter.Null,
                new HtmlHelperOptions());

            var writer = new StreamWriter(stream) { AutoFlush = true };

            var viewComponentDescriptor = new ViewComponentDescriptor()
            {
                Type = typeof(object),
            };

            var viewComponentContext = new ViewComponentContext(viewComponentDescriptor, new object[0], viewContext, writer);
            return viewComponentContext;
        }
    }
}