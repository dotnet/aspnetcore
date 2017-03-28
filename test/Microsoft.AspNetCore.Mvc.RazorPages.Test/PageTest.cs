// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public class PageTest
    {
        [Fact]
        public void PagePropertiesArePopulatedFromContext()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
            var modelMetadataProvider = new EmptyModelMetadataProvider();
            var viewDataDictionary = new ViewDataDictionary(modelMetadataProvider, modelState);
            var tempData = Mock.Of<ITempDataDictionary>();
            var pageContext = new PageContext(actionContext, viewDataDictionary, tempData, new HtmlHelperOptions());

            var page = new TestPage
            {
                PageContext = pageContext,
            };

            // Act & Assert
            Assert.Same(pageContext, page.ViewContext);
            Assert.Same(httpContext, page.HttpContext);
            Assert.Same(httpContext.Request, page.Request);
            Assert.Same(httpContext.Response, page.Response);
            Assert.Same(modelState, page.ModelState);
            Assert.Same(tempData, page.TempData);
        }

        private class TestPage : Page
        {
            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }
    }
}
