// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class RequestFormLimitsTest : IClassFixture<MvcTestFixture<BasicWebSite.StartupRequestLimitSize>>
    {
        public RequestFormLimitsTest(MvcTestFixture<BasicWebSite.StartupRequestLimitSize> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task RequestFormLimitCheckHappens_BeforeAntiforgeryTokenValidation()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var kvps = new List<KeyValuePair<string, string>>();
            // Controller has value count limit of 2
            kvps.Add(new KeyValuePair<string, string>("key1", "value1"));
            kvps.Add(new KeyValuePair<string, string>("key2", "value2"));
            kvps.Add(new KeyValuePair<string, string>("key3", "value3"));
            kvps.Add(new KeyValuePair<string, string>("RequestVerificationToken", "invalid-data"));

            // Act
            var response = await Client.PostAsync(
                "RequestFormLimits/RequestFormLimitsBeforeAntiforgeryValidation",
                new FormUrlEncodedContent(kvps));

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var result = await response.Content.ReadAsStringAsync();
            Assert.Contains(
                "InvalidDataException: Form value count limit 2 exceeded.",
                result);
        }

        [Fact]
        public async Task OverridesControllerLevelLimits()
        {
            // Arrange
            var expected = "{\"sampleInt\":10,\"sampleString\":null}";
            var request = new HttpRequestMessage();
            var kvps = new List<KeyValuePair<string, string>>();
            // Controller has a value count limit of 2, but the action has a limit of 5
            kvps.Add(new KeyValuePair<string, string>("key1", "value1"));
            kvps.Add(new KeyValuePair<string, string>("key2", "value2"));
            kvps.Add(new KeyValuePair<string, string>("SampleInt", "10"));

            // Act
            var response = await Client.PostAsync(
                "RequestFormLimits/OverrideControllerLevelLimits",
                new FormUrlEncodedContent(kvps));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, result);
        }
        
        [Fact]
        public async Task OverrideControllerLevelLimits_UsingDefaultLimits()
        {
            // Arrange
            var expected = "{\"sampleInt\":50,\"sampleString\":null}";
            var request = new HttpRequestMessage();
            var kvps = new List<KeyValuePair<string, string>>();
            // Controller has a key limit of 2, but the action has default limits
            for (var i = 0; i < 10; i++)
            {
                kvps.Add(new KeyValuePair<string, string>($"key{i}", $"value{i}"));
            }
            kvps.Add(new KeyValuePair<string, string>("SampleInt", "50"));
            kvps.Add(new KeyValuePair<string, string>("RequestVerificationToken", "invalid-data"));

            // Act
            var response = await Client.PostAsync(
                "RequestFormLimits/OverrideControllerLevelLimitsUsingDefaultLimits",
                new FormUrlEncodedContent(kvps));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task RequestSizeLimitCheckHappens_BeforeRequestFormLimits()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var kvps = new List<KeyValuePair<string, string>>();
            // Request size has a limit of 100 bytes
            // Request form limits has a value count limit of 2
            // Antiforgery validation is also present
            kvps.Add(new KeyValuePair<string, string>("key1", new string('a', 1024)));
            kvps.Add(new KeyValuePair<string, string>("key2", "value2"));
            kvps.Add(new KeyValuePair<string, string>("RequestVerificationToken", "invalid-data"));

            // Act
            var response = await Client.PostAsync(
                "RequestFormLimits/RequestSizeLimitBeforeRequestFormLimits",
                new FormUrlEncodedContent(kvps));

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var result = await response.Content.ReadAsStringAsync();
            Assert.Contains(
                "InvalidOperationException: Request content size is greater than the limit size",
                result);
        }

        [Fact]
        public async Task RequestFormLimitsCheckHappens_AfterRequestSizeLimit()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var kvps = new List<KeyValuePair<string, string>>();
            // Request size has a limit of 100 bytes
            // Request form limits has a value count limit of 2
            // Antiforgery validation is also present
            kvps.Add(new KeyValuePair<string, string>("key1", "value1"));
            kvps.Add(new KeyValuePair<string, string>("key1", "value2"));
            kvps.Add(new KeyValuePair<string, string>("key1", "value3"));
            kvps.Add(new KeyValuePair<string, string>("RequestVerificationToken", "invalid-data"));

            // Act
            var response = await Client.PostAsync(
                "RequestFormLimits/RequestSizeLimitBeforeRequestFormLimits",
                new FormUrlEncodedContent(kvps));

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var result = await response.Content.ReadAsStringAsync();
            Assert.Contains(
                "InvalidDataException: Form value count limit 2 exceeded.",
                result);
        }

        [Fact]
        public async Task AntiforgeryValidationHappens_AfterRequestFormAndSizeLimitCheck()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var kvps = new List<KeyValuePair<string, string>>();
            // Request size has a limit of 100 bytes
            // Request form limits has a value count limit of 2
            // Antiforgery validation is also present
            kvps.Add(new KeyValuePair<string, string>("key1", "value1"));
            kvps.Add(new KeyValuePair<string, string>("RequestVerificationToken", "invalid-data"));

            // Act
            var response = await Client.PostAsync(
                "RequestFormLimits/RequestSizeLimitBeforeRequestFormLimits",
                new FormUrlEncodedContent(kvps));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
