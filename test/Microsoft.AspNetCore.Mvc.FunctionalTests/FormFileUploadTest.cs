// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            Client = fixture.Client;
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

        private class User
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string Biography { get; set; }
        }
    }
}
