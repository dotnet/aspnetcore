// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class FileResultTests
    {
        private const string SiteName = nameof(FilesWebSite);
        private readonly Action<IApplicationBuilder> _app = new FilesWebSite.Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new FilesWebSite.Startup().ConfigureServices;

        [ConditionalTheory]
        // https://github.com/aspnet/Mvc/issues/2727
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task FileFromDisk_CanBeEnabled_WithMiddleware()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/DownloadFiles/DowloadFromDisk");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());

            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal("This is a sample text file", body);
        }

        [ConditionalTheory]
        // https://github.com/aspnet/Mvc/issues/2727
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task FileFromDisk_ReturnsFileWithFileName()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/DownloadFiles/DowloadFromDiskWithFileName");

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
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/DownloadFiles/DowloadFromStream");

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
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/DownloadFiles/DowloadFromStreamWithFileName");

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
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/DownloadFiles/DowloadFromBinaryData");

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
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/DownloadFiles/DowloadFromBinaryDataWithFileName");

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
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var expectedBody = "Sample text file as embedded resource.";

            // Act
            var response = await client.GetAsync("http://localhost/EmbeddedFiles/DownloadFileWithFileName");

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