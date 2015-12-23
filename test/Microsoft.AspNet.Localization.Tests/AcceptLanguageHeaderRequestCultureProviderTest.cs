// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Localization;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.Extensions.Localization.Tests
{
    public class AcceptLanguageHeaderRequestCultureProviderTest
    {
        [Fact]
        public async void GetFallbackLanguage_ReturnsFirstNonNullCultureFromSupportedCultureList()
        {
            var builder = new WebApplicationBuilder()
                .Configure(app =>
                {
                    app.UseRequestLocalization(options =>
                    {
                        options.DefaultRequestCulture = new RequestCulture("en-US");
                        options.SupportedCultures = new List<CultureInfo>
                        {
                            new CultureInfo("ar-SA"),
                            new CultureInfo("en-US")
                        };
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("ar-SA", requestCulture.Culture.Name);
                        return Task.FromResult(0);
                    });
                });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("jp,ar-SA,en-US");
                var count = client.DefaultRequestHeaders.AcceptLanguage.Count;
                var response = await client.GetAsync(string.Empty);
                Assert.Equal(3, count);
            }
        }

        [Fact]
        public async void GetFallbackLanguage_ReturnsFromSupportedCulture_AcceptLanguageListContainsSupportedCultures()
        {
            var builder = new WebApplicationBuilder()
                .Configure(app =>
                {
                    app.UseRequestLocalization(options =>
                    {
                        options.DefaultRequestCulture = new RequestCulture("fr-FR");
                        options.SupportedCultures = new List<CultureInfo>
                        {
                            new CultureInfo("ar-SA"),
                            new CultureInfo("en-US")
                        };
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("ar-SA", requestCulture.Culture.Name);
                        return Task.FromResult(0);
                    });
                });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,ar-SA,en-US");
                var count = client.DefaultRequestHeaders.AcceptLanguage.Count;
                var response = await client.GetAsync(string.Empty);
            }
        }

        [Fact]
        public async void GetFallbackLanguage_ReturnsDefault_AcceptLanguageListDoesnotContainSupportedCultures()
        {
            var builder = new WebApplicationBuilder()
                .Configure(app =>
                {
                    app.UseRequestLocalization(options =>
                    {
                        options.DefaultRequestCulture = new RequestCulture("fr-FR");
                        options.SupportedCultures = new List<CultureInfo>
                        {
                            new CultureInfo("ar-SA"),
                            new CultureInfo("af-ZA")
                        };
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("fr-FR", requestCulture.Culture.Name);
                        return Task.FromResult(0);
                    });
                });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,ar-MA,en-US");
                var count = client.DefaultRequestHeaders.AcceptLanguage.Count;
                var response = await client.GetAsync(string.Empty);
                Assert.Equal(3, count);
            }
        }
        
        [Fact]
        public async void OmitDefaultRequestCultureShouldNotThrowNullReferenceException_And_ShouldGetTheRightCulture()
        {
            var builder = new WebApplicationBuilder()
                .Configure(app =>
                {
                    app.UseRequestLocalization(options =>
                    {
                        options.DefaultRequestCulture = new RequestCulture("en-US");
                        options.SupportedCultures = new List<CultureInfo>
                        {
                            new CultureInfo("ar-YE")
                        };
                        options.SupportedUICultures = new List<CultureInfo>
                        {
                            new CultureInfo("ar-YE")
                        };
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("ar-YE", requestCulture.Culture.Name);
                        Assert.Equal("ar-YE", requestCulture.UICulture.Name);
                        return Task.FromResult(0);
                    });
                });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,ar-YE,en-US");
                var count = client.DefaultRequestHeaders.AcceptLanguage.Count;
                var response = await client.GetAsync(string.Empty);
                Assert.Equal(3, count);
            }
        }
    }
}