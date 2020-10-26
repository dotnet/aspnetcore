// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public class HtmlHelperComponentExtensionsTest
    {
        [Fact]
        public async Task RenderComponentAsync_Works()
        {
            // Arrange
            var viewContext = GetViewContext();
            var htmlHelper = Mock.Of<IHtmlHelper>(h => h.ViewContext == viewContext);

            // Act
            var result = await HtmlHelperComponentExtensions.RenderComponentAsync<TestComponent>(htmlHelper, RenderMode.Static);

            // Assert
            Assert.Equal("Hello world", HtmlContentUtilities.HtmlContentToString(result));
        }

        private static ViewContext GetViewContext()
        {
            var htmlContent = new HtmlContentBuilder().AppendHtml("Hello world");
            var renderer = Mock.Of<IComponentRenderer>(c =>
                c.RenderComponentAsync(It.IsAny<ViewContext>(), It.IsAny<Type>(), It.IsAny<RenderMode>(), It.IsAny<object>()) == Task.FromResult<IHtmlContent>(htmlContent));

            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceCollection().AddSingleton<IComponentRenderer>(renderer).BuildServiceProvider(),
            };

            var viewContext = new ViewContext { HttpContext = httpContext };
            return viewContext;
        }

        private class TestComponent : IComponent
        {
            public void Attach(RenderHandle renderHandle)
            {
            }

            public Task SetParametersAsync(ParameterView parameters) => null;
        }
    }
}
