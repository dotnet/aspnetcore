// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewComponents;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class JsonViewComponentResultTest
    {
        [Fact]
        public void Execute_UsesSerializer_WithSpecifiedSerializerSettings()
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
            Assert.Equal(
                $"{{{Environment.NewLine}  \"foo\": \"abcd\"{Environment.NewLine}}}",
                Encoding.UTF8.GetString(buffer.ToArray()));
        }

        [Fact]
        public void Execute_UsesSerializerSettingsFromOptions_IfNotProvided()
        {
            // Arrange
            var view = Mock.Of<IView>();
            var buffer = new MemoryStream();
            var viewComponentContext = GetViewComponentContext(view, buffer);

            var result = new JsonViewComponentResult(new { foo = "abcd" });
            viewComponentContext.ViewContext.HttpContext.Response.Body = buffer;

            // Act
            result.Execute(viewComponentContext);

            // Assert
            Assert.Equal("{\"foo\":\"abcd\"}", Encoding.UTF8.GetString(buffer.ToArray()));
        }

        private static ViewComponentContext GetViewComponentContext(IView view, Stream stream)
        {
            var actionContext = new ActionContext(GetHttpContext(), new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider());
            var viewContext = new ViewContext(
                actionContext,
                view,
                viewData,
                Mock.Of<ITempDataDictionary>(),
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

        private static HttpContext GetHttpContext()
        {
            var httpContext = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddOptions();
            httpContext.RequestServices = services.BuildServiceProvider();

            return httpContext;
        }
    }
}
