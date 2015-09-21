// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using System.Xml.Linq;
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
#if DNXCORE50
                // Work around aspnet/External#42. Only the invariant culture works with Core CLR on Linux.
                if (!TestPlatformHelper.IsLinux)
#endif
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
                }

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
                // Dnx does not support reading resources yet. Coreclr return null value while trying to read resources.
                // https://github.com/aspnet/Mvc/issues/2747
#if DNX451
                var expected1 =
@"Hello there!!
Learn More
Hi John      ! You are in 2015 year and today is Thursday";

                yield return new[] {"en-GB", expected1 };

                var expected2 =
@"Bonjour!
apprendre Encore Plus
Salut John      ! Vous êtes en 2015 an aujourd'hui est Thursday";
                yield return new[] { "fr", expected2 };
#else
                var expectedCoreClr =
@"Hello there!!
Learn More
Hi";
                yield return new[] {"en-GB", expectedCoreClr };
                yield return new[] {"fr", expectedCoreClr };
#endif

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

            if (!value.StartsWith("en"))
            {
                // Manually generating .resources file since we don't autogenerate .resources file yet.
                WriteResourceFile("HomeController." + value + ".resx");
                WriteResourceFile("Views.Shared._LocalizationLayout.cshtml." + value + ".resx");
            }
            WriteResourceFile("Views.Home.Locpage.cshtml." + value + ".resx");

            // Act
            var response = await Client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        private void WriteResourceFile(string resxFileName)
        {
            var resxFilePath = Path.Combine("..", "WebSites", SiteName, "Resources");
            var resxFullFileName = Path.Combine(resxFilePath, resxFileName);
            if (File.Exists(resxFullFileName))
            {
                using (var fs = File.OpenRead(resxFullFileName))
                {
                    var document = XDocument.Load(fs);

                    var binDirPath = Path.Combine(resxFilePath, "bin");
                    if (!Directory.Exists(binDirPath))
                    {
                        Directory.CreateDirectory(binDirPath);
                    }

                    // Put in "bin" sub-folder of resx file
                    var targetPath = Path.Combine(
                        binDirPath,
                        Path.ChangeExtension(resxFileName, ".resources"));

                    using (var targetStream = File.Create(targetPath))
                    {
                        var rw = new ResourceWriter(targetStream);

                        foreach (var e in document.Root.Elements("data"))
                        {
                            var name = e.Attribute("name").Value;
                            var value = e.Element("value").Value;

                            rw.AddResource(name, value);
                        }

                        rw.Generate();
                    }
                }
            }
        }
    }
}
