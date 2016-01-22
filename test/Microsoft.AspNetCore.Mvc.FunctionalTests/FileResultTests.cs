// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class FileResultTests : IClassFixture<MvcTestFixture<FilesWebSite.Startup>>
    {
        public FileResultTests(MvcTestFixture<FilesWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [ConditionalFact]
        // https://github.com/aspnet/Mvc/issues/2727
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task FileFromDisk_CanBeEnabled_WithMiddleware()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/DownloadFiles/DowloadFromDisk");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());

            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal("This is a sample text file", body);
        }

        [ConditionalFact]
        // https://github.com/aspnet/Mvc/issues/2727
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task FileFromDisk_ReturnsFileWithFileName()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/DownloadFiles/DowloadFromDiskWithFileName");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());

            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal("This is a sample text file", body);

            var contentDisposition = response.Content.Headers.ContentDisposition.ToString();
            Assert.NotNull(contentDisposition);
            Assert.Equal("attachment; filename=downloadName.txt; filename*=UTF-8''downloadName.txt", contentDisposition);
        }

        [Fact]
        public async Task FileFromStream_ReturnsFile()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/DownloadFiles/DowloadFromStream");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());

            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal("This is sample text from a stream", body);
        }

        [Fact]
        public async Task FileFromStream_ReturnsFileWithFileName()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/DownloadFiles/DowloadFromStreamWithFileName");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());

            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal("This is sample text from a stream", body);

            var contentDisposition = response.Content.Headers.ContentDisposition.ToString();
            Assert.NotNull(contentDisposition);
            Assert.Equal("attachment; filename=downloadName.txt; filename*=UTF-8''downloadName.txt", contentDisposition);
        }

        [Fact]
        public async Task FileFromBinaryData_ReturnsFile()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/DownloadFiles/DowloadFromBinaryData");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());

            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal("This is a sample text from a binary array", body);
        }

        [Fact]
        public async Task FileFromBinaryData_ReturnsFileWithFileName()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/DownloadFiles/DowloadFromBinaryDataWithFileName");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());

            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal("This is a sample text from a binary array", body);

            var contentDisposition = response.Content.Headers.ContentDisposition.ToString();
            Assert.NotNull(contentDisposition);
            Assert.Equal("attachment; filename=downloadName.txt; filename*=UTF-8''downloadName.txt", contentDisposition);
        }

        [Fact]
        public async Task FileFromEmbeddedResources_ReturnsFileWithFileName()
        {
            // Arrange
            var expectedBody = "Sample text file as embedded resource.";

            // Act
            var response = await Client.GetAsync("http://localhost/EmbeddedFiles/DownloadFileWithFileName");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());

            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal(expectedBody, body);

            var contentDisposition = response.Content.Headers.ContentDisposition.ToString();
            Assert.NotNull(contentDisposition);
            Assert.Equal("attachment; filename=downloadName.txt; filename*=UTF-8''downloadName.txt", contentDisposition);
        }
    }
}