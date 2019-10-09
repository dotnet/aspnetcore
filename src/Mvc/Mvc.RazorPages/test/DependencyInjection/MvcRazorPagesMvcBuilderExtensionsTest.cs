// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class MvcRazorPagesMvcBuilderExtensionsTest
    {
        [Fact]
        public void AddRazorPagesOptions_AddsConventions()
        {
            // Arrange
            var services = new ServiceCollection().AddOptions()
                .AddSingleton<IConfigureOptions<RazorPagesOptions>, RazorPagesOptionsSetup>();
            var applicationModelConvention = Mock.Of<IPageApplicationModelConvention>();
            var routeModelConvention = Mock.Of<IPageRouteModelConvention>();
            var builder = new MvcBuilder(services, new ApplicationPartManager());
            builder.AddRazorPagesOptions(options =>
            {
                options.Conventions.Add(applicationModelConvention);
                options.Conventions.Add(routeModelConvention);
            });
            var serviceProvider = services.BuildServiceProvider();
            var accessor = serviceProvider.GetRequiredService<IOptions<RazorPagesOptions>>();

            // Act & Assert
            var conventions = accessor.Value.Conventions;

            // Assert
            Assert.Collection(
                conventions,
                convention => Assert.Same(applicationModelConvention, convention),
                convention => Assert.Same(routeModelConvention, convention));
        }
    }
}
