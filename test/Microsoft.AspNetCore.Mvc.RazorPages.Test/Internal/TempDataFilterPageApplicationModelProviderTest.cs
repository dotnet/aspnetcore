// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class TempDataFilterPageApplicationModelProviderTest
    {
        [Fact]
        public void OnProvidersExecuting_AddsFiltersToModel()
        {
            // Arrange
            var actionDescriptor = new PageActionDescriptor();
            var applicationModel = new PageApplicationModel(
                actionDescriptor,
                typeof(object).GetTypeInfo(),
                new object[0]);
            var applicationModelProvider = new TempDataFilterPageApplicationModelProvider();
            var context = new PageApplicationModelProviderContext(new PageActionDescriptor(), typeof(object).GetTypeInfo())
            {
                PageApplicationModel = applicationModel,
            };

            // Act
            applicationModelProvider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(
                applicationModel.Filters,
                filter => Assert.IsType<PageSaveTempDataPropertyFilterFactory>(filter));
        }
    }
}
