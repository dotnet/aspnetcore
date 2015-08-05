// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Localization;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.Framework.Localization.Tests
{
    public class AccessLanguageHeaderRequestCultureProviderTest
    {
        [Fact]
        public async void GetFallbackLanguage()
        {
            using (var server = TestServer.Create(app =>
            {
                var options = new RequestLocalizationOptions
                {
                    SupportedCultures = new List<CultureInfo>
                    {
                        new CultureInfo("ar-SA"),
                        new CultureInfo("en-US")
                    }
                };
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
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("jp,ar-SA,en-US");
                var count = client.DefaultRequestHeaders.AcceptLanguage.Count;
                var response = await client.GetAsync(string.Empty);
            }
        }
    }
}