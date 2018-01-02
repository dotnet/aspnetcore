// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class CompiledPageRouteModelProviderTest
    {
        [Fact]
        public void OnProvidersExecuting_AddsModelsForCompiledViews()
        {
            // Arrange
            var descriptors = new[]
            {
                GetDescriptor("/Pages/About.cshtml"),
                GetDescriptor("/Pages/Home.cshtml", "some-prefix"),
            };
            var provider = new TestCompiledPageRouteModelProvider(descriptors, new RazorPagesOptions());
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.RouteModels,
                result =>
                {
                    Assert.Equal("/Pages/About.cshtml", result.RelativePath);
                    Assert.Equal("/About", result.ViewEnginePath);
                    Assert.Collection(result.Selectors,
                        selector => Assert.Equal("About", selector.AttributeRouteModel.Template));
                    Assert.Collection(result.RouteValues.OrderBy(k => k.Key),
                       kvp =>
                       {
                           Assert.Equal("page", kvp.Key);
                           Assert.Equal("/About", kvp.Value);
                       });
                },
                result =>
                {
                    Assert.Equal("/Pages/Home.cshtml", result.RelativePath);
                    Assert.Equal("/Home", result.ViewEnginePath);
                    Assert.Collection(result.Selectors,
                        selector => Assert.Equal("Home/some-prefix", selector.AttributeRouteModel.Template));
                    Assert.Collection(result.RouteValues.OrderBy(k => k.Key),
                       kvp =>
                       {
                           Assert.Equal("page", kvp.Key);
                           Assert.Equal("/Home", kvp.Value);
                       });
                });
        }

        [Fact]
        public void OnProvidersExecuting_AddsModelsForCompiledAreaPages()
        {
            // Arrange
            var descriptors = new[]
            {
                GetDescriptor("/Features/Products/Files/About.cshtml"),
                GetDescriptor("/Features/Products/Files/Manage/Index.cshtml"),
                GetDescriptor("/Features/Products/Files/Manage/Edit.cshtml", "{id}"),
            };
            var options = new RazorPagesOptions
            {
                AllowAreas = true,
                AreaRootDirectory = "/Features",
                RootDirectory = "/Files",
            };
            var provider = new TestCompiledPageRouteModelProvider(descriptors, options);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.RouteModels,
                result =>
                {
                    Assert.Equal("/Features/Products/Files/About.cshtml", result.RelativePath);
                    Assert.Equal("/About", result.ViewEnginePath);
                    Assert.Collection(result.Selectors,
                        selector => Assert.Equal("Products/About", selector.AttributeRouteModel.Template));
                    Assert.Collection(result.RouteValues.OrderBy(k => k.Key),
                      kvp =>
                      {
                          Assert.Equal("area", kvp.Key);
                          Assert.Equal("Products", kvp.Value);
                      },
                      kvp =>
                      {
                          Assert.Equal("page", kvp.Key);
                          Assert.Equal("/About", kvp.Value);
                      });
                },
                result =>
                {
                    Assert.Equal("/Features/Products/Files/Manage/Index.cshtml", result.RelativePath);
                    Assert.Equal("/Manage/Index", result.ViewEnginePath);
                    Assert.Collection(result.Selectors,
                        selector => Assert.Equal("Products/Manage/Index", selector.AttributeRouteModel.Template),
                        selector => Assert.Equal("Products/Manage", selector.AttributeRouteModel.Template));
                    Assert.Collection(result.RouteValues.OrderBy(k => k.Key),
                      kvp =>
                      {
                          Assert.Equal("area", kvp.Key);
                          Assert.Equal("Products", kvp.Value);
                      },
                      kvp =>
                      {
                          Assert.Equal("page", kvp.Key);
                          Assert.Equal("/Manage/Index", kvp.Value);
                      });
                },
                result =>
                {
                    Assert.Equal("/Features/Products/Files/Manage/Edit.cshtml", result.RelativePath);
                    Assert.Equal("/Manage/Edit", result.ViewEnginePath);
                    Assert.Collection(result.Selectors,
                        selector => Assert.Equal("Products/Manage/Edit/{id}", selector.AttributeRouteModel.Template));
                    Assert.Collection(result.RouteValues.OrderBy(k => k.Key),
                      kvp =>
                      {
                          Assert.Equal("area", kvp.Key);
                          Assert.Equal("Products", kvp.Value);
                      },
                      kvp =>
                      {
                          Assert.Equal("page", kvp.Key);
                          Assert.Equal("/Manage/Edit", kvp.Value);
                      });
                });
        }

        [Fact]
        public void OnProvidersExecuting_DoesNotAddsModelsForAreaPages_IfFeatureIsDisabled()
        {
            // Arrange
            var descriptors = new[]
            {
                GetDescriptor("/Pages/About.cshtml"),
                GetDescriptor("/Areas/Accounts/Pages/Home.cshtml"),
            };
            var options = new RazorPagesOptions { AllowAreas = false };
            var provider = new TestCompiledPageRouteModelProvider(descriptors, options);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.RouteModels,
                result =>
                {
                    Assert.Equal("/Pages/About.cshtml", result.RelativePath);
                    Assert.Equal("/About", result.ViewEnginePath);
                    Assert.Collection(result.Selectors,
                        selector => Assert.Equal("About", selector.AttributeRouteModel.Template));
                    Assert.Collection(result.RouteValues.OrderBy(k => k.Key),
                       kvp =>
                       {
                           Assert.Equal("page", kvp.Key);
                           Assert.Equal("/About", kvp.Value);
                       });
                });
        }

        [Fact]
        public void OnProvidersExecuting_DoesNotAddAreaAndNonAreaRoutesForAPage()
        {
            // Arrange
            var descriptors = new[]
            {
                GetDescriptor("/Areas/Accounts/Manage/Home.cshtml"),
                GetDescriptor("/Areas/About.cshtml"),
                GetDescriptor("/Contact.cshtml"),
            };
            var options = new RazorPagesOptions
            {
                AllowAreas = true,
                AreaRootDirectory = "/Areas",
                RootDirectory = "/",
            };
            var provider = new TestCompiledPageRouteModelProvider(descriptors, options);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.RouteModels,
                result =>
                {
                    Assert.Equal("/Areas/Accounts/Manage/Home.cshtml", result.RelativePath);
                    Assert.Equal("/Manage/Home", result.ViewEnginePath);
                    Assert.Collection(result.Selectors,
                        selector => Assert.Equal("Accounts/Manage/Home", selector.AttributeRouteModel.Template));
                    Assert.Collection(result.RouteValues.OrderBy(k => k.Key),
                      kvp =>
                      {
                          Assert.Equal("area", kvp.Key);
                          Assert.Equal("Accounts", kvp.Value);
                      },
                      kvp =>
                      {
                          Assert.Equal("page", kvp.Key);
                          Assert.Equal("/Manage/Home", kvp.Value);
                      });
                },
                result =>
                {
                    Assert.Equal("/Contact.cshtml", result.RelativePath);
                    Assert.Equal("/Contact", result.ViewEnginePath);
                    Assert.Collection(result.Selectors,
                        selector => Assert.Equal("Contact", selector.AttributeRouteModel.Template));
                    Assert.Collection(result.RouteValues.OrderBy(k => k.Key),
                      kvp =>
                      {
                          Assert.Equal("page", kvp.Key);
                          Assert.Equal("/Contact", kvp.Value);
                      });
                });
        }

        [Fact]
        public void OnProvidersExecuting_AddsMultipleSelectorsForIndexPage_WithIndexAtRoot()
        {
            // Arrange
            var descriptors = new[]
            {
                GetDescriptor("/Pages/Index.cshtml"),
                GetDescriptor("/Pages/Admin/Index.cshtml", "some-template"),
            };
            var provider = new TestCompiledPageRouteModelProvider(descriptors, new RazorPagesOptions { RootDirectory = "/" });
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.RouteModels,
                result =>
                {
                    Assert.Equal("/Pages/Index.cshtml", result.RelativePath);
                    Assert.Equal("/Pages/Index", result.ViewEnginePath);
                    Assert.Collection(result.Selectors,
                        selector => Assert.Equal("Pages/Index", selector.AttributeRouteModel.Template),
                        selector => Assert.Equal("Pages", selector.AttributeRouteModel.Template));
                },
                result =>
                {
                    Assert.Equal("/Pages/Admin/Index.cshtml", result.RelativePath);
                    Assert.Equal("/Pages/Admin/Index", result.ViewEnginePath);
                    Assert.Collection(result.Selectors,
                        selector => Assert.Equal("Pages/Admin/Index/some-template", selector.AttributeRouteModel.Template),
                        selector => Assert.Equal("Pages/Admin/some-template", selector.AttributeRouteModel.Template));
                });
        }

        [Fact]
        public void OnProvidersExecuting_AddsMultipleSelectorsForIndexPage()
        {
            // Arrange
            var descriptors = new[]
            {
                GetDescriptor("/Pages/Index.cshtml"),
                GetDescriptor("/Pages/Admin/Index.cshtml", "some-template"),
            };
            var provider = new TestCompiledPageRouteModelProvider(descriptors, new RazorPagesOptions());
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.RouteModels,
                result =>
                {
                    Assert.Equal("/Pages/Index.cshtml", result.RelativePath);
                    Assert.Equal("/Index", result.ViewEnginePath);
                    Assert.Collection(result.Selectors,
                        selector => Assert.Equal("Index", selector.AttributeRouteModel.Template),
                        selector => Assert.Equal("", selector.AttributeRouteModel.Template));
                },
                result =>
                {
                    Assert.Equal("/Pages/Admin/Index.cshtml", result.RelativePath);
                    Assert.Equal("/Admin/Index", result.ViewEnginePath);
                    Assert.Collection(result.Selectors,
                        selector => Assert.Equal("Admin/Index/some-template", selector.AttributeRouteModel.Template),
                        selector => Assert.Equal("Admin/some-template", selector.AttributeRouteModel.Template));
                });
        }

        [Fact]
        public void OnProvidersExecuting_ThrowsIfRouteTemplateHasOverridePattern()
        {
            // Arrange
            var descriptors = new[]
            {
                GetDescriptor("/Pages/Index.cshtml"),
                GetDescriptor("/Pages/Home.cshtml", "/some-prefix"),
            };
            var provider = new TestCompiledPageRouteModelProvider(descriptors, new RazorPagesOptions());
            var context = new PageRouteModelProviderContext();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));
            Assert.Equal("The route for the page at '/Pages/Home.cshtml' cannot start with / or ~/. Pages do not support overriding the file path of the page.",
                ex.Message);
        }

        private static CompiledViewDescriptor GetDescriptor(string path, string prefix = "")
        {
            return new CompiledViewDescriptor
            {
                RelativePath = path,
                ViewAttribute = new RazorPageAttribute(path, typeof(object), prefix),
            };
        }

        public class TestCompiledPageRouteModelProvider : CompiledPageRouteModelProvider
        {
            private readonly IEnumerable<CompiledViewDescriptor> _descriptors;

            public TestCompiledPageRouteModelProvider(IEnumerable<CompiledViewDescriptor> descriptors, RazorPagesOptions options)
                : base(new ApplicationPartManager(), Options.Create(options), NullLoggerFactory.Instance)
            {
                _descriptors = descriptors;
            }

            protected override IEnumerable<CompiledViewDescriptor> GetViewDescriptors(ApplicationPartManager applicationManager) => _descriptors;
        }
    }
}
