// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class JsonViewComponentResultTest
    {
        [Fact]
        public void Execute_SerializesData_UsingSpecifiedFormatter()
        {
            // Arrange
            var view = Mock.Of<IView>();
            var buffer = new MemoryStream();
            var viewComponentContext = GetViewComponentContext(view, buffer);

            var expectedFormatter = new JsonOutputFormatter();
            var result = new JsonViewComponentResult(1, expectedFormatter);

            // Act
            result.Execute(viewComponentContext);
            buffer.Position = 0;

            // Assert
            Assert.Equal(expectedFormatter, result.Formatter);
            Assert.Equal("1", new StreamReader(buffer).ReadToEnd());
        }

        [Fact]
        public void Execute_FallsbackToServices_WhenNoJsonFormatterIsProvided()
        {
            // Arrange
            var view = Mock.Of<IView>();

            var serviceProvider = new Mock<IServiceProvider>();

            serviceProvider
                .Setup(p => p.GetService(typeof(JsonOutputFormatter)))
                .Returns(new JsonOutputFormatter())
                .Verifiable();

            var buffer = new MemoryStream();

            var result = new JsonViewComponentResult(1);
            var viewComponentContext = GetViewComponentContext(view, buffer);
            viewComponentContext.ViewContext.HttpContext.RequestServices = serviceProvider.Object;

            // Act
            result.Execute(viewComponentContext);
            buffer.Position = 0;

            // Assert
            Assert.Equal("1", new StreamReader(buffer).ReadToEnd());
            serviceProvider.Verify();
        }

        [Fact]
        public void Execute_Throws_IfNoFormatterCanBeResolved()
        {
            // Arrange
            var expected = "No service for type 'Microsoft.AspNet.Mvc.JsonOutputFormatter'" +
                " has been registered.";

            var view = Mock.Of<IView>();

            var serviceProvider = new ServiceCollection().BuildServiceProvider();

            var buffer = new MemoryStream();

            var result = new JsonViewComponentResult(1);
            var viewComponentContext = GetViewComponentContext(view, buffer);
            viewComponentContext.ViewContext.HttpContext.RequestServices = serviceProvider;

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => result.Execute(viewComponentContext));

            // Assert
            Assert.Equal(expected, ex.Message);
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