// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using LocalizationWebSite;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class LocalizationTest
    {
        private const string SiteName = nameof(LocalizationWebSite);
        private static readonly Assembly _assembly = typeof(LocalizationTest).GetTypeInfo().Assembly;

        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new Startup().ConfigureServices;

        public static IEnumerable<object[]> LocalizationData
        {
            get
            {
                var expected1 =
 @"<language-layout>
en-gb-index
partial
mypartial
</language-layout>";

                yield return new[] { "en-GB", expected1 };

                var expected2 =
 @"<fr-language-layout>
fr-index
fr-partial
mypartial
</fr-language-layout>";
                yield return new[] { "fr", expected2 };

                var expected3 =
 @"<language-layout>
index
partial
mypartial
</language-layout>";
                yield return new[] { "na", expected3 };

            }
        }

        [Theory]
        [MemberData(nameof(LocalizationData))]
        public async Task Localization_SuffixViewName(string value, string expected)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var cultureCookie = "c=" + value + "|uic=" + value;
            client.DefaultRequestHeaders.Add(
                "Cookie",
                new CookieHeaderValue("ASPNET_CULTURE", cultureCookie).ToString());

            // Act
            var body = await client.GetStringAsync("http://localhost/");

            // Assert
            Assert.Equal(expected, body.Trim());
        }
    }
}
