// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Localization;
using Microsoft.AspNet.TestHost;
using Microsoft.Extensions.Globalization;
using Xunit;

namespace Microsoft.Extensions.Localization.Tests
{
    public class CustomRequestCultureProviderTest
    {
        [Fact]
        public async void CustomRequestCultureProviderThatGetsCultureInfoFromUrl()
        {
            using (var server = TestServer.Create(app =>
            {
                var options = new RequestLocalizationOptions()
                {
                    SupportedCultures = new List<CultureInfo>
                    {
                        new CultureInfo("ar")
                    },
                    SupportedUICultures = new List<CultureInfo>
                    {
                        new CultureInfo("ar")
                    }
                };
                options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(context =>
                {
                    var culture = GetCultureInfoFromUrl(context, options.SupportedCultures);
                    var requestCulture = new ProviderCultureResult(culture);
                    return Task.FromResult(requestCulture);
                }));
                app.UseRequestLocalization(options, defaultRequestCulture: new RequestCulture("en-US"));
                app.Run(context =>
                {
                    var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                    var requestCulture = requestCultureFeature.RequestCulture;
                    Assert.Equal("ar", requestCulture.Culture.Name);
                    return Task.FromResult(0);
                });
            }))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync("/ar/page");
            }
        }

        private string GetCultureInfoFromUrl(HttpContext context, IList<CultureInfo> supportedCultures)
        {
            var currentCulture = "en";
            var segments = context.Request.Path.Value.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 1 && segments[0].Length == 2)
            {
                currentCulture = segments[0];
            }

            return currentCulture;
        }
    }
}