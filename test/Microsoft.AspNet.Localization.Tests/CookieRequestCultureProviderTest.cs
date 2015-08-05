// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Localization;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.Framework.Localization.Tests
{
    public class CookieRequestCultureProviderTest
    {
        [Fact]
        public async void GetCultureInfoFromPersistentCookie()
        {
            using (var server = TestServer.Create(app =>
            {
                var options = new RequestLocalizationOptions();
                var provider = new CookieRequestCultureProvider();
                provider.CookieName = "Preferences";
                options.RequestCultureProviders.Insert(0, provider);
                app.UseRequestLocalization(options);
                app.Run(context =>
                {
                    var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                    var requestCulture = requestCultureFeature.RequestCulture;
                    Assert.Equal("ar-SA", requestCulture.Culture.Name);
                    return Task.FromResult(0);
                });
            }))
            {
                var client = server.CreateClient();
                var culture = new CultureInfo("ar-SA");
                var requestCulture = new RequestCulture(culture);
                var value = CookieRequestCultureProvider.MakeCookieValue(requestCulture);
                client.DefaultRequestHeaders.Add("Cookie", new CookieHeaderValue("Preferences", value).ToString());
                var response = await client.GetAsync(string.Empty);
                Assert.Equal("c=ar-SA|uic=ar-SA",value);
            }
        }

        [Fact]
        public async void GetDefaultCultureInfoIfCultureKeysAreMissingOrInvalid()
        {
            using (var server = TestServer.Create(app =>
            {
                var options = new RequestLocalizationOptions();
                var provider = new CookieRequestCultureProvider();
                provider.CookieName = "Preferences";
                options.RequestCultureProviders.Insert(0, provider);
                app.UseRequestLocalization(options);
                app.Run(context =>
                {
                    var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                    var requestCulture = requestCultureFeature.RequestCulture;
                    Assert.Equal(options.DefaultRequestCulture.Culture.Name, requestCulture.Culture.Name);
                    return Task.FromResult(0);
                });
            }))
            {
                var client = server.CreateClient();
                client.DefaultRequestHeaders.Add("Cookie", new CookieHeaderValue("Preferences", "uic=ar-SA").ToString());
                var response = await client.GetAsync(string.Empty);
            }
        }

        [Fact]
        public async void GetDefaultCultureInfoIfCookieDoesNotExist()
        {
            using (var server = TestServer.Create(app =>
            {
                var options = new RequestLocalizationOptions();
                var provider = new CookieRequestCultureProvider();
                provider.CookieName = "Preferences";
                options.RequestCultureProviders.Insert(0, provider);
                app.UseRequestLocalization(options);
                app.Run(context =>
                {
                    var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                    var requestCulture = requestCultureFeature.RequestCulture;
                    Assert.Equal(options.DefaultRequestCulture.Culture.Name, requestCulture.Culture.Name);
                    return Task.FromResult(0);
                });
            }))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync(string.Empty);
            }
        }
    }
}