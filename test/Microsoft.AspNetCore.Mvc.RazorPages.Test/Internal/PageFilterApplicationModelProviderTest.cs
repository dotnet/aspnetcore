// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageFilterApplicationModelProviderTest
    {
        [Fact]
        public void OnProvidersExecuting_AddsFiltersToModels()
        {
            // Arrange
            var applicationModel1 = new PageApplicationModel("/Home.cshtml", "/Home.cshtml");
            var applicationModel2 = new PageApplicationModel("/About.cshtml", "/About.cshtml");
            var modelProvider = new PageFilterApplicationModelProvider();
            var context = new PageApplicationModelProviderContext
            {
                Results =
                {
                    applicationModel1,
                    applicationModel2,
                }
            };

            // Act
            modelProvider.OnProvidersExecuting(context);

            // Assert
            Assert.Collection(applicationModel1.Filters,
                filter => Assert.IsType<PageSaveTempDataPropertyFilterFactory>(filter),
                filter => Assert.IsType<AutoValidateAntiforgeryTokenAttribute>(filter));

            Assert.Collection(applicationModel2.Filters,
                filter => Assert.IsType<PageSaveTempDataPropertyFilterFactory>(filter),
                filter => Assert.IsType<AutoValidateAntiforgeryTokenAttribute>(filter));
        }
    }
}
