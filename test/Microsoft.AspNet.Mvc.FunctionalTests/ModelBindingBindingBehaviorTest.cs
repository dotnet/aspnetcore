// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using ModelBindingWebSite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ModelBindingBindingBehaviorTest
    {
        private const string SiteName = nameof(ModelBindingWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new Startup().ConfigureServices;

        [Fact]
        public async Task BindingBehavior_MissingRequiredProperties_ValidationErrors()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            var url = "http://localhost/BindingBehavior/EchoModelValues";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("model.BehaviourOptionalProperty", "Hi"),
            };

            request.Content = new FormUrlEncodedContent(formData);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var errors = JsonConvert.DeserializeObject<SerializableError>(body);

            Assert.Equal(2, errors.Count);

            var error = Assert.Single(errors, kvp => kvp.Key == "model.BehaviourRequiredProperty");
            Assert.Equal(
                "A value for the 'BehaviourRequiredProperty' property was not provided.",
                ((JArray)error.Value)[0].Value<string>());

            error = Assert.Single(errors, kvp => kvp.Key == "model.BindRequiredProperty");
            Assert.Equal(
                "A value for the 'BindRequiredProperty' property was not provided.",
                ((JArray)error.Value)[0].Value<string>());
        }

        [Fact]
        public async Task BindingBehavior_OptionalIsOptional()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            var url = "http://localhost/BindingBehavior/EchoModelValues";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("model.BehaviourRequiredProperty", "Hello"),
                new KeyValuePair<string, string>("model.BindRequiredProperty", "World!"),
            };

            request.Content = new FormUrlEncodedContent(formData);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var model = JsonConvert.DeserializeObject<BindingBehaviorModel>(body);

            Assert.Null(model.BehaviourNeverProperty);
            Assert.Null(model.BehaviourOptionalProperty);
            Assert.Equal("Hello", model.BehaviourRequiredProperty);
            Assert.Equal("World!", model.BindRequiredProperty);
            Assert.Null(model.BindNeverProperty);
        }

        [Fact]
        public async Task BindingBehavior_Never_IsNotBound()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            var url = "http://localhost/BindingBehavior/EchoModelValues";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var formData = new List<KeyValuePair<string, string>>
            {

                new KeyValuePair<string, string>("model.BehaviourNeverProperty", "Ignored"),
                new KeyValuePair<string, string>("model.BehaviourOptionalProperty", "Optional"),
                new KeyValuePair<string, string>("model.BehaviourRequiredProperty", "Hello"),
                new KeyValuePair<string, string>("model.BindRequiredProperty", "World!"),
                new KeyValuePair<string, string>("model.BindNeverProperty", "Ignored"),
            };

            request.Content = new FormUrlEncodedContent(formData);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var model = JsonConvert.DeserializeObject<BindingBehaviorModel>(body);

            Assert.Null(model.BehaviourNeverProperty);
            Assert.Equal("Optional", model.BehaviourOptionalProperty);
            Assert.Equal("Hello", model.BehaviourRequiredProperty);
            Assert.Equal("World!", model.BindRequiredProperty);
            Assert.Null(model.BindNeverProperty);
        }
    }
}