// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    public class CompiledPageRouteModelProviderTest
    {
        [Fact]
        public void OnProvidersExecuting_AddsModelsForCompiledViews()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateVersion_2_0_Descriptor("/Pages/About.cshtml"),
                CreateVersion_2_0_Descriptor("/Pages/Home.cshtml", "some-prefix"),
            };

            var provider = CreateProvider(descriptors: descriptors);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                context.RouteModels,
                result =>
                {
                    Assert.Equal("/Pages/About.cshtml", result.RelativePath);
                    Assert.Equal("/About", result.ViewEnginePath);
                    Assert.Collection(
                        result.Selectors,
                        selector => Assert.Equal("About", selector.AttributeRouteModel.Template));
                    Assert.Collection(
                        result.RouteValues.OrderBy(k => k.Key),
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
                    Assert.Collection(
                        result.Selectors,
                        selector => Assert.Equal("Home/some-prefix", selector.AttributeRouteModel.Template));
                    Assert.Collection(
                        result.RouteValues.OrderBy(k => k.Key),
                        kvp =>
                        {
                            Assert.Equal("page", kvp.Key);
                            Assert.Equal("/Home", kvp.Value);
                        });
                });
        }

        [Fact] // 2.1 adds some additional metadata to the view descriptors. We want to make sure both versions work.
        public void OnProvidersExecuting_AddsModelsForCompiledViews_Version_2_1()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateVersion_2_1_Descriptor("/Pages/About.cshtml"),
                CreateVersion_2_1_Descriptor("/Pages/Home.cshtml", metadata: new[]
                {
                    new RazorCompiledItemMetadataAttribute("RouteTemplate", "some-prefix"),
                }),
            };

            var provider = CreateProvider(descriptors: descriptors);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                context.RouteModels,
                result =>
                {
                    Assert.Equal("/Pages/About.cshtml", result.RelativePath);
                    Assert.Equal("/About", result.ViewEnginePath);
                    Assert.Collection(
                        result.Selectors,
                        selector => Assert.Equal("About", selector.AttributeRouteModel.Template));
                    Assert.Collection(
                        result.RouteValues.OrderBy(k => k.Key),
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
                    Assert.Collection(
                        result.Selectors,
                        selector => Assert.Equal("Home/some-prefix", selector.AttributeRouteModel.Template));
                    Assert.Collection(
                        result.RouteValues.OrderBy(k => k.Key),
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
                CreateVersion_2_0_Descriptor("/Areas/Products/Files/About.cshtml"),
                CreateVersion_2_0_Descriptor("/Areas/Products/Pages/About.cshtml"),
                CreateVersion_2_0_Descriptor("/Areas/Products/Pages/Manage/Index.cshtml"),
                CreateVersion_2_0_Descriptor("/Areas/Products/Pages/Manage/Edit.cshtml", "{id}"),
            };

            var options = new RazorPagesOptions
            {
                // Setting this value should not affect area page lookup.
                RootDirectory = "/Files",
            };

            var provider = CreateProvider(options: options, descriptors: descriptors);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                context.RouteModels,
                result =>
                {
                    Assert.Equal("/Areas/Products/Pages/About.cshtml", result.RelativePath);
                    Assert.Equal("/About", result.ViewEnginePath);
                    Assert.Collection(
                        result.Selectors,
                        selector => Assert.Equal("Products/About", selector.AttributeRouteModel.Template));
                    Assert.Collection(
                        result.RouteValues.OrderBy(k => k.Key),
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
                    Assert.Equal("/Areas/Products/Pages/Manage/Index.cshtml", result.RelativePath);
                    Assert.Equal("/Manage/Index", result.ViewEnginePath);
                    Assert.Collection(result.Selectors,
                        selector => Assert.Equal("Products/Manage/Index", selector.AttributeRouteModel.Template),
                        selector => Assert.Equal("Products/Manage", selector.AttributeRouteModel.Template));
                    Assert.Collection(
                        result.RouteValues.OrderBy(k => k.Key),
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
                    Assert.Equal("/Areas/Products/Pages/Manage/Edit.cshtml", result.RelativePath);
                    Assert.Equal("/Manage/Edit", result.ViewEnginePath);
                    Assert.Collection(
                        result.Selectors,
                        selector => Assert.Equal("Products/Manage/Edit/{id}", selector.AttributeRouteModel.Template));
                    Assert.Collection(
                        result.RouteValues.OrderBy(k => k.Key),
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
        public void OnProvidersExecuting_DoesNotAddAreaAndNonAreaRoutesForAPage()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateVersion_2_0_Descriptor("/Areas/Accounts/Pages/Manage/Home.cshtml"),
                CreateVersion_2_0_Descriptor("/Areas/Accounts/Manage/Home.cshtml"),
                CreateVersion_2_0_Descriptor("/Areas/About.cshtml"),
                CreateVersion_2_0_Descriptor("/Contact.cshtml"),
            };

            var options = new RazorPagesOptions
            {
                RootDirectory = "/",
            };

            var provider = CreateProvider(options: options, descriptors: descriptors);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                context.RouteModels,
                result =>
                {
                    Assert.Equal("/Areas/Accounts/Pages/Manage/Home.cshtml", result.RelativePath);
                    Assert.Equal("/Manage/Home", result.ViewEnginePath);
                    Assert.Collection(
                        result.Selectors,
                        selector => Assert.Equal("Accounts/Manage/Home", selector.AttributeRouteModel.Template));
                    Assert.Collection(
                        result.RouteValues.OrderBy(k => k.Key),
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
                    Assert.Collection(
                        result.Selectors,
                        selector => Assert.Equal("Contact", selector.AttributeRouteModel.Template));
                    Assert.Collection(
                        result.RouteValues.OrderBy(k => k.Key),
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
                CreateVersion_2_0_Descriptor("/Pages/Index.cshtml"),
                CreateVersion_2_0_Descriptor("/Pages/Admin/Index.cshtml", "some-template"),
            };
            var options = new RazorPagesOptions { RootDirectory = "/" };

            var provider = CreateProvider(options: options, descriptors: descriptors);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                context.RouteModels,
                result =>
                {
                    Assert.Equal("/Pages/Index.cshtml", result.RelativePath);
                    Assert.Equal("/Pages/Index", result.ViewEnginePath);
                    Assert.Collection(
                        result.Selectors,
                        selector => Assert.Equal("Pages/Index", selector.AttributeRouteModel.Template),
                        selector => Assert.Equal("Pages", selector.AttributeRouteModel.Template));
                },
                result =>
                {
                    Assert.Equal("/Pages/Admin/Index.cshtml", result.RelativePath);
                    Assert.Equal("/Pages/Admin/Index", result.ViewEnginePath);
                    Assert.Collection(
                        result.Selectors,
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
                CreateVersion_2_0_Descriptor("/Pages/Index.cshtml"),
                CreateVersion_2_0_Descriptor("/Pages/Admin/Index.cshtml", "some-template"),
            };

            var provider = CreateProvider(descriptors: descriptors);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                context.RouteModels,
                result =>
                {
                    Assert.Equal("/Pages/Index.cshtml", result.RelativePath);
                    Assert.Equal("/Index", result.ViewEnginePath);
                    Assert.Collection(
                        result.Selectors,
                        selector => Assert.Equal("Index", selector.AttributeRouteModel.Template),
                        selector => Assert.Equal("", selector.AttributeRouteModel.Template));
                },
                result =>
                {
                    Assert.Equal("/Pages/Admin/Index.cshtml", result.RelativePath);
                    Assert.Equal("/Admin/Index", result.ViewEnginePath);
                    Assert.Collection(
                        result.Selectors,
                        selector => Assert.Equal("Admin/Index/some-template", selector.AttributeRouteModel.Template),
                        selector => Assert.Equal("Admin/some-template", selector.AttributeRouteModel.Template));
                });
        }

        [Fact]
        public void OnProvidersExecuting_AllowsRouteTemplatesWithOverridePattern()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateVersion_2_0_Descriptor("/Pages/Index.cshtml", "~/some-other-prefix"),
                CreateVersion_2_0_Descriptor("/Pages/Home.cshtml", "/some-prefix"),
            };

            var provider = CreateProvider(descriptors: descriptors);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                context.RouteModels,
                result =>
                {
                    Assert.Equal("/Pages/Index.cshtml", result.RelativePath);
                    Assert.Equal("/Index", result.ViewEnginePath);
                    Assert.Collection(
                        result.Selectors,
                        selector => Assert.Equal("some-other-prefix", selector.AttributeRouteModel.Template));
                },
                result =>
                {
                    Assert.Equal("/Pages/Home.cshtml", result.RelativePath);
                    Assert.Equal("/Home", result.ViewEnginePath);
                    Assert.Collection(
                        result.Selectors,
                        selector => Assert.Equal("some-prefix", selector.AttributeRouteModel.Template));
                });
        }

        [Fact]
        public void OnProvidersExecuting_UsesTheFirstDescriptorForEachPath()
        {
            // ViewsFeature may contain duplicate entries for the same Page - for instance when an app overloads a library's views.
            // It picks the first entry for each path. In the ordinary case, this should ensure that the app's Razor Pages are preferred
            // to a Razor Page added by a library.

            // Arrange
            var descriptors = new[]
            {
                // Page coming from the app
                CreateVersion_2_1_Descriptor("/Pages/About.cshtml"),
                CreateVersion_2_1_Descriptor("/Pages/Home.cshtml"),
                // Page coming from the app
                CreateVersion_2_1_Descriptor("/Pages/About.cshtml"),
            };

            var provider = CreateProvider(descriptors: descriptors);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                context.RouteModels,
                result =>
                {
                    Assert.Equal("/Pages/About.cshtml", result.RelativePath);
                    Assert.Equal("/About", result.ViewEnginePath);
                },
                result =>
                {
                    Assert.Equal("/Pages/Home.cshtml", result.RelativePath);
                    Assert.Equal("/Home", result.ViewEnginePath);
                });
        }

        [Fact]
        public void OnProvidersExecuting_AllowsRazorFilesWithUnderscorePrefix()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateVersion_2_1_Descriptor("/Pages/_About.cshtml"),
                CreateVersion_2_1_Descriptor("/Pages/Home.cshtml"),
            };

            var provider = CreateProvider(descriptors: descriptors);
            var context = new PageRouteModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                context.RouteModels,
                result =>
                {
                    Assert.Equal("/Pages/_About.cshtml", result.RelativePath);
                    Assert.Equal("/_About", result.ViewEnginePath);
                    Assert.Collection(
                        result.Selectors,
                        selector => Assert.Equal("_About", selector.AttributeRouteModel.Template));
                    Assert.Collection(
                        result.RouteValues.OrderBy(k => k.Key),
                        kvp =>
                        {
                            Assert.Equal("page", kvp.Key);
                            Assert.Equal("/_About", kvp.Value);
                        });
                },
                result =>
                {
                    Assert.Equal("/Pages/Home.cshtml", result.RelativePath);
                    Assert.Equal("/Home", result.ViewEnginePath);
                    Assert.Collection(
                        result.Selectors,
                        selector => Assert.Equal("Home", selector.AttributeRouteModel.Template));
                    Assert.Collection(
                        result.RouteValues.OrderBy(k => k.Key),
                        kvp =>
                        {
                            Assert.Equal("page", kvp.Key);
                            Assert.Equal("/Home", kvp.Value);
                        });
                });
        }

        [Fact]
        public void GetRouteTemplate_ReturnsPathFromRazorPageAttribute()
        {
            // Arrange
            var expected = "test";
            var descriptor = CreateVersion_2_0_Descriptor("/Pages/Home.cshtml", expected);

            // Act
            var result = CompiledPageRouteModelProvider.GetRouteTemplate(descriptor);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetRouteTemplate_ReturnsNull_IfPageAttributeDoesNotHaveTemplate()
        {
            // Arrange
            var descriptor = CreateVersion_2_0_Descriptor("/Pages/Home.cshtml", routeTemplate: null);

            // Act
            var result = CompiledPageRouteModelProvider.GetRouteTemplate(descriptor);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetRouteTemplate_ReturnsPathFromMetadataAttribute()
        {
            // Arrange
            var expected = "test";
            var descriptor = CreateVersion_2_1_Descriptor("/Pages/About.cshtml", metadata: new object[]
            {
                new RazorCompiledItemMetadataAttribute("RouteTemplate", expected),
            });

            // Act
            var result = CompiledPageRouteModelProvider.GetRouteTemplate(descriptor);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetRouteTemplate_ReturnsNull_IfAttributeDoesNotExist()
        {
            // Arrange
            var descriptor = CreateVersion_2_1_Descriptor("/Pages/About.cshtml");

            // Act
            var result = CompiledPageRouteModelProvider.GetRouteTemplate(descriptor);

            // Assert
            Assert.Null(result);
        }

        private TestCompiledPageRouteModelProvider CreateProvider(
           RazorPagesOptions options = null,
           IList<CompiledViewDescriptor> descriptors = null)
        {
            options = options ?? new RazorPagesOptions();

            var provider = new TestCompiledPageRouteModelProvider(
                new ApplicationPartManager(),
                Options.Create(options),
                NullLogger<CompiledPageRouteModelProvider>.Instance);

            provider.Descriptors.AddRange(descriptors ?? Array.Empty<CompiledViewDescriptor>());

            return provider;
        }

        private static CompiledViewDescriptor CreateVersion_2_0_Descriptor(string path, string routeTemplate = "")
        {
            return new CompiledViewDescriptor
            {
                RelativePath = path,
                ViewAttribute = new RazorPageAttribute(path, typeof(object), routeTemplate),
            };
        }

        private static CompiledViewDescriptor CreateVersion_2_1_Descriptor(
            string path,
            object[] metadata = null)
        {
            return new CompiledViewDescriptor
            {
                RelativePath = path,
                Item = new TestRazorCompiledItem(typeof(object), "mvc.1.0.razor-page", path, metadata ?? Array.Empty<object>()),
            };
        }

        private class TestCompiledPageRouteModelProvider : CompiledPageRouteModelProvider
        {
            public TestCompiledPageRouteModelProvider(
                ApplicationPartManager partManager,
                IOptions<RazorPagesOptions> options,
                ILogger<CompiledPageRouteModelProvider> logger)
                : base(partManager, options, logger)
            {
            }

            public List<CompiledViewDescriptor> Descriptors { get; } = new List<CompiledViewDescriptor>();

            protected override ViewsFeature GetViewFeature(ApplicationPartManager applicationManager)
            {
                var feature = new ViewsFeature();
                foreach (var descriptor in Descriptors)
                {
                    feature.ViewDescriptors.Add(descriptor);
                }

                return feature;
            }
        }
    }
}
