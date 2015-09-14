// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Localization;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.Framework.Localization.Tests
{
    public class QueryStringRequestCultureProviderTest
    {
        [Fact]
        public async void GetCultureInfoFromQueryString()
        {
            using (var server = TestServer.Create(app =>
            {
                var options = new RequestLocalizationOptions();
                app.UseRequestLocalization(options);
                app.Run(context =>
                {
                    var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                    var requestCulture = requestCultureFeature.RequestCulture;
                    Assert.Equal("ar-SA", requestCulture.Culture.Name);
                    Assert.Equal("ar-YE", requestCulture.UICulture.Name);
                    return Task.FromResult(0);
                });
            }))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync("/page?culture=ar-SA&ui-culture=ar-YE");
            }
        }

        [Fact]
        public async void GetDefaultCultureInfoIfCultureKeysAreMissing()
        {
            using (var server = TestServer.Create(app =>
            {
                var options = new RequestLocalizationOptions();
                app.UseRequestLocalization(options);
                app.Run(context =>
                {
                    var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                    var requestCulture = requestCultureFeature.RequestCulture;
                    Assert.Equal(options.DefaultRequestCulture.Culture.Name, requestCulture.Culture.Name);
                    Assert.Equal(options.DefaultRequestCulture.UICulture.Name, requestCulture.UICulture.Name);
                    return Task.FromResult(0);
                });
            }))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync("/page");
            }
        }

        [Fact]
        public async void GetDefaultCultureInfoIfCultureIsInvalid()
        {
            using (var server = TestServer.Create(app =>
            {
                var options = new RequestLocalizationOptions();
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
                var response = await client.GetAsync("/page?culture=ar-XY&ui-culture=ar-SA");
            }
        }

        [Fact]
        public async void GetDefaultCultureInfoIfUICultureIsInvalid()
        {
            using (var server = TestServer.Create(app =>
            {
                var options = new RequestLocalizationOptions();
                app.UseRequestLocalization(options);
                app.Run(context =>
                {
                    var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                    var requestCulture = requestCultureFeature.RequestCulture;
                    Assert.Equal(options.DefaultRequestCulture.UICulture.Name, requestCulture.UICulture.Name);
                    return Task.FromResult(0);
                });
            }))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync("/page?culture=ar-SA&ui-culture=ar-XY");
            }
        }

        [Fact]
        public async void GetSameCultureInfoIfCultureKeyIsMissing()
        {
            using (var server = TestServer.Create(app =>
            {
                var options = new RequestLocalizationOptions();
                app.UseRequestLocalization(options);
                app.Run(context =>
                {
                    var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                    var requestCulture = requestCultureFeature.RequestCulture;
                    Assert.Equal("ar-SA", requestCulture.Culture.Name);
                    Assert.Equal("ar-SA", requestCulture.UICulture.Name);
                    return Task.FromResult(0);
                });
            }))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync("/page?ui-culture=ar-SA");
            }
        }

        [Fact]
        public async void GetSameCultureInfoIfUICultureKeyIsMissing()
        {
            using (var server = TestServer.Create(app =>
            {
                var options = new RequestLocalizationOptions();
                app.UseRequestLocalization(options);
                app.Run(context =>
                {
                    var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                    var requestCulture = requestCultureFeature.RequestCulture;
                    Assert.Equal("ar-SA", requestCulture.Culture.Name);
                    Assert.Equal("ar-SA", requestCulture.UICulture.Name);
                    return Task.FromResult(0);
                });
            }))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync("/page?culture=ar-SA");
            }
        }

        [Fact]
        public async void GetCultureInfoFromQueryStringWithCustomKeys()
        {
            using (var server = TestServer.Create(app =>
            {
                var options = new RequestLocalizationOptions();
                var provider = new QueryStringRequestCultureProvider();
                provider.QueryStringKey = "c";
                provider.UIQueryStringKey = "uic";
                options.RequestCultureProviders.Insert(0, provider);
                app.UseRequestLocalization(options);
                app.Run(context =>
                {
                    var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                    var requestCulture = requestCultureFeature.RequestCulture;
                    Assert.Equal("ar-SA", requestCulture.Culture.Name);
                    Assert.Equal("ar-YE", requestCulture.UICulture.Name);
                    return Task.FromResult(0);
                });
            }))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync("/page?c=ar-SA&uic=ar-YE");
            }
        }
    }
}