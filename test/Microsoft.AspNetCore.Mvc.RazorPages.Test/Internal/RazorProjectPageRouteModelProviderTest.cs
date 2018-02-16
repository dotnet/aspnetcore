// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class RazorProjectPageRouteModelProviderTest
    {
        private readonly IHostingEnvironment _hostingEnvironment = Mock.Of<IHostingEnvironment>(e => e.ContentRootPath == "BasePath");

        [Fact]
        public void OnProvidersExecuting_ReturnsPagesWithPageDirective()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var file1 = fileProvider.AddFile("/Pages/Home.cshtml", "@page");
            var file2 = fileProvider.AddFile("/Pages/Test.cshtml", "Hello world");

            var dir1 = fileProvider.AddDirectoryContent("/Pages", new IFileInfo[] { file1, file2 });
            fileProvider.AddDirectoryContent("/", new[] { dir1 });

            var fileSystem = new TestRazorProjectFileSystem(fileProvider, _hostingEnvironment);

            var optionsManager = Options.Create(new RazorPagesOptions());
            optionsManager.Value.RootDirectory = "/";
            var provider = new RazorProjectPageRouteModelProvider(fileSystem, optionsManager, NullLoggerFactory.Instance);
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
                    Assert.Collection(model.RouteValues.OrderBy(k => k.Key),
                       kvp =>
                       {
                           Assert.Equal("page", kvp.Key);
                           Assert.Equal("/Pages/Home", kvp.Value);
                       });
                });
        }

        [Fact]
        public void OnProvidersExecuting_AddsPagesUnderAreas()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var file1 = fileProvider.AddFile("Categories.cshtml", "@page");
            var file2 = fileProvider.AddFile("Index.cshtml", "@page");
            var file3 = fileProvider.AddFile("List.cshtml", "@page \"{sortOrder?}\"");
            var file4 = fileProvider.AddFile("_ViewStart.cshtml", "@page");
            var manageDir = fileProvider.AddDirectoryContent("/Areas/Products/Pages/Manage", new[] { file1 });
            var pagesDir = fileProvider.AddDirectoryContent("/Areas/Products/Pages", new IFileInfo[] { manageDir, file2, file3, file4 });
            var productsDir = fileProvider.AddDirectoryContent("/Areas/Products", new[] { pagesDir });
            var areasDir = fileProvider.AddDirectoryContent("/Areas", new[] { productsDir });
            var rootDir = fileProvider.AddDirectoryContent("/", new[] { areasDir });

            var fileSystem = new TestRazorProjectFileSystem(fileProvider, _hostingEnvironment);

            var optionsManager = Options.Create(new RazorPagesOptions { AllowAreas = true });
            var provider = new RazorProjectPageRouteModelProvider(fileSystem, optionsManager, NullLoggerFactory.Instance);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.RouteModels,
                model =>
                {
                    Assert.Equal("/Areas/Products/Pages/Manage/Categories.cshtml", model.RelativePath);
                    Assert.Equal("/Manage/Categories", model.ViewEnginePath);
                    Assert.Collection(model.Selectors,
                        selector => Assert.Equal("Products/Manage/Categories", selector.AttributeRouteModel.Template));
                    Assert.Collection(model.RouteValues.OrderBy(k => k.Key),
                       kvp =>
                       {
                           Assert.Equal("area", kvp.Key);
                           Assert.Equal("Products", kvp.Value);
                       },
                       kvp =>
                       {
                           Assert.Equal("page", kvp.Key);
                           Assert.Equal("/Manage/Categories", kvp.Value);
                       });
                },
                model =>
                {
                    Assert.Equal("/Areas/Products/Pages/Index.cshtml", model.RelativePath);
                    Assert.Equal("/Index", model.ViewEnginePath);
                    Assert.Collection(model.Selectors,
                        selector => Assert.Equal("Products/Index", selector.AttributeRouteModel.Template),
                        selector => Assert.Equal("Products", selector.AttributeRouteModel.Template));
                    Assert.Collection(model.RouteValues.OrderBy(k => k.Key),
                      kvp =>
                      {
                          Assert.Equal("area", kvp.Key);
                          Assert.Equal("Products", kvp.Value);
                      },
                      kvp =>
                      {
                          Assert.Equal("page", kvp.Key);
                          Assert.Equal("/Index", kvp.Value);
                      });
                },
                model =>
                {
                    Assert.Equal("/Areas/Products/Pages/List.cshtml", model.RelativePath);
                    Assert.Equal("/List", model.ViewEnginePath);
                    Assert.Collection(model.Selectors,
                        selector => Assert.Equal("Products/List/{sortOrder?}", selector.AttributeRouteModel.Template));
                    Assert.Collection(model.RouteValues.OrderBy(k => k.Key),
                      kvp =>
                      {
                          Assert.Equal("area", kvp.Key);
                          Assert.Equal("Products", kvp.Value);
                      },
                      kvp =>
                      {
                          Assert.Equal("page", kvp.Key);
                          Assert.Equal("/List", kvp.Value);
                      });
                });
        }

        [Fact]
        public void OnProvidersExecuting_DoesNotAddPagesUnderAreas_WhenFeatureIsDisabled()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var file1 = fileProvider.AddFile("Categories.cshtml", "@page");
            var file2 = fileProvider.AddFile("Index.cshtml", "@page");
            var file3 = fileProvider.AddFile("List.cshtml", "@page \"{sortOrder?}\"");
            var file4 = fileProvider.AddFile("About.cshtml", "@page");
            var manageDir = fileProvider.AddDirectoryContent("/Areas/Products/Pages/Manage", new[] { file1 });
            var areaPagesDir = fileProvider.AddDirectoryContent("/Areas/Products/Pages", new IFileInfo[] { manageDir, file2, file3, });
            var productsDir = fileProvider.AddDirectoryContent("/Areas/Products", new[] { areaPagesDir });
            var areasDir = fileProvider.AddDirectoryContent("/Areas", new[] { productsDir });
            var pagesDir = fileProvider.AddDirectoryContent("/Pages", new[] { file4 });
            var rootDir = fileProvider.AddDirectoryContent("/", new[] { areasDir, pagesDir });

            var fileSystem = new TestRazorProjectFileSystem(fileProvider, _hostingEnvironment);

            var optionsManager = Options.Create(new RazorPagesOptions { AllowAreas = false });
            var provider = new RazorProjectPageRouteModelProvider(fileSystem, optionsManager, NullLoggerFactory.Instance);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.RouteModels,
                model =>
                {
                    Assert.Equal("/Pages/About.cshtml", model.RelativePath);
                    Assert.Equal("/About", model.ViewEnginePath);
                    Assert.Collection(model.Selectors,
                        selector => Assert.Equal("About", selector.AttributeRouteModel.Template));
                });
        }

        [Fact]
        public void OnProvidersExecuting_DoesNotAddAreaAndNonAreaRoutesForAPage()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var conformingFileUnderAreasDirectory = fileProvider.AddFile("Categories.cshtml", "@page");
            // We shouldn't add a route for this.
            var nonConformingFileUnderAreasDirectory = fileProvider.AddFile("Home.cshtml", "@page");
            var rootFile = fileProvider.AddFile("About.cshtml", "@page");

            var productsDir = fileProvider.AddDirectoryContent("/Areas/Products", new[] { conformingFileUnderAreasDirectory });
            var areasDir = fileProvider.AddDirectoryContent("/Areas", new IFileInfo[] { productsDir, nonConformingFileUnderAreasDirectory });
            var rootDir = fileProvider.AddDirectoryContent("/", new IFileInfo[] { areasDir, rootFile });

            var fileSystem = new TestRazorProjectFileSystem(fileProvider, _hostingEnvironment);

            var optionsManager = Options.Create(new RazorPagesOptions
            {
                RootDirectory = "/",
                AreaRootDirectory = "/Areas",
                AllowAreas = true,
            });
            var provider = new RazorProjectPageRouteModelProvider(fileSystem, optionsManager, NullLoggerFactory.Instance);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.RouteModels,
                model =>
                {
                    Assert.Equal("/Areas/Products/Categories.cshtml", model.RelativePath);
                    Assert.Equal("/Categories", model.ViewEnginePath);
                    Assert.Collection(model.Selectors,
                        selector => Assert.Equal("Products/Categories", selector.AttributeRouteModel.Template));
                    Assert.Collection(model.RouteValues.OrderBy(k => k.Key),
                      kvp =>
                      {
                          Assert.Equal("area", kvp.Key);
                          Assert.Equal("Products", kvp.Value);
                      },
                      kvp =>
                      {
                          Assert.Equal("page", kvp.Key);
                          Assert.Equal("/Categories", kvp.Value);
                      });
                },
                model =>
                {
                    Assert.Equal("/About.cshtml", model.RelativePath);
                    Assert.Equal("/About", model.ViewEnginePath);
                    Assert.Collection(model.Selectors,
                        selector => Assert.Equal("About", selector.AttributeRouteModel.Template));
                    Assert.Collection(model.RouteValues.OrderBy(k => k.Key),
                      kvp =>
                      {
                          Assert.Equal("page", kvp.Key);
                          Assert.Equal("/About", kvp.Value);
                      });
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

            var fileSystem = new TestRazorProjectFileSystem(fileProvider, _hostingEnvironment);

            var optionsManager = Options.Create(new RazorPagesOptions());
            optionsManager.Value.RootDirectory = "/";
            var provider = new RazorProjectPageRouteModelProvider(fileSystem, optionsManager, NullLoggerFactory.Instance);
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
        public void OnProvidersExecuting_AllowsRouteTemplateWithOverridePattern()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var file = fileProvider.AddFile("/Index.cshtml", "@page \"/custom-route\"");
            fileProvider.AddDirectoryContent("/", new[] { file });

            var fileSystem = new TestRazorProjectFileSystem(fileProvider, _hostingEnvironment);

            var optionsManager = Options.Create(new RazorPagesOptions());
            optionsManager.Value.RootDirectory = "/";
            var provider = new RazorProjectPageRouteModelProvider(fileSystem, optionsManager, NullLoggerFactory.Instance);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                context.RouteModels,
                model =>
                {
                    Assert.Equal("/Index.cshtml", model.RelativePath);
                    Assert.Equal("/Index", model.ViewEnginePath);
                    Assert.Collection(
                        model.Selectors,
                        selector => Assert.Equal("custom-route", selector.AttributeRouteModel.Template));
                });
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

            var fileSystem = new TestRazorProjectFileSystem(fileProvider, _hostingEnvironment);

            var optionsManager = Options.Create(new RazorPagesOptions());
            optionsManager.Value.RootDirectory = "/";
            var provider = new RazorProjectPageRouteModelProvider(fileSystem, optionsManager, NullLoggerFactory.Instance);
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

            var fileSystem = new TestRazorProjectFileSystem(fileProvider, _hostingEnvironment);

            var optionsManager = Options.Create(new RazorPagesOptions());
            optionsManager.Value.RootDirectory = "/Pages";
            var provider = new RazorProjectPageRouteModelProvider(fileSystem, optionsManager, NullLoggerFactory.Instance);
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

            var fileSystem = new TestRazorProjectFileSystem(fileProvider, _hostingEnvironment);

            var optionsManager = Options.Create(new RazorPagesOptions());
            optionsManager.Value.RootDirectory = "/";
            var provider = new RazorProjectPageRouteModelProvider(fileSystem, optionsManager, NullLoggerFactory.Instance);
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
