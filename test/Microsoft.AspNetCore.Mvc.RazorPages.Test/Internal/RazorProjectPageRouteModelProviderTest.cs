// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Razor.Language;
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
            var fileSystem = new VirtualRazorProjectFileSystem();
            fileSystem.Add(new TestRazorProjectItem("/Pages/Home.cshtml", "@page"));
            fileSystem.Add(new TestRazorProjectItem("/Pages/Test.cshtml", "Hello world"));

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
            var fileSystem = new VirtualRazorProjectFileSystem();
            fileSystem.Add(new TestRazorProjectItem("/Areas/Products/Pages/Manage/Categories.cshtml", "@page"));
            fileSystem.Add(new TestRazorProjectItem("/Areas/Products/Pages/Index.cshtml", "@page"));
            fileSystem.Add(new TestRazorProjectItem("/Areas/Products/Pages/List.cshtml", "@page \"{sortOrder?}\""));
            fileSystem.Add(new TestRazorProjectItem("/Areas/Products/Pages/_ViewStart.cshtml", "@page"));

            var optionsManager = Options.Create(new RazorPagesOptions { AllowAreas = true });
            var provider = new RazorProjectPageRouteModelProvider(fileSystem, optionsManager, NullLoggerFactory.Instance);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.RouteModels,
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
                },
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
                });
        }

        [Fact]
        public void OnProvidersExecuting_DoesNotAddPagesUnderAreas_WhenFeatureIsDisabled()
        {
            // Arrange
            var fileSystem = new VirtualRazorProjectFileSystem();
            fileSystem.Add(new TestRazorProjectItem("/Areas/Products/Pages/Manage/Categories.cshtml", "@page"));
            fileSystem.Add(new TestRazorProjectItem("/Areas/Products/Pages/Index.cshtml", "@page"));
            fileSystem.Add(new TestRazorProjectItem("/Areas/Products/Pages/List.cshtml", "@page \"{sortOrder?}\""));
            fileSystem.Add(new TestRazorProjectItem("/Areas/Products/Pages/_ViewStart.cshtml", "@page"));
            fileSystem.Add(new TestRazorProjectItem("/Pages/About.cshtml", "@page"));

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
            var fileSystem = new VirtualRazorProjectFileSystem();
            fileSystem.Add(new TestRazorProjectItem("/Areas/Products/Pages/Categories.cshtml", "@page"));
            fileSystem.Add(new TestRazorProjectItem("/About.cshtml", "@page"));
            // We shouldn't add a route for the following paths.
            fileSystem.Add(new TestRazorProjectItem("/Areas/Products/Categories.cshtml", "@page"));
            fileSystem.Add(new TestRazorProjectItem("/Areas/Home.cshtml", "@page"));

            var optionsManager = Options.Create(new RazorPagesOptions
            {
                RootDirectory = "/",
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
                    Assert.Equal("/Areas/Products/Pages/Categories.cshtml", model.RelativePath);
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
            var fileSystem = new VirtualRazorProjectFileSystem();
            fileSystem.Add(new TestRazorProjectItem("/Pages/Index.cshtml", "@page"));
            fileSystem.Add(new TestRazorProjectItem("/Pages/Test.cshtml", "Hello world"));
            fileSystem.Add(new TestRazorProjectItem("/Pages/Admin/Index.cshtml", "@page \"test\""));

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
                    Assert.Equal("/Pages/Index.cshtml", model.RelativePath);
                    Assert.Equal("/Pages/Index", model.ViewEnginePath);
                    Assert.Collection(model.Selectors,
                        selector => Assert.Equal("Pages/Index", selector.AttributeRouteModel.Template),
                        selector => Assert.Equal("Pages", selector.AttributeRouteModel.Template));
                },
                model =>
                {
                    Assert.Equal("/Pages/Admin/Index.cshtml", model.RelativePath);
                    Assert.Equal("/Pages/Admin/Index", model.ViewEnginePath);
                    Assert.Collection(model.Selectors,
                        selector => Assert.Equal("Pages/Admin/Index/test", selector.AttributeRouteModel.Template),
                        selector => Assert.Equal("Pages/Admin/test", selector.AttributeRouteModel.Template));
                });
        }

        [Fact]
        public void OnProvidersExecuting_AllowsRouteTemplateWithOverridePattern()
        {
            // Arrange
            var fileSystem = new VirtualRazorProjectFileSystem();
            fileSystem.Add(new TestRazorProjectItem("/Index.cshtml", "@page \"/custom-route\""));

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
            var fileSystem = new VirtualRazorProjectFileSystem();
            fileSystem.Add(new TestRazorProjectItem("/Pages/Home.cshtml", "@page"));
            fileSystem.Add(new TestRazorProjectItem("/Pages/_Layout.cshtml", "@page"));

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
            var fileSystem = new VirtualRazorProjectFileSystem();
            fileSystem.Add(new TestRazorProjectItem("/Pages/Index.cshtml", "@page"));
            fileSystem.Add(new TestRazorProjectItem("/Pages/_Layout.cshtml", "@page"));
            fileSystem.Add(new TestRazorProjectItem("/NotPages/Index.cshtml", "@page"));
            fileSystem.Add(new TestRazorProjectItem("/NotPages/_Layout.cshtml", "@page"));
            fileSystem.Add(new TestRazorProjectItem("/Index.cshtml", "@page"));

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
            var fileSystem = new VirtualRazorProjectFileSystem();
            fileSystem.Add(new TestRazorProjectItem("/Pages/Home.cshtml", "@page"));
            fileSystem.Add(new TestRazorProjectItem("/Pages/Test.cshtml", "@page"));

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
