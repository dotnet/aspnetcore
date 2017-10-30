// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class PageActionDescriptorProviderTest
    {
        [Fact]
        public void GetDescriptors_DoesNotAddDescriptorsIfNoApplicationModelsAreDiscovered()
        {
            // Arrange
            var applicationModelProvider = new TestPageRouteModelProvider();
            var provider = new PageActionDescriptorProvider(
                new[] { applicationModelProvider },
                GetAccessor<MvcOptions>(),
                GetRazorPagesOptions());
            var context = new ActionDescriptorProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Empty(context.Results);
        }

        [Fact]
        public void GetDescriptors_AddsDescriptorsForModelWithSelector()
        {
            // Arrange
            var model = new PageRouteModel("/Test.cshtml", "/Test")
            {
                Selectors =
                {
                    new SelectorModel
                    {
                        AttributeRouteModel = new AttributeRouteModel
                        {
                            Template = "/Test/{id:int?}",
                        }
                    }
                }
            };
            var applicationModelProvider = new TestPageRouteModelProvider(model);
            var provider = new PageActionDescriptorProvider(
                new[] { applicationModelProvider },
                GetAccessor<MvcOptions>(),
                GetRazorPagesOptions());
            var context = new ActionDescriptorProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var result = Assert.Single(context.Results);
            var descriptor = Assert.IsType<PageActionDescriptor>(result);
            Assert.Equal("/Test.cshtml", descriptor.RelativePath);
            Assert.Equal("/Test", descriptor.RouteValues["page"]);
            Assert.Equal("/Test/{id:int?}", descriptor.AttributeRouteInfo.Template);
        }

        [Fact]
        public void GetDescriptors_AddsActionDescriptorForEachSelector()
        {
            // Arrange
            var applicationModelProvider = new TestPageRouteModelProvider(
                new PageRouteModel("/base-path/Test.cshtml", "/base-path/Test")
                {
                    Selectors =
                    {
                        CreateSelectorModel("base-path/Test/Home")
                    }
                },
                new PageRouteModel("/base-path/Index.cshtml", "/base-path/Index")
                {
                    Selectors =
                    {
                         CreateSelectorModel("base-path/Index"),
                         CreateSelectorModel("base-path/"),
                    }
                },
                new PageRouteModel("/base-path/Admin/Index.cshtml", "/base-path/Admin/Index")
                {
                    Selectors =
                    {
                         CreateSelectorModel("base-path/Admin/Index"),
                         CreateSelectorModel("base-path/Admin"),
                    }
                },
                new PageRouteModel("/base-path/Admin/User.cshtml", "/base-path/Admin/User")
                {
                    Selectors =
                    {
                         CreateSelectorModel("base-path/Admin/User"),
                    },
                });

            var options = GetRazorPagesOptions();

            var provider = new PageActionDescriptorProvider(
                new[] { applicationModelProvider },
                GetAccessor<MvcOptions>(),
                options);
            var context = new ActionDescriptorProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.Results,
                result => Assert.Equal("base-path/Test/Home", result.AttributeRouteInfo.Template),
                result => Assert.Equal("base-path/Index", result.AttributeRouteInfo.Template),
                result => Assert.Equal("base-path/", result.AttributeRouteInfo.Template),
                result => Assert.Equal("base-path/Admin/Index", result.AttributeRouteInfo.Template),
                result => Assert.Equal("base-path/Admin", result.AttributeRouteInfo.Template),
                result => Assert.Equal("base-path/Admin/User", result.AttributeRouteInfo.Template));
        }

        private static SelectorModel CreateSelectorModel(string template)
        {
            return new SelectorModel
            {
                AttributeRouteModel = new AttributeRouteModel
                {
                    Template = template,
                }
            };
        }

        [Fact]
        public void GetDescriptors_AddsMultipleDescriptorsForPageWithMultipleSelectors()
        {
            // Arrange
            var applicationModelProvider = new TestPageRouteModelProvider(
                new PageRouteModel("/Catalog/Details/Index.cshtml", "/Catalog/Details/Index")
                {
                    Selectors =
                    {
                         CreateSelectorModel("/Catalog/Details/Index/{id:int?}"),
                         CreateSelectorModel("/Catalog/Details/{id:int?}"),
                    },
                });

            var provider = new PageActionDescriptorProvider(
                new[] { applicationModelProvider },
                GetAccessor<MvcOptions>(),
                GetRazorPagesOptions());
            var context = new ActionDescriptorProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.Results,
                result =>
                {
                    var descriptor = Assert.IsType<PageActionDescriptor>(result);
                    Assert.Equal("/Catalog/Details/Index.cshtml", descriptor.RelativePath);
                    Assert.Equal("/Catalog/Details/Index", descriptor.RouteValues["page"]);
                    Assert.Equal("/Catalog/Details/Index/{id:int?}", descriptor.AttributeRouteInfo.Template);
                },
                result =>
                {
                    var descriptor = Assert.IsType<PageActionDescriptor>(result);
                    Assert.Equal("/Catalog/Details/Index.cshtml", descriptor.RelativePath);
                    Assert.Equal("/Catalog/Details/Index", descriptor.RouteValues["page"]);
                    Assert.Equal("/Catalog/Details/{id:int?}", descriptor.AttributeRouteInfo.Template);
                });
        }

        private static PageRouteModel CreateModel()
        {
            return new PageRouteModel("/Home.cshtml", "/Home")
            {
                Selectors =
                {
                    new SelectorModel
                    {
                        AttributeRouteModel = new AttributeRouteModel
                        {
                            Template = "Home",
                        }
                    }
                }
            };
        }

        private static IOptions<TOptions> GetAccessor<TOptions>(TOptions options = null)
            where TOptions : class, new()
        {
            var accessor = new Mock<IOptions<TOptions>>();
            accessor.SetupGet(a => a.Value).Returns(options ?? new TOptions());
            return accessor.Object;
        }

        private static IOptions<RazorPagesOptions> GetRazorPagesOptions()
        {
            return Options.Create(new RazorPagesOptions());
        }

        private static RazorProjectItem GetProjectItem(string basePath, string path, string content)
        {
            var testFileInfo = new TestFileInfo
            {
                Content = content,
            };

            return new FileProviderRazorProjectItem(testFileInfo, basePath, path);
        }

        private class TestPageRouteModelProvider : IPageRouteModelProvider
        {
            private readonly PageRouteModel[] _models;

            public TestPageRouteModelProvider(params PageRouteModel[] models)
            {
                _models = models ?? Array.Empty<PageRouteModel>();
            }

            public int Order => -1000;

            public void OnProvidersExecuted(PageRouteModelProviderContext context)
            {
            }

            public void OnProvidersExecuting(PageRouteModelProviderContext context)
            {
                foreach (var model in _models)
                {
                    context.RouteModels.Add(model);
                }
            }
        }
    }
}
