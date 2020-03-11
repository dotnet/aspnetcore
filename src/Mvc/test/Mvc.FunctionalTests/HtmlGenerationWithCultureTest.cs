// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using AngleSharp.Html;
using HtmlGenerationWebSite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class HtmlGenerationWithCultureTest : LoggedTest, IClassFixture<MvcTestFixture<StartupWithCultureReplace>>
    {
        public HtmlGenerationWithCultureTest(
            ITestOutputHelper testOutputHelper,
            MvcTestFixture<StartupWithCultureReplace> fixture) : base(testOutputHelper)
        {
            Factory = fixture.WithWebHostBuilder(builder => builder.UseStartup<StartupWithCultureReplace>());
            Client = Factory.CreateDefaultClient();
        }

        public WebApplicationFactory<StartupWithCultureReplace> Factory { get; }

        public HttpClient Client { get; }

        [Fact]
        public async Task CacheTagHelper_AllowsVaryingByCulture()
        {
            // Arrange
            string culture;
            string correlationId;
            string cachedCorrelationId;

            // Act - 1
            var document = await Client.GetHtmlDocumentAsync("/CacheTagHelper_VaryByCulture?culture=fr-Fr&correlationId=10");
            ReadValuesFromDocument();

            // Assert - 1
            Assert.Equal("fr-FR", culture);
            Assert.Equal("10", correlationId);
            Assert.Equal("10", cachedCorrelationId);

            // Act - 2
            document = await Client.GetHtmlDocumentAsync("/CacheTagHelper_VaryByCulture?culture=en-GB&correlationId=11");
            ReadValuesFromDocument();

            // Assert - 2
            Assert.Equal("en-GB", culture);
            Assert.Equal("11", correlationId);
            Assert.Equal("11", cachedCorrelationId);

            // Act - 3
            document = await Client.GetHtmlDocumentAsync("/CacheTagHelper_VaryByCulture?culture=fr-Fr&correlationId=14");
            ReadValuesFromDocument();

            // Assert - 3
            Assert.Equal("fr-FR", culture);
            Assert.Equal("14", correlationId);
            // Verify we're reading a cached value
            Assert.Equal("10", cachedCorrelationId);

            void ReadValuesFromDocument()
            {
                culture = QuerySelector(document, "#culture").TextContent;
                correlationId = QuerySelector(document, "#correlation-id").TextContent;
                cachedCorrelationId = QuerySelector(document, "#cached-correlation-id").TextContent;
            }
        }

        [Fact]
        public async Task CacheTagHelper_AllowsVaryingByUICulture()
        {
            // Arrange
            string culture;
            string uiCulture;
            string correlationId;
            string cachedCorrelationId;

            // Act - 1
            var document = await Client.GetHtmlDocumentAsync("/CacheTagHelper_VaryByCulture?culture=fr-Fr&ui-culture=fr-FR&correlationId=10");
            ReadValuesFromDocument();

            // Assert - 1
            Assert.Equal("fr-FR", culture);
            Assert.Equal("fr-FR", uiCulture);
            Assert.Equal("10", correlationId);
            Assert.Equal("10", cachedCorrelationId);

            // Act - 2
            document = await Client.GetHtmlDocumentAsync("/CacheTagHelper_VaryByCulture?culture=fr-Fr&ui-culture=fr-CA&correlationId=11");
            ReadValuesFromDocument();

            // Assert - 2
            Assert.Equal("fr-FR", culture);
            Assert.Equal("fr-CA", uiCulture);
            Assert.Equal("11", correlationId);
            Assert.Equal("11", cachedCorrelationId);

            // Act - 3
            document = await Client.GetHtmlDocumentAsync("/CacheTagHelper_VaryByCulture?culture=fr-Fr&ui-culture=fr-FR&correlationId=14");
            ReadValuesFromDocument();

            // Assert - 3
            Assert.Equal("fr-FR", culture);
            Assert.Equal("fr-FR", uiCulture);
            Assert.Equal("14", correlationId);
            // Verify we're reading a cached value
            Assert.Equal("10", cachedCorrelationId);

            void ReadValuesFromDocument()
            {
                culture = QuerySelector(document, "#culture").TextContent;
                uiCulture = QuerySelector(document, "#ui-culture").TextContent;
                correlationId = QuerySelector(document, "#correlation-id").TextContent;
                cachedCorrelationId = QuerySelector(document, "#cached-correlation-id").TextContent;
            }
        }

        [Fact]
        public async Task CacheTagHelper_VaryByCultureComposesWithOtherVaryByOptions()
        {
            // Arrange
            var client = Factory
                .WithWebHostBuilder(builder => builder
                    .UseStartup<StartupWithCultureReplace>()
                    .ConfigureTestServices(services => services.AddSingleton(LoggerFactory)))
                .CreateDefaultClient();
            string culture;
            string correlationId;
            string cachedCorrelationId;

            // Act - 1
            var document = await client.GetHtmlDocumentAsync("/CacheTagHelper_VaryByCulture?culture=fr-Fr&correlationId=10");
            ReadValuesFromDocument();

            // Assert - 1
            Assert.Equal("fr-FR", culture);
            Assert.Equal("10", correlationId);
            Assert.Equal("10", cachedCorrelationId);

            // Act - 2
            document = await client.GetHtmlDocumentAsync("/CacheTagHelper_VaryByCulture?culture=fr-Fr&correlationId=11&varyByQueryKey=new-key");
            ReadValuesFromDocument();

            // Assert - 2
            // vary-by-query should produce a new cached value.
            Assert.Equal("fr-FR", culture);
            Assert.Equal("11", correlationId);
            Assert.Equal("11", cachedCorrelationId);

            // Act - 3
            document = await client.GetHtmlDocumentAsync("/CacheTagHelper_VaryByCulture?culture=fr-Fr&correlationId=14");
            ReadValuesFromDocument();

            // Assert - 3
            Assert.Equal("fr-FR", culture);
            Assert.Equal("14", correlationId);

            if (cachedCorrelationId != "10")
            {
                // This is logging to investigate potential flakiness in this test tracked by https://github.com/aspnet/Mvc/issues/8281
                var documentContent = document.ToHtml(new HtmlMarkupFormatter());
                throw new XunitException($"Unexpected correlation Id, reading values from document:{Environment.NewLine}{documentContent}");
            }

            Assert.Equal("10", cachedCorrelationId);

            void ReadValuesFromDocument()
            {
                culture = QuerySelector(document, "#culture").TextContent;
                correlationId = QuerySelector(document, "#correlation-id").TextContent;
                cachedCorrelationId = QuerySelector(document, "#cached-correlation-id").TextContent;
            }
        }

        private static IElement QuerySelector(IHtmlDocument document, string selector)
        {
            var element = document.QuerySelector(selector);
            if (element == null)
            {
                throw new ArgumentException($"Document does not contain element that matches the selector {selector}: " + Environment.NewLine + document.DocumentElement.OuterHtml);
            }

            return element;
        }
    }
}
