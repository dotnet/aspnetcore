// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class RazorProjectPageRouteModelProviderTest
    {
        [Fact]
        public void OnProvidersExecuting_ReturnsPagesWithPageDirective()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var file1 = fileProvider.AddFile("/Pages/Home.cshtml", "@page");
            var file2 = fileProvider.AddFile("/Pages/Test.cshtml", "Hello world");

            var dir1 = fileProvider.AddDirectoryContent("/Pages", new IFileInfo[] { file1, file2 });
            fileProvider.AddDirectoryContent("/", new[] { dir1 });

            var project = new TestRazorProject(fileProvider);

            var optionsManager = Options.Create(new RazorPagesOptions());
            optionsManager.Value.RootDirectory = "/";
            var provider = new RazorProjectPageRouteModelProvider(project, optionsManager, NullLoggerFactory.Instance);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.RouteModels,
                model =>
                {
                    Assert.Equal("/Pages/Home.cshtml", model.RelativePath);
                    Assert.Equal("/Pages/Home", model.ViewEnginePath);
                    Assert.Collection(model.Selectors,
                        selector => Assert.Equal("Pages/Home", selector.AttributeRouteModel.Template));
                });
        }

        [Fact]
        public void OnProvidersExecuting_AddsMultipleSelectorsForIndexPages()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var file1 = fileProvider.AddFile("/Pages/Index.cshtml", "@page");
            var file2 = fileProvider.AddFile("/Pages/Test.cshtml", "Hello world");
            var file3 = fileProvider.AddFile("/Pages/Admin/Index.cshtml", "@page \"test\"");

            var dir2 = fileProvider.AddDirectoryContent("/Pages/Admin", new[] { file3 });
            var dir1 = fileProvider.AddDirectoryContent("/Pages", new IFileInfo[] { dir2, file1, file2 });
            fileProvider.AddDirectoryContent("/", new[] { dir1 });

            var project = new TestRazorProject(fileProvider);

            var optionsManager = Options.Create(new RazorPagesOptions());
            optionsManager.Value.RootDirectory = "/";
            var provider = new RazorProjectPageRouteModelProvider(project, optionsManager, NullLoggerFactory.Instance);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.RouteModels,
                model =>
                {
                    Assert.Equal("/Pages/Admin/Index.cshtml", model.RelativePath);
                    Assert.Equal("/Pages/Admin/Index", model.ViewEnginePath);
                    Assert.Collection(model.Selectors,
                        selector => Assert.Equal("Pages/Admin/Index/test", selector.AttributeRouteModel.Template),
                        selector => Assert.Equal("Pages/Admin/test", selector.AttributeRouteModel.Template));
                },
                model =>
                {
                    Assert.Equal("/Pages/Index.cshtml", model.RelativePath);
                    Assert.Equal("/Pages/Index", model.ViewEnginePath);
                    Assert.Collection(model.Selectors,
                        selector => Assert.Equal("Pages/Index", selector.AttributeRouteModel.Template),
                        selector => Assert.Equal("Pages", selector.AttributeRouteModel.Template));
                });
        }

        [Fact]
        public void OnProvidersExecuting_ThrowsIfRouteTemplateHasOverridePattern()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var file = fileProvider.AddFile("/Index.cshtml", "@page \"/custom-route\"");
            fileProvider.AddDirectoryContent("/", new[] { file });

            var project = new TestRazorProject(fileProvider);

            var optionsManager = Options.Create(new RazorPagesOptions());
            optionsManager.Value.RootDirectory = "/";
            var provider = new RazorProjectPageRouteModelProvider(project, optionsManager, NullLoggerFactory.Instance);
            var context = new PageRouteModelProviderContext();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));
            Assert.Equal("The route for the page at '/Index.cshtml' cannot start with / or ~/. Pages do not support overriding the file path of the page.",
                ex.Message);
        }

        [Fact]
        public void OnProvidersExecuting_SkipsPagesStartingWithUnderscore()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var dir1 = fileProvider.AddDirectoryContent("/Pages",
                new[]
                {
                    fileProvider.AddFile("/Pages/Home.cshtml", "@page"),
                    fileProvider.AddFile("/Pages/_Layout.cshtml", "@page")
                });
            fileProvider.AddDirectoryContent("/", new[] { dir1 });

            var project = new TestRazorProject(fileProvider);

            var optionsManager = Options.Create(new RazorPagesOptions());
            optionsManager.Value.RootDirectory = "/";
            var provider = new RazorProjectPageRouteModelProvider(project, optionsManager, NullLoggerFactory.Instance);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.RouteModels,
                model =>
                {
                    Assert.Equal("/Pages/Home.cshtml", model.RelativePath);
                });
        }

        [Fact]
        public void OnProvidersExecuting_DiscoversFilesUnderBasePath()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var dir1 = fileProvider.AddDirectoryContent("/Pages",
                new[]
                {
                    fileProvider.AddFile("/Pages/Index.cshtml", "@page"),
                    fileProvider.AddFile("/Pages/_Layout.cshtml", "@page")
                });
            var dir2 = fileProvider.AddDirectoryContent("/NotPages",
                new[]
                {
                    fileProvider.AddFile("/NotPages/Index.cshtml", "@page"),
                    fileProvider.AddFile("/NotPages/_Layout.cshtml", "@page")
                });
            var rootFile = fileProvider.AddFile("/Index.cshtml", "@page");
            fileProvider.AddDirectoryContent("/", new IFileInfo[] { rootFile, dir1, dir2 });

            var project = new TestRazorProject(fileProvider);

            var optionsManager = Options.Create(new RazorPagesOptions());
            optionsManager.Value.RootDirectory = "/Pages";
            var provider = new RazorProjectPageRouteModelProvider(project, optionsManager, NullLoggerFactory.Instance);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.RouteModels,
                model =>
                {
                    Assert.Equal("/Pages/Index.cshtml", model.RelativePath);
                });
        }

        [Fact]
        public void OnProvidersExecuting_DoesNotAddPageDirectivesIfItAlreadyExists()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var file1 = fileProvider.AddFile("/Pages/Home.cshtml", "@page");
            var file2 = fileProvider.AddFile("/Pages/Test.cshtml", "@page");

            var dir1 = fileProvider.AddDirectoryContent("/Pages", new IFileInfo[] { file1, file2 });
            fileProvider.AddDirectoryContent("/", new[] { dir1 });

            var project = new TestRazorProject(fileProvider);

            var optionsManager = Options.Create(new RazorPagesOptions());
            optionsManager.Value.RootDirectory = "/";
            var provider = new RazorProjectPageRouteModelProvider(project, optionsManager, NullLoggerFactory.Instance);
            var context = new PageRouteModelProviderContext();
            var pageModel = new PageRouteModel("/Pages/Test.cshtml", "/Pages/Test");
            context.RouteModels.Add(pageModel);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.RouteModels,
                model => Assert.Same(pageModel, model),
                model =>
                {
                    Assert.Equal("/Pages/Home.cshtml", model.RelativePath);
                    Assert.Equal("/Pages/Home", model.ViewEnginePath);
                    Assert.Collection(model.Selectors,
                        selector => Assert.Equal("Pages/Home", selector.AttributeRouteModel.Template));
                });
        }
    }
}
