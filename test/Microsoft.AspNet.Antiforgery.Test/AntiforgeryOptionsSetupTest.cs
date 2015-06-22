// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Antiforgery
{
    public class AntiforgeryOptionsSetupTest
    {
        [Theory]
        [InlineData("HelloWorldApp", "tGmK82_ckDw")]
        [InlineData("TodoCalendar", "7mK1hBEBwYs")]
        public void AntiforgeryOptionsSetup_SetsDefaultCookieName_BasedOnApplicationId(
            string applicationId,
            string expectedCookieName)
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddAntiforgery();
            serviceCollection.ConfigureDataProtection(o => o.SetApplicationName(applicationId));

            var services = serviceCollection.BuildServiceProvider();
            var options = services.GetRequiredService<IOptions<AntiforgeryOptions>>();

            // Act
            var cookieName = options.Options.CookieName;

            // Assert
            Assert.Equal(expectedCookieName, cookieName);
        }

        [Fact]
        public void AntiforgeryOptionsSetup_UserOptionsSetup_CanSetCookieName()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddAntiforgery();
            serviceCollection.ConfigureDataProtection(o => o.SetApplicationName("HelloWorldApp"));

            serviceCollection.Configure<AntiforgeryOptions>(o =>
            {
                Assert.Null(o.CookieName);
                o.CookieName = "antiforgery";
            }, order: 9999);

            var services = serviceCollection.BuildServiceProvider();
            var options = services.GetRequiredService<IOptions<AntiforgeryOptions>>();

            // Act
            var cookieName = options.Options.CookieName;

            // Assert
            Assert.Equal("antiforgery", cookieName);
        }
    }
}
