// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.RazorPages.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class PageActionDescriptorProviderTest
    {
        [Fact]
        public void GetDescriptors_DoesNotAddDescriptorsForFilesWithoutDirectives()
        {
            // Arrange
            var razorProject = new Mock<RazorProject>();
            razorProject.Setup(p => p.EnumerateItems("/"))
                .Returns(new[]
                {
                    GetProjectItem("/", "/Index.cshtml", "<h1>Hello world</h1>"),
                });
            var provider = new PageActionDescriptorProvider(
                razorProject.Object,
                GetAccessor<MvcOptions>(),
                GetRazorPagesOptions());
            var context = new ActionDescriptorProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Empty(context.Results);
        }

        [Fact]
        public void GetDescriptors_AddsDescriptorsForFileWithPageDirective()
        {
            // Arrange
            var razorProject = new Mock<RazorProject>();
            razorProject.Setup(p => p.EnumerateItems("/"))
                .Returns(new[]
                {
                    GetProjectItem("/", "/Test.cshtml", $"@page{Environment.NewLine}<h1>Hello world</h1>"),
                });
            var provider = new PageActionDescriptorProvider(
                razorProject.Object,
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
            Assert.Equal("Test", descriptor.AttributeRouteInfo.Template);
        }

        [Fact]
        public void GetDescriptors_AddsDescriptorsForFileWithPageDirectiveAndRouteTemplate()
        {
            // Arrange
            var razorProject = new Mock<RazorProject>();
            razorProject.Setup(p => p.EnumerateItems("/"))
                .Returns(new[]
                {
                    GetProjectItem("/", "/Test.cshtml", $"@page \"Home\" {Environment.NewLine}<h1>Hello world</h1>"),
                });
            var provider = new PageActionDescriptorProvider(
                razorProject.Object,
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
            Assert.Equal("Test/Home", descriptor.AttributeRouteInfo.Template);
        }

        [Fact]
        public void GetDescriptors_GeneratesRouteTemplate()
        {
            // Arrange
            var razorProject = new Mock<RazorProject>(MockBehavior.Strict);
            razorProject.Setup(p => p.EnumerateItems("/"))
                .Returns(new[]
                {
                    GetProjectItem("/", "/base-path/Test.cshtml", $"@page \"Home\" {Environment.NewLine}<h1>Hello world</h1>"),
                    GetProjectItem("/", "/base-path/Index.cshtml", $"@page {Environment.NewLine}"),
                    GetProjectItem("/", "/base-path/Admin/Index.cshtml", $"@page{Environment.NewLine}"),
                    GetProjectItem("/", "/base-path/Admin/User.cshtml", $"@page{Environment.NewLine}"),
                });
            var options = GetRazorPagesOptions();

            var provider = new PageActionDescriptorProvider(
                razorProject.Object,
                GetAccessor<MvcOptions>(),
                options);
            var context = new ActionDescriptorProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.Results,
                result => Assert.Equal("base-path/Test/Home", result.AttributeRouteInfo.Template),
                result => Assert.Equal("base-path/Index", result.AttributeRouteInfo.Template),
                result => Assert.Equal("base-path", result.AttributeRouteInfo.Template),
                result => Assert.Equal("base-path/Admin/Index", result.AttributeRouteInfo.Template),
                result => Assert.Equal("base-path/Admin", result.AttributeRouteInfo.Template),
                result => Assert.Equal("base-path/Admin/User", result.AttributeRouteInfo.Template));
        }

        [Fact]
        public void GetDescriptors_UsesBasePathOption_WhenGeneratingRouteTemplate()
        {
            // Arrange
            var razorProject = new Mock<RazorProject>(MockBehavior.Strict);
            razorProject.Setup(p => p.EnumerateItems("/base-path"))
                .Returns(new[]
                {
                    GetProjectItem("/base-path", "/Test.cshtml", $"@page \"Home\" {Environment.NewLine}<h1>Hello world</h1>"),
                    GetProjectItem("/base-path", "/Index.cshtml", $"@page {Environment.NewLine}"),
                    GetProjectItem("/base-path", "/Admin/Index.cshtml", $"@page{Environment.NewLine}"),
                    GetProjectItem("/base-path", "/Admin/User.cshtml", $"@page{Environment.NewLine}"),
                });
            var options = GetRazorPagesOptions();
            options.Value.RootDirectory = "/base-path";
            var provider = new PageActionDescriptorProvider(
                razorProject.Object,
                GetAccessor<MvcOptions>(),
                options);
            var context = new ActionDescriptorProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.Results,
                result => Assert.Equal("Test/Home", result.AttributeRouteInfo.Template),
                result => Assert.Equal("Index", result.AttributeRouteInfo.Template),
                result => Assert.Equal("", result.AttributeRouteInfo.Template),
                result => Assert.Equal("Admin/Index", result.AttributeRouteInfo.Template),
                result => Assert.Equal("Admin", result.AttributeRouteInfo.Template),
                result => Assert.Equal("Admin/User", result.AttributeRouteInfo.Template));

        }

        [Theory]
        [InlineData("/Path1")]
        [InlineData("~/Path1")]
        public void GetDescriptors_ThrowsIfRouteTemplatesAreOverriden(string template)
        {
            // Arrange
            var razorProject = new Mock<RazorProject>();
            razorProject.Setup(p => p.EnumerateItems("/"))
                .Returns(new[]
                {
                    GetProjectItem("/", "/Test.cshtml", $"@page \"{template}\" {Environment.NewLine}<h1>Hello world</h1>"),
                });
            var provider = new PageActionDescriptorProvider(
                razorProject.Object,
                GetAccessor<MvcOptions>(),
                GetRazorPagesOptions());
            var context = new ActionDescriptorProviderContext();

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));
            Assert.Equal(
                "The route for the page at '/Test.cshtml' cannot start with / or ~/. " +
                "Pages do not support overriding the file path of the page.",
                ex.Message);
        }

        [Fact]
        public void GetDescriptors_WithEmptyPageDirective_MapsIndexToEmptySegment()
        {
            // Arrange
            var razorProject = new Mock<RazorProject>();
            razorProject.Setup(p => p.EnumerateItems("/"))
                .Returns(new[]
                {
                    GetProjectItem("", "/About/Index.cshtml", $"@page {Environment.NewLine}"),
                });
            var provider = new PageActionDescriptorProvider(
                razorProject.Object,
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
                    Assert.Equal("/About/Index.cshtml", descriptor.RelativePath);
                    Assert.Equal("/About/Index", descriptor.RouteValues["page"]);
                    Assert.Equal("About/Index", descriptor.AttributeRouteInfo.Template);
                },
                result =>
                {
                    var descriptor = Assert.IsType<PageActionDescriptor>(result);
                    Assert.Equal("/About/Index.cshtml", descriptor.RelativePath);
                    Assert.Equal("/About/Index", descriptor.RouteValues["page"]);
                    Assert.Equal("About", descriptor.AttributeRouteInfo.Template);
                });
        }

        [Fact]
        public void GetDescriptors_WithNonEmptyPageDirective_MapsIndexToEmptySegment()
        {
            // Arrange
            var razorProject = new Mock<RazorProject>();
            razorProject.Setup(p => p.EnumerateItems("/"))
                .Returns(new[]
                {
                    GetProjectItem("", "/Catalog/Details/Index.cshtml", $"@page \"{{id:int?}}\" {Environment.NewLine}"),
                });
            var provider = new PageActionDescriptorProvider(
                razorProject.Object,
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
                    Assert.Equal("Catalog/Details/Index/{id:int?}", descriptor.AttributeRouteInfo.Template);
                },
                result =>
                {
                    var descriptor = Assert.IsType<PageActionDescriptor>(result);
                    Assert.Equal("/Catalog/Details/Index.cshtml", descriptor.RelativePath);
                    Assert.Equal("/Catalog/Details/Index", descriptor.RouteValues["page"]);
                    Assert.Equal("Catalog/Details/{id:int?}", descriptor.AttributeRouteInfo.Template);
                });
        }

        [Fact]
        public void GetDescriptors_ImplicitFilters()
        {
            // Arrange
            var options = new MvcOptions();
            var razorProject = new Mock<RazorProject>();
            razorProject.Setup(p => p.EnumerateItems("/"))
                .Returns(new[]
                {
                    GetProjectItem("/", "/Home.cshtml", $"@page {Environment.NewLine}"),
                });
            var provider = new PageActionDescriptorProvider(
                razorProject.Object,
                GetAccessor(options),
                GetRazorPagesOptions());
            var context = new ActionDescriptorProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var result = Assert.Single(context.Results);
            var descriptor = Assert.IsType<PageActionDescriptor>(result);
            Assert.Collection(
                descriptor.FilterDescriptors,
                filterDescriptor =>
                {
                    Assert.Equal(FilterScope.Action, filterDescriptor.Scope);
                    Assert.IsType<SaveTempDataPropertyFilterFactory>(filterDescriptor.Filter);
                },
                filterDescriptor =>
                {
                    Assert.Equal(FilterScope.Action, filterDescriptor.Scope);
                    Assert.IsType<AutoValidateAntiforgeryTokenAttribute>(filterDescriptor.Filter);
                });
        }

        [Fact]
        public void GetDescriptors_AddsGlobalFilters()
        {
            // Arrange
            var filter1 = Mock.Of<IFilterMetadata>();
            var filter2 = Mock.Of<IFilterMetadata>();
            var options = new MvcOptions();
            options.Filters.Add(filter1);
            options.Filters.Add(filter2);
            var razorProject = new Mock<RazorProject>();
            razorProject.Setup(p => p.EnumerateItems("/"))
                .Returns(new[]
                {
                    GetProjectItem("/", "/Home.cshtml", $"@page {Environment.NewLine}"),
                });
            var provider = new PageActionDescriptorProvider(
                razorProject.Object,
                GetAccessor(options),
                GetRazorPagesOptions());
            var context = new ActionDescriptorProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var result = Assert.Single(context.Results);
            var descriptor = Assert.IsType<PageActionDescriptor>(result);
            Assert.Collection(
                descriptor.FilterDescriptors,
                filterDescriptor =>
                {
                    Assert.Equal(FilterScope.Global, filterDescriptor.Scope);
                    Assert.Same(filter1, filterDescriptor.Filter);
                },
                filterDescriptor =>
                {
                    Assert.Equal(FilterScope.Global, filterDescriptor.Scope);
                    Assert.Same(filter2, filterDescriptor.Filter);
                },
                filterDescriptor =>
                {
                    Assert.Equal(FilterScope.Action, filterDescriptor.Scope);
                    Assert.IsType<SaveTempDataPropertyFilterFactory>(filterDescriptor.Filter);
                },
                filterDescriptor =>
                {
                    Assert.Equal(FilterScope.Action, filterDescriptor.Scope);
                    Assert.IsType<AutoValidateAntiforgeryTokenAttribute>(filterDescriptor.Filter);
                });
        }

        [Fact]
        public void GetDescriptors_AddsFiltersAddedByConvention()
        {
            // Arrange
            var globalFilter = Mock.Of<IFilterMetadata>();
            var localFilter = Mock.Of<IFilterMetadata>();
            var options = new MvcOptions();
            options.Filters.Add(globalFilter);
            var convention = new Mock<IPageApplicationModelConvention>();
            convention.Setup(c => c.Apply(It.IsAny<PageApplicationModel>()))
                .Callback((PageApplicationModel model) =>
                {
                    model.Filters.Add(localFilter);
                });
            var razorOptions = GetRazorPagesOptions();
            razorOptions.Value.Conventions.Add(convention.Object);

            var razorProject = new Mock<RazorProject>();
            razorProject.Setup(p => p.EnumerateItems("/"))
                .Returns(new[]
                {
                    GetProjectItem("/", "/Home.cshtml", $"@page {Environment.NewLine}"),
                });
            var provider = new PageActionDescriptorProvider(
                razorProject.Object,
                GetAccessor(options),
                razorOptions);
            var context = new ActionDescriptorProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var result = Assert.Single(context.Results);
            var descriptor = Assert.IsType<PageActionDescriptor>(result);
            Assert.Collection(descriptor.FilterDescriptors,
                filterDescriptor =>
                {
                    Assert.Equal(FilterScope.Global, filterDescriptor.Scope);
                    Assert.Same(globalFilter, filterDescriptor.Filter);
                },
                filterDescriptor =>
                {
                    Assert.Equal(FilterScope.Action, filterDescriptor.Scope);
                    Assert.IsType<SaveTempDataPropertyFilterFactory>(filterDescriptor.Filter);
                },
                filterDescriptor =>
                {
                    Assert.Equal(FilterScope.Action, filterDescriptor.Scope);
                    Assert.IsType<AutoValidateAntiforgeryTokenAttribute>(filterDescriptor.Filter);
                },
                filterDescriptor =>
                {
                    Assert.Equal(FilterScope.Action, filterDescriptor.Scope);
                    Assert.Same(localFilter, filterDescriptor.Filter);
                });
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
            return new OptionsManager<RazorPagesOptions>(new[] { new RazorPagesOptionsSetup() });
        }

        private static RazorProjectItem GetProjectItem(string basePath, string path, string content)
        {
            var testFileInfo = new TestFileInfo
            {
                Content = content,
            };

            return new DefaultRazorProjectItem(testFileInfo, basePath, path);
        }
    }
}
