// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using HtmlGenerationWebSite;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc.TagHelpers;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.WebEncoders;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class HtmlGenerationTest
    {
        private const string SiteName = nameof(HtmlGenerationWebSite);
        private static readonly Assembly _resourcesAssembly = typeof(HtmlGenerationTest).GetTypeInfo().Assembly;

        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new Startup().ConfigureServices;

        [Theory]
        [InlineData("Index", null)]
        // Test ability to generate nearly identical HTML with MVC tag and HTML helpers.
        // Only attribute order should differ.
        [InlineData("Order", "/HtmlGeneration_Order/Submit")]
        [InlineData("OrderUsingHtmlHelpers", "/HtmlGeneration_Order/Submit")]
        [InlineData("Product", null)]
        [InlineData("Customer", "/Customer/HtmlGeneration_Customer")]
        // Testing InputTagHelpers invoked in the partial views
        [InlineData("ProductList", null)]
        // Testing MVC tag helpers invoked in the editor templates from HTML helpers
        [InlineData("EmployeeList", null)]
        // Testing SelectTagHelper with Html.BeginForm
        [InlineData("CreateWarehouse", null)]
        // Testing the HTML helpers with FormTagHelper
        [InlineData("EditWarehouse", null)]
        // Testing the EnvironmentTagHelper
        [InlineData("Environment", null)]
        // Testing the LinkTagHelper
        [InlineData("Link", null)]
        // Testing the ScriptTagHelper
        [InlineData("Script", null)]
        // Testing the ImageTagHelper
        [InlineData("Image", null)]
        // Testing InputTagHelper with File
        [InlineData("Input", null)]
        public async Task HtmlGenerationWebSite_GeneratesExpectedResults(string action, string antiForgeryPath)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/HtmlGenerationWebSite.HtmlGeneration_Home." + action + ".html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await client.GetAsync("http://localhost/HtmlGeneration_Home/" + action);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

            responseContent = responseContent.Trim();
            if (antiForgeryPath == null)
            {
#if GENERATE_BASELINES
                ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
                Assert.Equal(expectedContent.Trim(), responseContent);
#endif
            }
            else
            {
                var forgeryToken = AntiForgeryTestHelper.RetrieveAntiForgeryToken(responseContent, antiForgeryPath);
#if GENERATE_BASELINES
                // Reverse usual substitution and insert a format item into the new file content.
                responseContent = responseContent.Replace(forgeryToken, "{0}");
                ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
                expectedContent = string.Format(expectedContent, forgeryToken);
                Assert.Equal(expectedContent.Trim(), responseContent);
#endif
            }
        }

        [Theory]
        [InlineData("EditWarehouse", null)]
        [InlineData("Index", null)]
        [InlineData("Link", null)]
        [InlineData("Order", "/HtmlGeneration_Order/Submit")]
        [InlineData("OrderUsingHtmlHelpers", "/HtmlGeneration_Order/Submit")]
        [InlineData("Product", null)]
        [InlineData("Script", null)]
        public async Task HtmlGenerationWebSite_GenerateEncodedResults(string action, string antiForgeryPath)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, services =>
            {
                _configureServices(services);
                services.AddTransient<IHtmlEncoder, TestHtmlEncoder>();
                services.AddTransient<IJavaScriptStringEncoder, TestJavaScriptEncoder>();
                services.AddTransient<IUrlEncoder, TestUrlEncoder>();
            });
            var client = server.CreateClient();
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/HtmlGenerationWebSite.HtmlGeneration_Home." + action + ".Encoded.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await client.GetAsync("http://localhost/HtmlGeneration_Home/" + action);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

            responseContent = responseContent.Trim();
            if (antiForgeryPath == null)
            {
#if GENERATE_BASELINES
                ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
                Assert.Equal(expectedContent.Trim(), responseContent);
#endif
            }
            else
            {
                var forgeryToken = AntiForgeryTestHelper.RetrieveAntiForgeryToken(responseContent, antiForgeryPath);
#if GENERATE_BASELINES
                // Reverse usual substitution and insert a format item into the new file content.
                responseContent = responseContent.Replace(forgeryToken, "{0}");
                ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
                expectedContent = string.Format(expectedContent, forgeryToken);
                Assert.Equal(expectedContent.Trim(), responseContent);
#endif
            }
        }

        [Fact]
        public async Task ValidationTagHelpers_GeneratesExpectedSpansAndDivs()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var outputFile = "compiler/resources/HtmlGenerationWebSite.HtmlGeneration_Customer.Index.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Customer/HtmlGeneration_Customer");
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("Number", string.Empty),
                new KeyValuePair<string,string>("Name", string.Empty),
                new KeyValuePair<string,string>("Email", string.Empty),
                new KeyValuePair<string,string>("PhoneNumber", string.Empty),
                new KeyValuePair<string,string>("Password", string.Empty)
            };
            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            responseContent = responseContent.Trim();
            var forgeryToken =
                AntiForgeryTestHelper.RetrieveAntiForgeryToken(responseContent, "Customer/HtmlGeneration_Customer");

#if GENERATE_BASELINES
            // Reverse usual substitution and insert a format item into the new file content.
            responseContent = responseContent.Replace(forgeryToken, "{0}");
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            expectedContent = string.Format(expectedContent, forgeryToken);
            Assert.Equal(expectedContent.Trim(), responseContent);
#endif
        }

        [Fact]
        public async Task CacheTagHelper_CanCachePortionsOfViewsPartialViewsAndViewComponents()
        {
            // Arrange
            var assertFile =
                "compiler/resources/CacheTagHelper_CanCachePortionsOfViewsPartialViewsAndViewComponents.Assert";
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost");
            client.DefaultRequestHeaders.Add("Locale", "North");

            var outputFile1 = assertFile + "1.txt";
            var expected1 =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile1, sourceFile: false);
            var outputFile2 = assertFile + "2.txt";
            var expected2 =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile2, sourceFile: false);
            var outputFile3 = assertFile + "3.txt";
            var expected3 =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile3, sourceFile: false);

            // Act - 1
            // Verify that content gets cached based on vary-by-params
            var targetUrl = "/catalog?categoryId=1&correlationid=1";
            var response1 = await client.GetStringAsync(targetUrl);
            var response2 = await client.GetStringAsync(targetUrl);

            // Assert - 1
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile1, expected1, response1.Trim());
#else
            Assert.Equal(expected1, response1.Trim());
            Assert.Equal(expected1, response2.Trim());
#endif

            // Act - 2
            // Verify content gets changed in partials when one of the vary by parameters is changed
            targetUrl = "/catalog?categoryId=3&correlationid=2";
            var response3 = await client.GetStringAsync(targetUrl);
            var response4 = await client.GetStringAsync(targetUrl);

            // Assert - 2
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile2, expected2, response3.Trim());
#else
            Assert.Equal(expected2, response3.Trim());
            Assert.Equal(expected2, response4.Trim());
#endif

            // Act - 3
            // Verify content gets changed in a View Component when the Vary-by-header parameters is changed
            client.DefaultRequestHeaders.Remove("Locale");
            client.DefaultRequestHeaders.Add("Locale", "East");

            targetUrl = "/catalog?categoryId=3&correlationid=3";
            var response5 = await client.GetStringAsync(targetUrl);
            var response6 = await client.GetStringAsync(targetUrl);

            // Assert - 3
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile3, expected3, response5.Trim());
#else
            Assert.Equal(expected3, response5.Trim());
            Assert.Equal(expected3, response6.Trim());
#endif
        }

        [Fact]
        public async Task CacheTagHelper_ExpiresContent_BasedOnExpiresParameter()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost");

            // Act - 1
            var response1 = await client.GetStringAsync("/catalog/2");

            // Assert - 1
            var expected1 = "Cached content for 2";
            Assert.Equal(expected1, response1.Trim());

            // Act - 2
            await Task.Delay(TimeSpan.FromSeconds(1));
            var response2 = await client.GetStringAsync("/catalog/3");

            // Assert - 2
            var expected2 = "Cached content for 3";
            Assert.Equal(expected2, response2.Trim());
        }

        [Fact]
        public async Task CacheTagHelper_UsesVaryByCookie_ToVaryContent()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost");

            // Act - 1
            var response1 = await client.GetStringAsync("/catalog/cart?correlationid=1");

            // Assert - 1
            var expected1 = "Cart content for 1";
            Assert.Equal(expected1, response1.Trim());

            // Act - 2
            client.DefaultRequestHeaders.Add("Cookie", "CartId=10");
            var response2 = await client.GetStringAsync("/catalog/cart?correlationid=2");

            // Assert - 2
            var expected2 = "Cart content for 2";
            Assert.Equal(expected2, response2.Trim());

            // Act - 3
            // Resend the cookiesless request and cached result from the first response.
            client.DefaultRequestHeaders.Remove("Cookie");
            var response3 = await client.GetStringAsync("/catalog/cart?correlationid=3");

            // Assert - 3
            Assert.Equal(expected1, response3.Trim());
        }

        [Fact]
        public async Task CacheTagHelper_VariesByRoute()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost");

            // Act - 1
            var response1 = await client.GetStringAsync(
                "/catalog/north-west/confirm-payment?confirmationId=1");

            // Assert - 1
            var expected1 = "Welcome Guest. Your confirmation id is 1. (Region north-west)";
            Assert.Equal(expected1, response1.Trim());

            // Act - 2
            var response2 = await client.GetStringAsync(
                "/catalog/south-central/confirm-payment?confirmationId=2");

            // Assert - 2
            var expected2 = "Welcome Guest. Your confirmation id is 2. (Region south-central)";
            Assert.Equal(expected2, response2.Trim());

            // Act 3
            var response3 = await client.GetStringAsync(
                "/catalog/north-west/Silver/confirm-payment?confirmationId=4");

            var expected3 = "Welcome Silver member. Your confirmation id is 4. (Region north-west)";
            Assert.Equal(expected3, response3.Trim());

            // Act 4
            var response4 = await client.GetStringAsync(
                "/catalog/north-west/Gold/confirm-payment?confirmationId=5");

            var expected4 = "Welcome Gold member. Your confirmation id is 5. (Region north-west)";
            Assert.Equal(expected4, response4.Trim());

            // Act - 4
            // Resend the responses and expect cached results.
            response1 = await client.GetStringAsync(
                "/catalog/north-west/confirm-payment?confirmationId=301");
            response2 = await client.GetStringAsync(
                "/catalog/south-central/confirm-payment?confirmationId=402");
            response3 = await client.GetStringAsync(
                "/catalog/north-west/Silver/confirm-payment?confirmationId=503");
            response4 = await client.GetStringAsync(
                "/catalog/north-west/Gold/confirm-payment?confirmationId=608");

            // Assert - 4
            Assert.Equal(expected1, response1.Trim());
            Assert.Equal(expected2, response2.Trim());
            Assert.Equal(expected3, response3.Trim());
            Assert.Equal(expected4, response4.Trim());
        }

        [Fact]
        public async Task CacheTagHelper_VariesByUserId()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost");

            // Act - 1
            var response1 = await client.GetStringAsync("/catalog/past-purchases/test1?correlationid=1");
            var response2 = await client.GetStringAsync("/catalog/past-purchases/test1?correlationid=2");

            // Assert - 1
            var expected1 = "Past purchases for user test1 (1)";
            Assert.Equal(expected1, response1.Trim());
            Assert.Equal(expected1, response2.Trim());

            // Act - 2
            var response3 = await client.GetStringAsync("/catalog/past-purchases/test2?correlationid=3");
            var response4 = await client.GetStringAsync("/catalog/past-purchases/test2?correlationid=4");

            // Assert - 2
            var expected2 = "Past purchases for user test2 (3)";
            Assert.Equal(expected2, response3.Trim());
            Assert.Equal(expected2, response4.Trim());
        }

        [Fact]
        public async Task CacheTagHelper_BubblesExpirationOfNestedTagHelpers()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost");

            // Act - 1
            var response1 = await client.GetStringAsync("/categories/Books?correlationId=1");

            // Assert - 1
            var expected1 =
@"Category: Books
Products: Book1, Book2 (1)";
            Assert.Equal(expected1, response1.Trim());

            // Act - 2
            var response2 = await client.GetStringAsync("/categories/Electronics?correlationId=2");

            // Assert - 2
            var expected2 =
@"Category: Electronics
Products: Book1, Book2 (1)";
            Assert.Equal(expected2, response2.Trim());

            // Act - 3
            // Trigger an expiration
            var response3 = await client.PostAsync("/categories/update-products", new StringContent(string.Empty));
            response3.EnsureSuccessStatusCode();

            var response4 = await client.GetStringAsync("/categories/Electronics?correlationId=3");

            // Assert - 3
            var expected3 =
@"Category: Electronics
Products: Laptops (3)";
            Assert.Equal(expected3, response4.Trim());
        }

        [Fact]
        public async Task CacheTagHelper_DoesNotCacheIfDisabled()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost");

            // Act
            var response1 = await client.GetStringAsync("/catalog/GetDealPercentage/20?isEnabled=true");
            var response2 = await client.GetStringAsync("/catalog/GetDealPercentage/40?isEnabled=true");
            var response3 = await client.GetStringAsync("/catalog/GetDealPercentage/30?isEnabled=false");

            // Assert
            Assert.Equal("Deal percentage is 20", response1.Trim());
            Assert.Equal("Deal percentage is 20", response2.Trim());
            Assert.Equal("Deal percentage is 30", response3.Trim());
        }

        [Theory]
        [InlineData(null)]
        [InlineData(true)]
        [InlineData(false)]
        public async Task FormTagHelper_GeneratesExpectedContent(bool? optionsAntiForgery)
        {
            // Arrange
            var newServices = new ServiceCollection();
            newServices.InitializeTagHelper<FormTagHelper>((helper, _) => helper.AntiForgery = optionsAntiForgery);
            var server = TestHelper.CreateServer(_app, SiteName,
                services =>
                {
                    services.Add(newServices);
                    _configureServices(services);
                });
            var client = server.CreateClient();
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");

            var outputFile = string.Format(
                "compiler/resources/HtmlGenerationWebSite.HtmlGeneration_Home.Form.Options.AntiForgery.{0}.html",
                optionsAntiForgery?.ToString() ?? "null");
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await client.GetAsync("http://localhost/HtmlGeneration_Home/Form");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

            responseContent = responseContent.Trim();
            var forgeryTokens = AntiForgeryTestHelper.RetrieveAntiForgeryTokens(responseContent).ToArray();

#if GENERATE_BASELINES
            // Reverse usual substitutions and insert format items into the new file content.
            for (var index = 0; index < forgeryTokens.Length; index++)
            {
                responseContent = responseContent.Replace(forgeryTokens[index], $"{{{ index }}}");
            }

            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            expectedContent = string.Format(expectedContent, forgeryTokens);
            Assert.Equal(expectedContent.Trim(), responseContent);
#endif
        }
    }
}