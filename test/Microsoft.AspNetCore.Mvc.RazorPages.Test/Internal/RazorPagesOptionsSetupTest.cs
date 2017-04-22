// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class RazorPagesOptionsSetupTest
    {
        [Fact]
        public void Configure_AddsGlobalFilters()
        {
            // Arrange
            var options = new RazorPagesOptions();
            var setup = new RazorPagesOptionsSetup();
            var applicationModel = new PageApplicationModel("/Home.cshtml", "/Home.cshtml");

            // Act
            setup.Configure(options);
            foreach (var convention in options.Conventions)
            {
                convention.Apply(applicationModel);
            }

            // Assert
            Assert.Collection(applicationModel.Filters,
                filter => Assert.IsType<PageSaveTempDataPropertyFilterFactory>(filter),
                filter => Assert.IsType<AutoValidateAntiforgeryTokenAttribute>(filter));
        }

        [Fact]
        public void Configure_SetsRazorPagesRoot()
        {
            // Arrange
            var options = new RazorPagesOptions();
            var setup = new RazorPagesOptionsSetup();

            // Act
            setup.Configure(options);

            // Assert
            Assert.Equal("/Pages", options.RootDirectory);
        }
    }
}
