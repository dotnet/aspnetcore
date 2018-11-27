// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.DataAnnotations.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations.Test.Internal
{
    public class MvcDataAnnotationsMvcOptionsSetupTests
    {
        [Fact]
        public void MvcDataAnnotationsMvcOptionsSetup_ServiceConstructorWithoutIStringLocalizer()
        {
            // Arrange
            var services = new ServiceCollection();

            services.AddSingleton<IHostingEnvironment>(Mock.Of<IHostingEnvironment>());
            services.AddSingleton<IValidationAttributeAdapterProvider, ValidationAttributeAdapterProvider>();
            services.AddSingleton<IOptions<MvcDataAnnotationsLocalizationOptions>>(
                Options.Create(new MvcDataAnnotationsLocalizationOptions()));
            services.AddSingleton<IConfigureOptions<MvcOptions>, MvcDataAnnotationsMvcOptionsSetup>();

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var optionsSetup = serviceProvider.GetRequiredService<IConfigureOptions<MvcOptions>>();

            // Assert
            Assert.NotNull(optionsSetup);
        }
    }
}
