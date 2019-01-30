// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    public class ViewDataAttributePageApplicationModelProviderTest
    {
        [Fact]
        public void OnProvidersExecuting_DoesNotAddFilter_IfTypeHasNoViewDataProperties()
        {
            // Arrange
            var type = typeof(TestPageModel_NoViewDataProperties);
            var provider = new ViewDataAttributePageApplicationModelProvider();
            var context = CreateProviderContext(type);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            Assert.Empty(context.PageApplicationModel.Filters);
        }

        [Fact]
        public void AddsViewDataPropertyFilter_ForViewDataAttributeProperties()
        {
            // Arrange
            var type = typeof(TestPageModel_ViewDataProperties);
            var provider = new ViewDataAttributePageApplicationModelProvider();
            var context = CreateProviderContext(type);

            // Act
            provider.OnProvidersExecuting(context);

            // Assert
            var filter = Assert.Single(context.PageApplicationModel.Filters);
            var viewDataFilter = Assert.IsType<PageViewDataAttributeFilterFactory>(filter);
            Assert.Collection(
                viewDataFilter.Properties,
                property => Assert.Equal(nameof(TestPageModel_ViewDataProperties.DateTime), property.PropertyInfo.Name));
        }

        private static PageApplicationModelProviderContext CreateProviderContext(Type handlerType)
        {
            var descriptor = new CompiledPageActionDescriptor();
            var context = new PageApplicationModelProviderContext(descriptor, typeof(TestPage).GetTypeInfo())
            {
                PageApplicationModel = new PageApplicationModel(descriptor, handlerType.GetTypeInfo(), Array.Empty<object>()),
            };

            return context;
        }

        private class TestPage : Page
        {
            public object Model => null;

            public override Task ExecuteAsync() => null;
        }

        public class TestPageModel_NoViewDataProperties
        {
            public DateTime? DateTime { get; set; }
        }

        public class TestPageModel_ViewDataProperties
        {
            [ViewData]
            public DateTime? DateTime { get; set; }
        }
    }
}
