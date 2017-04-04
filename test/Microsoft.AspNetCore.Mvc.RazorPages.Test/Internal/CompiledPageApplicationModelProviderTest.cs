// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class CompiledPageApplicationModelProviderTest
    {
        [Fact]
        public void OnProvidersExecuting_AddsModelsForCompiledViews()
        {
            // Arrange
            var info = new[]
            {
                new CompiledPageInfo("/Pages/About.cshtml", typeof(object), routePrefix: string.Empty),
                new CompiledPageInfo("/Pages/Home.cshtml", typeof(object), "some-prefix"),
            };
            var provider = new TestCompiledPageApplicationModelProvider(info, new RazorPagesOptions());
            var context = new PageApplicationModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.Results,
                result =>
                {
                    Assert.Equal("/Pages/About.cshtml", result.RelativePath);
                    Assert.Equal("/Pages/About", result.ViewEnginePath);
                    Assert.Collection(result.Selectors,
                        selector => Assert.Equal("Pages/About", selector.AttributeRouteModel.Template));
                },
                result =>
                {
                    Assert.Equal("/Pages/Home.cshtml", result.RelativePath);
                    Assert.Equal("/Pages/Home", result.ViewEnginePath);
                    Assert.Collection(result.Selectors,
                        selector => Assert.Equal("Pages/Home/some-prefix", selector.AttributeRouteModel.Template));
                });
        }

        [Fact]
        public void OnProvidersExecuting_AddsMultipleSelectorsForIndexPage()
        {
            // Arrange
            var info = new[]
            {
                new CompiledPageInfo("/Pages/Index.cshtml", typeof(object), routePrefix: string.Empty),
                new CompiledPageInfo("/Pages/Admin/Index.cshtml", typeof(object), "some-template"),
            };
            var provider = new TestCompiledPageApplicationModelProvider(info, new RazorPagesOptions());
            var context = new PageApplicationModelProviderContext();

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(context.Results,
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
        public void OnProvidersExecuting_ThrowsIfRouteTemplateHasOverridePattern()
        {
            // Arrange
            var info = new[]
            {
                new CompiledPageInfo("/Pages/Index.cshtml", typeof(object), routePrefix: string.Empty),
                new CompiledPageInfo("/Pages/Home.cshtml", typeof(object), "/some-prefix"),
            };
            var provider = new TestCompiledPageApplicationModelProvider(info, new RazorPagesOptions());
            var context = new PageApplicationModelProviderContext();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuting(context));
            Assert.Equal("The route for the page at '/Pages/Home.cshtml' cannot start with / or ~/. Pages do not support overriding the file path of the page.",
                ex.Message);
        }

        public class TestCompiledPageApplicationModelProvider : CompiledPageApplicationModelProvider
        {
            private readonly IEnumerable<CompiledPageInfo> _info;

            public TestCompiledPageApplicationModelProvider(IEnumerable<CompiledPageInfo> info, RazorPagesOptions options)
                : base(new ApplicationPartManager(), new TestOptionsManager<RazorPagesOptions>(options))
            {
                _info = info;
            }

            protected override IEnumerable<CompiledPageInfo> GetCompiledPages() => _info;
        }
    }
}
