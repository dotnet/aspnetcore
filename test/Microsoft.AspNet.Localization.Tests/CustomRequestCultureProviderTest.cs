// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Localization;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Globalization;
using Xunit;

namespace Microsoft.Framework.Localization.Tests
{
    public class CustomRequestCultureProviderTest
    {
        [Fact]
        public async void CustomRequestCultureProviderThatGetsCultureInfoFromUrl()
        {
            using (var server = TestServer.Create(app =>
            {
                var options = new RequestLocalizationOptions();
                options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(context =>
                {
                    var culture = GetCultureInfoFromUrl(context);
                    var requestCulture = new RequestCulture(culture);
                    return Task.FromResult(requestCulture);
                }));
                app.UseRequestLocalization(options);
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

        private CultureInfo GetCultureInfoFromUrl(HttpContext context)
        {
            var currentCulture = "en";
            var segments = context.Request.Path.Value.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 1 && segments[0].Length == 2)
            {
                if (CultureInfoCache.KnownCultureNames.Contains(segments[0]))
                    currentCulture = segments[0];
                else
                    throw new InvalidOperationException($"The '{segments[0]}' is invalid culture name.");
            }
            return CultureInfoCache.GetCultureInfo(currentCulture);
        }
    }
}