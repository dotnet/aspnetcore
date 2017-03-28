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
    public class PageModelTest
    {
        [Fact]
        public void PageModelPropertiesArePopulatedFromContext()
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
            pageContext.Page = page;

            var pageModel = new TestPageModel
            {
                PageContext = pageContext,
            };

            // Act & Assert
            Assert.Same(page, pageModel.Page);
            Assert.Same(pageContext, pageModel.PageContext);
            Assert.Same(pageContext, pageModel.ViewContext);
            Assert.Same(httpContext, pageModel.HttpContext);
            Assert.Same(httpContext.Request, pageModel.Request);
            Assert.Same(httpContext.Response, pageModel.Response);
            Assert.Same(modelState, pageModel.ModelState);
            Assert.Same(viewDataDictionary, pageModel.ViewData);
            Assert.Same(tempData, pageModel.TempData);
        }

        [Fact]
        public void UrlHelperIsSet()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var urlHelper = Mock.Of<IUrlHelper>();
            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper);
            httpContext.RequestServices = new ServiceCollection()
                .AddSingleton(urlHelperFactory.Object)
                .BuildServiceProvider();
            var actionContext = new ActionContext
            {
                HttpContext = httpContext,
            };
            var pageContext = new PageContext
            {
                HttpContext = httpContext,
            };

            var pageModel = new TestPageModel
            {
                PageContext = pageContext,
            };

            // Act & Assert
            Assert.Same(urlHelper, pageModel.Url);
        }

        [Fact]
        public async Task BindModel_InvokesBindOnPageArgumentBinder()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var binder = new TestPageArgumentBinder();
            httpContext.RequestServices = new ServiceCollection()
                .AddSingleton<PageArgumentBinder>(binder)
                .BuildServiceProvider();
            var pageContext = new PageContext
            {
                HttpContext = httpContext,
            };
            var pageModel = new TestPageModel
            {
                PageContext = pageContext,
            };

            // Act
            var result = await pageModel.BindAsync<Guid>("test-name");

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Redirect_ReturnsARedirectResult()
        {
            // Arrange
            var pageModel = new TestPageModel();

            // Act
            var result = pageModel.Redirect("test-url");

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("test-url", redirectResult.Url);
        }

        [Fact]
        public void View_ReturnsPageViewResult()
        {
            // Arrange
            var page = new TestPage();
            var pageModel = new TestPageModel
            {
                PageContext = new PageContext
                {
                    Page = page,
                }
            };

            // Act
            var result = pageModel.View();

            // Assert
            var pageResult = Assert.IsType<PageViewResult>(result);
            Assert.Same(page, pageResult.Page);
        }

        private class TestPageModel : PageModel
        {
        }

        private class TestPage : Page
        {
            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class TestPageArgumentBinder : PageArgumentBinder
        {
            protected override Task<ModelBindingResult> BindAsync(PageContext context, object value, string name, Type type)
            {
                return Task.FromResult(ModelBindingResult.Success(Guid.NewGuid()));
            }
        }
    }
}
