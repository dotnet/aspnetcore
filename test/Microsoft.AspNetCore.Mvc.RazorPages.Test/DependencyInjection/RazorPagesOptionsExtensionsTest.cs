// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class RazorPagesOptionsExtensionsTest
    {
        [Fact]
        public void AddFilter_AddsFiltersToAllPages()
        {
            // Arrange
            var filter = Mock.Of<IFilterMetadata>();
            var options = new RazorPagesOptions();
            var models = new[]
            {
                new PageApplicationModel("/Pages/Index.cshtml", "/Index.cshtml"),
                new PageApplicationModel("/Pages/Users/Account.cshtml", "/Users/Account.cshtml"),
                new PageApplicationModel("/Pages/Users/Contact.cshtml", "/Users/Contact.cshtml"),
            };

            // Act
            options.ConfigureFilter(filter);
            ApplyConventions(options, models);

            // Assert
            Assert.Collection(models,
                model => Assert.Same(filter, Assert.Single(model.Filters)),
                model => Assert.Same(filter, Assert.Single(model.Filters)),
                model => Assert.Same(filter, Assert.Single(model.Filters)));
        }

        [Fact]
        public void AuthorizePage_AddsAuthorizeFilterToSpecificPage()
        {
            // Arrange
            var options = new RazorPagesOptions();
            var models = new[]
            {
                new PageApplicationModel("/Pages/Index.cshtml", "/Index.cshtml"),
                new PageApplicationModel("/Pages/Users/Account.cshtml", "/Users/Account.cshtml"),
                new PageApplicationModel("/Pages/Users/Contact.cshtml", "/Users/Contact.cshtml"),
            };

            // Act
            options.AuthorizePage("/Users/Account.cshtml", "Manage-Accounts");
            ApplyConventions(options, models);

            // Assert
            Assert.Collection(models,
                model => Assert.Empty(model.Filters),
                model =>
                {
                    Assert.Equal("/Users/Account.cshtml", model.ViewEnginePath);
                    var authorizeFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                    var authorizeData = Assert.IsType<AuthorizeAttribute>(Assert.Single(authorizeFilter.AuthorizeData));
                    Assert.Equal("Manage-Accounts", authorizeData.Policy);
                },
                model => Assert.Empty(model.Filters));
        }

        [Theory]
        [InlineData("/Users")]
        [InlineData("/Users/")]
        public void AuthorizePage_AddsAuthorizeFilterToPagesUnderFolder(string folderName)
        {
            // Arrange
            var options = new RazorPagesOptions();
            var models = new[]
            {
                new PageApplicationModel("/Pages/Index.cshtml", "/Index.cshtml"),
                new PageApplicationModel("/Pages/Users/Account.cshtml", "/Users/Account.cshtml"),
                new PageApplicationModel("/Pages/Users/Contact.cshtml", "/Users/Contact.cshtml"),
            };

            // Act
            options.AuthorizeFolder(folderName, "Manage-Accounts");
            ApplyConventions(options, models);

            // Assert
            Assert.Collection(models,
                model => Assert.Empty(model.Filters),
                model =>
                {
                    Assert.Equal("/Users/Account.cshtml", model.ViewEnginePath);
                    var authorizeFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                    var authorizeData = Assert.IsType<AuthorizeAttribute>(Assert.Single(authorizeFilter.AuthorizeData));
                    Assert.Equal("Manage-Accounts", authorizeData.Policy);
                },
                model =>
                {
                    Assert.Equal("/Users/Contact.cshtml", model.ViewEnginePath);
                    var authorizeFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                    var authorizeData = Assert.IsType<AuthorizeAttribute>(Assert.Single(authorizeFilter.AuthorizeData));
                    Assert.Equal("Manage-Accounts", authorizeData.Policy);
                });
        }

        private static void ApplyConventions(RazorPagesOptions options, PageApplicationModel[] models)
        {
            foreach (var convention in options.Conventions)
            {
                foreach (var model in models)
                {
                    convention.Apply(model);
                }
            }
        }
    }
}
