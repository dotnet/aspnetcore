// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewComponents;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.WebEncoders.Testing;
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
                new TempDataDictionary(httpContext, new SessionStateTempDataProvider()),
                TextWriter.Null,
                new HtmlHelperOptions());

            var writer = new StreamWriter(stream) { AutoFlush = true };

            var viewComponentDescriptor = new ViewComponentDescriptor()
            {
                Type = typeof(object),
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
}
