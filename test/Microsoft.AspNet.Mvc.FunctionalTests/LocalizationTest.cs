// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class LocalizationTest : IClassFixture<MvcTestFixture<LocalizationWebSite.Startup>>
    {
        private const string SiteName = nameof(LocalizationWebSite);
        private static readonly Assembly _assembly = typeof(LocalizationTest).GetTypeInfo().Assembly;

        public LocalizationTest(MvcTestFixture<LocalizationWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        public static IEnumerable<object[]> LocalizationData
        {
            get
            {
                var expected1 =
@"<language-layout>en-gb-index
partial
mypartial
</language-layout>";

                yield return new[] { "en-GB", expected1 };

                var expected2 =
@"<fr-language-layout>fr-index
fr-partial
mypartial
</fr-language-layout>";
                yield return new[] { "fr", expected2 };

                if (!TestPlatformHelper.IsMono)
                {
                    // https://github.com/aspnet/Mvc/issues/2759
                    var expected3 =
 @"<language-layout>index
partial
mypartial
</language-layout>";
                    yield return new[] { "!-invalid-!", expected3 };
                }
            }
        }

        [Theory]
        [MemberData(nameof(LocalizationData))]
        public async Task Localization_SuffixViewName(string value, string expected)
        {
            // Arrange
            var cultureCookie = "c=" + value + "|uic=" + value;
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/");
            request.Headers.Add(
                "Cookie",
                new CookieHeaderValue("ASPNET_CULTURE", cultureCookie).ToString());

            // Act
            var response = await Client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
        }

        public static IEnumerable<object[]> LocalizationResourceData
        {
            get
            {
                var expected1 =
                    "Hello there!!" + Environment.NewLine +
                    "Learn More" + Environment.NewLine +
                    "Hi John      ! You are in 2015 year and today is Thursday";

                yield return new[] { "en-GB", expected1 };

                var expected2 =
                    "Bonjour!" + Environment.NewLine +
                    "apprendre Encore Plus" + Environment.NewLine +
                    "Salut John      ! Vous êtes en 2015 an aujourd'hui est Thursday";
                yield return new[] { "fr", expected2 };
            }
        }

        [Theory]
        [MemberData(nameof(LocalizationResourceData))]
        public async Task Localization_Resources_ReturnExpectedValues(string value, string expected)
        {
            // Arrange
            var cultureCookie = "c=" + value + "|uic=" + value;
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Home/Locpage");
            request.Headers.Add(
                "Cookie",
                new CookieHeaderValue("ASPNET_CULTURE", cultureCookie).ToString());

            // Act
            var response = await Client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task Localization_InvalidModel_ValidationAttributes_ReturnsLocalizedErrorMessage()
        {
            // Arrange
            var expected =
@"<span class=""field-validation-error"" data-valmsg-for=""Name"" data-valmsg-replace=""true"">Nom non valide. Longueur minimale de nom est 6</span>
<span class=""field-validation-error"" data-valmsg-for=""Product.ProductName"" data-valmsg-replace=""true"">Nom du produit est invalide</span>
<div class=""editor-label""><label for=""Name"">Name</label></div>
<div class=""editor-field""><input class=""input-validation-error text-box single-line"" data-val=""true"" data-val-minlength=""Nom non valide. Longueur minimale de nom est 6"" data-val-minlength-min=""6"" id=""Name"" name=""Name"" type=""text"" value=""A"" /> <span class=""field-validation-error"" data-valmsg-for=""Name"" data-valmsg-replace=""true"">Nom non valide. Longueur minimale de nom est 6</span></div>

<div class=""editor-label""><label for=""Product_ProductName"">ProductName</label></div>
<div class=""editor-field""><input class=""input-validation-error text-box single-line"" data-val=""true"" data-val-required=""Nom du produit est invalide"" id=""Product_ProductName"" name=""Product.ProductName"" type=""text"" value="""" /> <span class=""field-validation-error"" data-valmsg-for=""Product.ProductName"" data-valmsg-replace=""true"">Nom du produit est invalide</span></div>";

            var cultureCookie = "c=fr|uic=fr";
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Home/GetInvalidUser");
            request.Headers.Add(
                "Cookie",
                new CookieHeaderValue("ASPNET_CULTURE", cultureCookie).ToString());

            // Act
            var response = await Client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
        }
    }
}
