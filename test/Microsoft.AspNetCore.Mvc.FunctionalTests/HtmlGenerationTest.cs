// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class HtmlGenerationTest :
        IClassFixture<MvcTestFixture<HtmlGenerationWebSite.Startup>>,
        IClassFixture<MvcEncodedTestFixture<HtmlGenerationWebSite.Startup>>
    {
        private static readonly Assembly _resourcesAssembly = typeof(HtmlGenerationTest).GetTypeInfo().Assembly;

        public HtmlGenerationTest(
            MvcTestFixture<HtmlGenerationWebSite.Startup> fixture,
            MvcEncodedTestFixture<HtmlGenerationWebSite.Startup> encodedFixture)
        {
            Client = fixture.Client;
            EncodedClient = encodedFixture.Client;
        }

        public HttpClient Client { get; }

        public HttpClient EncodedClient { get; }

        public static TheoryData<string, string> WebPagesData
        {
            get
            {
                var data = new TheoryData<string, string>
                {
                    { "Customer", "/Customer/HtmlGeneration_Customer" },
                    { "Index", null },
                    { "Product", null },
                    // Testing attribute values with boolean and null values
                    { "AttributesWithBooleanValues", null },
                    // Testing SelectTagHelper with Html.BeginForm
                    { "CreateWarehouse", "/HtmlGeneration_Home/CreateWarehouse" },
                    // Testing the HTML helpers with FormTagHelper
                    { "EditWarehouse", null },
                    { "Form", "/HtmlGeneration_Home/Form" },
                    // Testing MVC tag helpers invoked in the editor templates from HTML helpers
                    { "EmployeeList", "/HtmlGeneration_Home/EmployeeList" },
                    // Testing the EnvironmentTagHelper
                    { "Environment", null },
                    // Testing the ImageTagHelper
                    { "Image", null },
                    // Testing InputTagHelper with File
                    { "Input", null },
                    // Testing the LinkTagHelper
                    { "Link", null },
                    // Test ability to generate nearly identical HTML with MVC tag and HTML helpers.
                    // Only attribute order should differ.
                    { "Order", "/HtmlGeneration_Order/Submit" },
                    { "OrderUsingHtmlHelpers", "/HtmlGeneration_Order/Submit" },
                    // Testing PartialTagHelper
                    { "PartialTagHelperWithoutModel", null },
                    { "Warehouse", null },
                    // Testing InputTagHelpers invoked in the partial views
                    { "ProductList", "/HtmlGeneration_Product" },
                    { "ProductListUsingTagHelpers", "/HtmlGeneration_Product" },
                    // Testing the ScriptTagHelper
                    { "Script", null },
                };

                return data;
            }
        }

        [Fact]
        public async Task EnumValues_SerializeCorrectly()
        {
            // Arrange & Act
            var response = await Client.GetStringAsync("http://localhost/HtmlGeneration_Home/Enum");

            // Assert
            Assert.Equal($"Vrijdag{Environment.NewLine}Month: FirstOne", response, ignoreLineEndingDifferences: true);
        }

        [Theory]
        [MemberData(nameof(WebPagesData))]
        public async Task HtmlGenerationWebSite_GeneratesExpectedResults(string action, string antiforgeryPath)
        {
            // Arrange
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/HtmlGenerationWebSite.HtmlGeneration_Home." + action + ".html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await Client.GetAsync("http://localhost/HtmlGeneration_Home/" + action);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

            responseContent = responseContent.Trim();
            if (antiforgeryPath == null)
            {
#if GENERATE_BASELINES
                ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
                Assert.Equal(expectedContent.Trim(), responseContent, ignoreLineEndingDifferences: true);
#endif
            }
            else
            {
                var forgeryToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(responseContent, antiforgeryPath);
#if GENERATE_BASELINES
                // Reverse usual substitution and insert a format item into the new file content.
                responseContent = responseContent.Replace(forgeryToken, "{0}");
                ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
                expectedContent = string.Format(expectedContent, forgeryToken);
                Assert.Equal(expectedContent.Trim(), responseContent, ignoreLineEndingDifferences: true);
#endif
            }
        }

        public static TheoryData<string, string> EncodedPagesData
        {
            get
            {
                return new TheoryData<string, string>
                {
                    { "AttributesWithBooleanValues", null },
                    { "EditWarehouse", null },
                    { "Index", null },
                    { "Link", null },
                    { "Order", "/HtmlGeneration_Order/Submit" },
                    { "OrderUsingHtmlHelpers", "/HtmlGeneration_Order/Submit" },
                    { "Product", null },
                    { "Script", null },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EncodedPagesData))]
        public async Task HtmlGenerationWebSite_GenerateEncodedResults(string action, string antiforgeryPath)
        {
            // Arrange
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/HtmlGenerationWebSite.HtmlGeneration_Home." + action + ".Encoded.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await EncodedClient.GetAsync("http://localhost/HtmlGeneration_Home/" + action);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

            responseContent = responseContent.Trim();
            if (antiforgeryPath == null)
            {
#if GENERATE_BASELINES
                ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
                Assert.Equal(
                    expectedContent.Trim(),
                    responseContent,
                    ignoreLineEndingDifferences: true);
#endif
            }
            else
            {
                var forgeryToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(responseContent, antiforgeryPath);
#if GENERATE_BASELINES
                // Reverse usual substitution and insert a format item into the new file content.
                responseContent = responseContent.Replace(forgeryToken, "{0}");
                ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
                expectedContent = string.Format(expectedContent, forgeryToken);
                Assert.Equal(expectedContent.Trim(), responseContent, ignoreLineEndingDifferences: true);
#endif
            }
        }

        // Testing how ModelMetadata is handled as ViewDataDictionary instances are created.
        [Theory]
        [InlineData("AtViewModel")]
        [InlineData("NullViewModel")]
        [InlineData("ViewModel")]
        public async Task CheckViewData_GeneratesExpectedResults(string action)
        {
            // Arrange
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/HtmlGenerationWebSite.CheckViewData." + action + ".html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            var response = await Client.GetAsync("http://localhost/CheckViewData/" + action);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

            responseContent = responseContent.Trim();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent, ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task ValidationTagHelpers_GeneratesExpectedSpansAndDivs()
        {
            // Arrange
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
            var response = await Client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            responseContent = responseContent.Trim();
            var forgeryToken =
                AntiforgeryTestHelper.RetrieveAntiforgeryToken(responseContent, "Customer/HtmlGeneration_Customer");

#if GENERATE_BASELINES
            // Reverse usual substitution and insert a format item into the new file content.
            responseContent = responseContent.Replace(forgeryToken, "{0}");
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            expectedContent = string.Format(expectedContent, forgeryToken);
            Assert.Equal(expectedContent.Trim(), responseContent, ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task CacheTagHelper_CanCachePortionsOfViewsPartialViewsAndViewComponents()
        {
            // Arrange
            var assertFile =
                "compiler/resources/CacheTagHelper_CanCachePortionsOfViewsPartialViewsAndViewComponents.Assert";

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
            var request = RequestWithLocale(targetUrl, "North");
            var response1 = await (await Client.SendAsync(request)).Content.ReadAsStringAsync();
            request = RequestWithLocale(targetUrl, "North");
            var response2 = await (await Client.SendAsync(request)).Content.ReadAsStringAsync();

            // Assert - 1
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile1, expected1, response1.Trim());
#else
            Assert.Equal(expected1, response1.Trim(), ignoreLineEndingDifferences: true);
            Assert.Equal(expected1, response2.Trim(), ignoreLineEndingDifferences: true);
#endif

            // Act - 2
            // Verify content gets changed in partials when one of the vary by parameters is changed
            targetUrl = "/catalog?categoryId=3&correlationid=2";
            request = RequestWithLocale(targetUrl, "North");
            var response3 = await (await Client.SendAsync(request)).Content.ReadAsStringAsync();
            request = RequestWithLocale(targetUrl, "North");
            var response4 = await (await Client.SendAsync(request)).Content.ReadAsStringAsync();

            // Assert - 2
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile2, expected2, response3.Trim());
#else
            Assert.Equal(expected2, response3.Trim(), ignoreLineEndingDifferences: true);
            Assert.Equal(expected2, response4.Trim(), ignoreLineEndingDifferences: true);
#endif

            // Act - 3
            // Verify content gets changed in a View Component when the Vary-by-header parameters is changed
            targetUrl = "/catalog?categoryId=3&correlationid=3";
            request = RequestWithLocale(targetUrl, "East");
            var response5 = await (await Client.SendAsync(request)).Content.ReadAsStringAsync();
            request = RequestWithLocale(targetUrl, "East");
            var response6 = await (await Client.SendAsync(request)).Content.ReadAsStringAsync();

            // Assert - 3
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile3, expected3, response5.Trim());
#else
            Assert.Equal(expected3, response5.Trim(), ignoreLineEndingDifferences: true);
            Assert.Equal(expected3, response6.Trim(), ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task CacheTagHelper_ExpiresContent_BasedOnExpiresParameter()
        {
            // Arrange & Act - 1
            var response1 = await Client.GetStringAsync("/catalog/2");

            // Assert - 1
            var expected1 = "Cached content for 2";
            Assert.Equal(expected1, response1.Trim());

            // Act - 2
            await Task.Delay(TimeSpan.FromSeconds(2));
            var response2 = await Client.GetStringAsync("/catalog/3");

            // Assert - 2
            var expected2 = "Cached content for 3";
            Assert.Equal(expected2, response2.Trim());
        }

        [Fact]
        public async Task CacheTagHelper_UsesVaryByCookie_ToVaryContent()
        {
            // Arrange & Act - 1
            var response1 = await Client.GetStringAsync("/catalog/cart?correlationid=1");

            // Assert - 1
            var expected1 = "Cart content for 1";
            Assert.Equal(expected1, response1.Trim());

            // Act - 2
            var request = new HttpRequestMessage(HttpMethod.Get, "/catalog/cart?correlationid=2");
            request.Headers.Add("Cookie", "CartId=10");
            var response2 = await (await Client.SendAsync(request)).Content.ReadAsStringAsync();

            // Assert - 2
            var expected2 = "Cart content for 2";
            Assert.Equal(expected2, response2.Trim());

            // Act - 3
            // Resend the cookiesless request and cached result from the first response.
            var response3 = await Client.GetStringAsync("/catalog/cart?correlationid=3");

            // Assert - 3
            Assert.Equal(expected1, response3.Trim());
        }

        [Fact]
        public async Task CacheTagHelper_VariesByRoute()
        {
            // Arrange & Act - 1
            var response1 = await Client.GetStringAsync(
                "/catalog/north-west/confirm-payment?confirmationId=1");

            // Assert - 1
            var expected1 = "Welcome Guest. Your confirmation id is 1. (Region north-west)";
            Assert.Equal(expected1, response1.Trim());

            // Act - 2
            var response2 = await Client.GetStringAsync(
                "/catalog/south-central/confirm-payment?confirmationId=2");

            // Assert - 2
            var expected2 = "Welcome Guest. Your confirmation id is 2. (Region south-central)";
            Assert.Equal(expected2, response2.Trim());

            // Act 3
            var response3 = await Client.GetStringAsync(
                "/catalog/north-west/Silver/confirm-payment?confirmationId=4");

            var expected3 = "Welcome Silver member. Your confirmation id is 4. (Region north-west)";
            Assert.Equal(expected3, response3.Trim());

            // Act 4
            var response4 = await Client.GetStringAsync(
                "/catalog/north-west/Gold/confirm-payment?confirmationId=5");

            var expected4 = "Welcome Gold member. Your confirmation id is 5. (Region north-west)";
            Assert.Equal(expected4, response4.Trim());

            // Act - 4
            // Resend the responses and expect cached results.
            response1 = await Client.GetStringAsync(
                "/catalog/north-west/confirm-payment?confirmationId=301");
            response2 = await Client.GetStringAsync(
                "/catalog/south-central/confirm-payment?confirmationId=402");
            response3 = await Client.GetStringAsync(
                "/catalog/north-west/Silver/confirm-payment?confirmationId=503");
            response4 = await Client.GetStringAsync(
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
            // Arrange & Act - 1
            var response1 = await Client.GetStringAsync("/catalog/past-purchases/test1?correlationid=1");
            var response2 = await Client.GetStringAsync("/catalog/past-purchases/test1?correlationid=2");

            // Assert - 1
            var expected1 = "Past purchases for user test1 (1)";
            Assert.Equal(expected1, response1.Trim());
            Assert.Equal(expected1, response2.Trim());

            // Act - 2
            var response3 = await Client.GetStringAsync("/catalog/past-purchases/test2?correlationid=3");
            var response4 = await Client.GetStringAsync("/catalog/past-purchases/test2?correlationid=4");

            // Assert - 2
            var expected2 = "Past purchases for user test2 (3)";
            Assert.Equal(expected2, response3.Trim());
            Assert.Equal(expected2, response4.Trim());
        }

        [Fact]
        public async Task CacheTagHelper_BubblesExpirationOfNestedTagHelpers()
        {
            // Arrange & Act - 1
            var response1 = await Client.GetStringAsync("/categories/Books?correlationId=1");

            // Assert - 1
            var expected1 =
@"Category: Books
Products: Book1, Book2 (1)";
            Assert.Equal(expected1, response1.Trim(), ignoreLineEndingDifferences: true);

            // Act - 2
            var response2 = await Client.GetStringAsync("/categories/Electronics?correlationId=2");

            // Assert - 2
            var expected2 =
@"Category: Electronics
Products: Book1, Book2 (1)";
            Assert.Equal(expected2, response2.Trim(), ignoreLineEndingDifferences: true);

            // Act - 3
            // Trigger an expiration of the nested content.
            var content = @"[{ productName: ""Music Systems"" },{ productName: ""Televisions"" }]";
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/categories/Electronics");
            requestMessage.Content = new StringContent(content, Encoding.UTF8, "application/json");
            (await Client.SendAsync(requestMessage)).EnsureSuccessStatusCode();

            var response3 = await Client.GetStringAsync("/categories/Electronics?correlationId=3");

            // Assert - 3
            var expected3 =
@"Category: Electronics
Products: Music Systems, Televisions (3)";
            Assert.Equal(expected3, response3.Trim(), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task CacheTagHelper_DoesNotCacheIfDisabled()
        {
            // Arrange & Act
            var response1 = await Client.GetStringAsync("/catalog/GetDealPercentage/20?isEnabled=true");
            var response2 = await Client.GetStringAsync("/catalog/GetDealPercentage/40?isEnabled=true");
            var response3 = await Client.GetStringAsync("/catalog/GetDealPercentage/30?isEnabled=false");

            // Assert
            Assert.Equal("Deal percentage is 20", response1.Trim());
            Assert.Equal("Deal percentage is 20", response2.Trim());
            Assert.Equal("Deal percentage is 30", response3.Trim());
        }

        [Fact]
        public async Task EditorTemplateWithNoModel_RendersWithCorrectMetadata()
        {
            // Arrange
            var expected =
                "<label class=\"control-label col-md-2\" for=\"Name\">ItemName</label>" + Environment.NewLine +
                "<input id=\"Name\" name=\"Name\" type=\"text\" value=\"\" />" + Environment.NewLine + Environment.NewLine +
                "<label class=\"control-label col-md-2\" for=\"Id\">ItemNo</label>" + Environment.NewLine +
                "<input data-val=\"true\" data-val-required=\"The ItemNo field is required.\" id=\"Id\" name=\"Id\" type=\"text\" value=\"\" />" +
                Environment.NewLine + Environment.NewLine;

            // Act
            var response = await Client.GetStringAsync("http://localhost/HtmlGeneration_Home/ItemUsingSharedEditorTemplate");

            // Assert
            Assert.Equal(expected, response, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task EditorTemplateWithSpecificModel_RendersWithCorrectMetadata()
        {
            // Arrange
            var expected = "<label for=\"Description\">ItemDesc</label>" + Environment.NewLine +
                "<input id=\"Description\" name=\"Description\" type=\"text\" value=\"\" />" + Environment.NewLine + Environment.NewLine;

            // Act
            var response = await Client.GetStringAsync("http://localhost/HtmlGeneration_Home/ItemUsingModelSpecificEditorTemplate");

            // Assert
            Assert.Equal(expected, response, ignoreLineEndingDifferences: true);
        }

        // We want to make sure that for 'weird' model expressions involving:
        // - fields
        // - statics
        // - private
        //
        // These tests verify that we don't throw, and can evaluate the expression to get the model
        // value. One quirk of behavior for these cases is that we can't return a correct model metadata
        // instance (this is true for anything other than a public instance property). We're not overly
        // concerned with that, and so the accuracy of the model metadata is not verified by the test.
        [Theory]
        [InlineData("GetWeirdWithHtmlHelpers")]
        [InlineData("GetWeirdWithTagHelpers")]
        public async Task WeirdModelExpressions_CanAccessModelValues(string action)
        {
            // Arrange
            var url = "http://localhost/HtmlGeneration_WeirdExpressions/" + action;

            // Act
            var response = await Client.GetStringAsync(url);

            // Assert
            Assert.Contains("Hello, Field World!", response);
            Assert.Contains("Hello, Static World!", response);
            Assert.Contains("Hello, Private World!", response);
        }

        private static HttpRequestMessage RequestWithLocale(string url, string locale)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Locale", locale);

            return request;
        }
    }
}
