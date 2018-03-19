// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class FormFileUploadTest : IClassFixture<MvcTestFixture<FilesWebSite.Startup>>
    {
        public FormFileUploadTest(MvcTestFixture<FilesWebSite.Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task CanUploadFileInFrom()
        {
            // Arrange
            var content = new MultipartFormDataContent();
            content.Add(new StringContent("John"), "Name");
            content.Add(new StringContent("23"), "Age");
            content.Add(new StringContent("John's biography content"), "Biography", "Bio.txt");

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/UploadFiles");
            request.Content = content;

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var user = await response.Content.ReadAsAsync<User>();

            Assert.Equal("John", user.Name);
            Assert.Equal(23, user.Age);
            Assert.Equal("John's biography content", user.Biography);
        }

        [Fact]
        public async Task UploadMultipleFiles()
        {
            // Arrange
            var content = new MultipartFormDataContent();
            content.Add(new StringContent("Phone"), "Name");
            content.Add(new StringContent("camera"), "Specs[0].Key");
            content.Add(new StringContent("camera spec1 file contents"), "Specs[0].Value", "camera_spec1.txt");
            content.Add(new StringContent("camera spec2 file contents"), "Specs[0].Value", "camera_spec2.txt");
            content.Add(new StringContent("battery"), "Specs[1].Key");
            content.Add(new StringContent("battery spec1 file contents"), "Specs[1].Value", "battery_spec1.txt");
            content.Add(new StringContent("battery spec2 file contents"), "Specs[1].Value", "battery_spec2.txt");

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/UploadProductSpecs");
            request.Content = content;

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var product = await response.Content.ReadAsAsync<Product>();
            Assert.NotNull(product);
            Assert.Equal("Phone", product.Name);
            Assert.NotNull(product.Specs);
            Assert.Equal(2, product.Specs.Count);
            Assert.True(product.Specs.ContainsKey("camera"));
            Assert.Equal(new[] { "camera_spec1.txt", "camera_spec2.txt" }, product.Specs["camera"]);
            Assert.True(product.Specs.ContainsKey("battery"));
            Assert.Equal(new[] { "battery_spec1.txt", "battery_spec2.txt" }, product.Specs["battery"]);
        }

        private class User
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string Biography { get; set; }
        }

        private class Product
        {
            public string Name { get; set; }

            public Dictionary<string, List<string>> Specs { get; set; }
        }
    }
}
