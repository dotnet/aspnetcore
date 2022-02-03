// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

public class CompiledPageRouteModelProviderTest
{
    [Fact]
    public void OnProvidersExecuting_AddsModelsForCompiledViews()
    {
        // Arrange
        var items = new[]
        {
                TestRazorCompiledItem.CreateForPage("/Pages/About.cshtml"),
                TestRazorCompiledItem.CreateForPage("/Pages/Home.cshtml", metadata: new[]
                {
                    new RazorCompiledItemMetadataAttribute("RouteTemplate", "some-prefix"),
                }),
            };

        var provider = CreateProvider(items);
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
        var items = new[]
        {
                TestRazorCompiledItem.CreateForPage("/Areas/Products/Files/About.cshtml"),
                TestRazorCompiledItem.CreateForPage("/Areas/Products/Pages/About.cshtml"),
                TestRazorCompiledItem.CreateForPage("/Areas/Products/Pages/Manage/Index.cshtml"),
                TestRazorCompiledItem.CreateForPage("/Areas/Products/Pages/Manage/Edit.cshtml", metadata: new object[]
                {
                    new RazorCompiledItemMetadataAttribute("RouteTemplate", "{id}"),
                }),
            };

        var options = new RazorPagesOptions
        {
            // Setting this value should not affect area page lookup.
            RootDirectory = "/Files",
        };

        var provider = CreateProvider(items, options);
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
        var items = new[]
        {
                TestRazorCompiledItem.CreateForPage("/Areas/Accounts/Pages/Manage/Home.cshtml"),
                TestRazorCompiledItem.CreateForPage("/Areas/Accounts/Manage/Home.cshtml"),
                TestRazorCompiledItem.CreateForPage("/Areas/About.cshtml"),
                TestRazorCompiledItem.CreateForPage("/Contact.cshtml"),
            };

        var options = new RazorPagesOptions
        {
            RootDirectory = "/",
        };

        var provider = CreateProvider(items, options);
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
        var items = new[]
        {
                TestRazorCompiledItem.CreateForPage("/Pages/Index.cshtml"),
                TestRazorCompiledItem.CreateForPage("/Pages/Admin/Index.cshtml", metadata: new object[]
                {
                    new RazorCompiledItemMetadataAttribute("RouteTemplate", "some-template"),
                }),
            };
        var options = new RazorPagesOptions { RootDirectory = "/" };

        var provider = CreateProvider(items, options);
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
        var items = new[]
        {
                TestRazorCompiledItem.CreateForPage("/Pages/Index.cshtml"),
                TestRazorCompiledItem.CreateForPage("/Pages/Admin/Index.cshtml", metadata: new object[]
                {
                    new RazorCompiledItemMetadataAttribute("RouteTemplate", "some-template"),
                }),
            };

        var provider = CreateProvider(items);
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
        var items = new[]
        {
                TestRazorCompiledItem.CreateForPage("/Pages/Index.cshtml", metadata: new object[]
                {
                    new RazorCompiledItemMetadataAttribute("RouteTemplate", "~/some-other-prefix"),
                }),
                TestRazorCompiledItem.CreateForPage("/Pages/Home.cshtml", metadata: new object[]
                {
                    new RazorCompiledItemMetadataAttribute("RouteTemplate", "/some-prefix"),
                }),
            };

        var provider = CreateProvider(items);
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
        var items = new[]
        {
                // Page coming from the app
                TestRazorCompiledItem.CreateForPage("/Pages/About.cshtml"),
                TestRazorCompiledItem.CreateForPage("/Pages/Home.cshtml"),
                // Page coming from the app
                TestRazorCompiledItem.CreateForPage("/Pages/About.cshtml"),
            };

        var provider = CreateProvider(items);
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
        var items = new[]
        {
                TestRazorCompiledItem.CreateForPage("/Pages/_About.cshtml"),
                TestRazorCompiledItem.CreateForPage("/Pages/Home.cshtml"),
            };

        var provider = CreateProvider(items);
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
    public void GetRouteTemplate_ReturnsPathFromMetadataAttribute()
    {
        // Arrange
        var expected = "test";
        var descriptor = new CompiledViewDescriptor(TestRazorCompiledItem.CreateForPage("/Pages/About.cshtml", metadata: new object[]
        {
                new RazorCompiledItemMetadataAttribute("RouteTemplate", expected),
        }));

        // Act
        var result = CompiledPageRouteModelProvider.GetRouteTemplate(descriptor);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetRouteTemplate_ReturnsNull_IfAttributeDoesNotExist()
    {
        // Arrange
        var descriptor = new CompiledViewDescriptor(TestRazorCompiledItem.CreateForPage("/Pages/About.cshtml"));

        // Act
        var result = CompiledPageRouteModelProvider.GetRouteTemplate(descriptor);

        // Assert
        Assert.Null(result);
    }

    private CompiledPageRouteModelProvider CreateProvider(IList<RazorCompiledItem> items, RazorPagesOptions options = null)
    {
        options = options ?? new RazorPagesOptions();

        var provider = new TestCompiledPageRouteModelProvider(
            new ApplicationPartManager(),
            Options.Create(options),
            NullLogger<CompiledPageRouteModelProvider>.Instance);

        for (var i = 0; i < items.Count; i++)
        {
            provider.Descriptors.Add(new CompiledViewDescriptor(items[i]));
        }

        return provider;
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
