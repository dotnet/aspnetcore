// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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
            var response = await Client.GetAsync("http://localhost/DownloadFiles/DownloadFromDisk");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());

            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal("This is a sample text file", body);
        }

        [Theory]
        [InlineData(0, 6, "This is")]
        [InlineData(17, 25, "text file")]
        [InlineData(0, 50, "This is a sample text file")]
        public async Task FileFromDisk_CanBeEnabled_WithMiddleware_RangeRequest(long start, long end, string expectedBody)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromDisk");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(start, end);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal(expectedBody, body);
        }

        [Theory]
        [InlineData("0-6")]
        [InlineData("bytes = ")]
        [InlineData("bytes = 1-4, 5-11")]
        public async Task FileFromDisk_CanBeEnabled_WithMiddleware_RangeRequestIgnored(string rangeString)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromDisk");
            httpRequestMessage.Headers.TryAddWithoutValidation("Range", rangeString);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("This is a sample text file", body);
        }

        [Theory]
        [InlineData("bytes = 35-36")]
        [InlineData("bytes = -0")]
        public async Task FileFromDisk_CanBeEnabled_WithMiddleware_RangeRequestNotSatisfiable(string rangeString)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromDisk");
            httpRequestMessage.Headers.TryAddWithoutValidation("Range", rangeString);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.Empty(body);
        }

        [Theory]
        [InlineData(0, 6, "This is")]
        [InlineData(17, 25, "text file")]
        [InlineData(0, 50, "This is a sample text file")]
        public async Task FileFromDisk_CanBeEnabled_WithMiddleware_RangeRequest_WithLastModifiedAndEtag(long start, long end, string expectedBody)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromDisk_WithLastModifiedAndEtag");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(start, end);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal(expectedBody, body);
        }

        [Theory]
        [InlineData("0-6")]
        [InlineData("bytes = ")]
        [InlineData("bytes = 1-4, 5-11")]
        public async Task FileFromDisk_CanBeEnabled_WithMiddleware_RangeRequestIgnored_WithLastModifiedAndEtag(string rangeString)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromDiskWithFileName_WithLastModifiedAndEtag");
            httpRequestMessage.Headers.TryAddWithoutValidation("Range", rangeString);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("This is a sample text file", body);
        }

        [Theory]
        [InlineData("bytes = 35-36")]
        [InlineData("bytes = -0")]
        public async Task FileFromDisk_CanBeEnabled_WithMiddleware_RangeRequestNotSatisfiable_WithLastModifiedAndEtag(string rangeString)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromDiskWithFileName_WithLastModifiedAndEtag");
            httpRequestMessage.Headers.TryAddWithoutValidation("Range", rangeString);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.Empty(body);
        }

        [ConditionalFact]
        // https://github.com/aspnet/Mvc/issues/2727
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task FileFromDisk_ReturnsFileWithFileName()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/DownloadFiles/DownloadFromDiskWithFileName");

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
        public async Task FileFromDisk_ReturnsFileWithFileName_RangeProcessingNotEnabled_RangeRequestedIgnored()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromDiskWithFileName");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(0, 6);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("This is a sample text file", body);
        }

        [Fact]
        public async Task FileFromDisk_ReturnsFileWithFileName_IfRangeHeaderValid_RangeRequest_WithLastModifiedAndEtag()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromDiskWithFileName_WithLastModifiedAndEtag");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(0, 6);
            httpRequestMessage.Headers.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"Etag\""));

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal("This is", body);
        }

        [Fact]
        public async Task FileFromDisk_ReturnsFileWithFileName_IfRangeHeaderInvalid_RangeRequestIgnored_WithLastModifiedAndEtag()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromDiskWithFileName_WithLastModifiedAndEtag");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(0, 6);
            httpRequestMessage.Headers.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"NotEtag\""));

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("This is a sample text file", body);
        }

        [Fact]
        public async Task FileFromStream_ReturnsFile()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/DownloadFiles/DownloadFromStream");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());

            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal("This is sample text from a stream", body);
        }

        [Theory]
        [InlineData(0, 6, "This is")]
        [InlineData(25, 32, "a stream")]
        [InlineData(0, 50, "This is sample text from a stream")]
        public async Task FileFromStream_ReturnsFile_RangeRequest(long start, long end, string expectedBody)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromStream");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(start, end);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
            Assert.Equal(expectedBody, body);
        }

        [Theory]
        [InlineData("0-6")]
        [InlineData("bytes = ")]
        [InlineData("bytes = 1-4, 5-11")]
        public async Task FileFromStream_ReturnsFile_RangeRequestIgnored(string rangeString)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromStream");
            httpRequestMessage.Headers.TryAddWithoutValidation("Range", rangeString);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("This is sample text from a stream", body);
        }

        [Theory]
        [InlineData("bytes = 35-36")]
        [InlineData("bytes = -0")]
        public async Task FileFromStream_ReturnsFile_RangeRequestNotSatisfiable(string rangeString)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromStream");
            httpRequestMessage.Headers.TryAddWithoutValidation("Range", rangeString);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);
            var body = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Empty(body);
        }

        [Fact]
        public async Task FileFromStream_ReturnsFileWithFileName()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/DownloadFiles/DownloadFromStreamWithFileName");

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
        public async Task FileFromStream_ReturnsFileWithFileName_RangeProcessingNotEnabled_RangeRequestedIgnored()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromStreamWithFileName");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(0, 6);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("This is sample text from a stream", body);
        }

        [Fact]
        public async Task FileFromStream_ReturnsFileWithFileName_IfRangeHeaderValid_RangeRequest()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromStreamWithFileName_WithEtag");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(0, 6);
            httpRequestMessage.Headers.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"Etag\""));

            // Act
            var response = await Client.SendAsync(httpRequestMessage);
            var body = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.NotNull(body);
            Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
            Assert.Equal("This is", body);
        }

        [Fact]
        public async Task FileFromStream_ReturnsFileWithFileName_IfRangeHeaderInvalid_RangeRequestNotSatisfiable()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromStreamWithFileName_WithEtag");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(0, 6);
            httpRequestMessage.Headers.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"NotEtag\""));

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("This is sample text from a stream", body);
        }

        [Fact]
        public async Task FileFromBinaryData_ReturnsFile()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/DownloadFiles/DownloadFromBinaryData");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());

            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal("This is a sample text from a binary array", body);
        }

        [Theory]
        [InlineData(0, 6, "This is")]
        [InlineData(29, 40, "binary array")]
        [InlineData(0, 50, "This is a sample text from a binary array")]
        public async Task FileFromBinaryData_ReturnsFile_RangeRequest(long start, long end, string expectedBody)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromBinaryData");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(start, end);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);
            var body = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.NotNull(body);
            Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
            Assert.Equal(expectedBody, body);
        }

        [Theory]
        [InlineData("0-6")]
        [InlineData("bytes = ")]
        [InlineData("bytes = 1-4, 5-11")]
        public async Task FileFromBinaryData_ReturnsFile_RangeRequestIgnored(string rangeString)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromBinaryData");
            httpRequestMessage.Headers.TryAddWithoutValidation("Range", rangeString);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("This is a sample text from a binary array", body);
        }

        [Theory]
        [InlineData("bytes = 45-46")]
        [InlineData("bytes = -0")]
        public async Task FileFromBinaryData_ReturnsFile_RangeRequestNotSatisfiable(string rangeString)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromBinaryData");
            httpRequestMessage.Headers.TryAddWithoutValidation("Range", rangeString);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);
            var body = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.NotNull(body);
            Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.Empty(body);
        }

        [Fact]
        public async Task FileFromBinaryData_ReturnsFileWithFileName()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/DownloadFiles/DownloadFromBinaryDataWithFileName");

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
        public async Task FileFromBinaryData_ReturnsFileWithFileName_RangeProcessingNotEnabled_RangeRequestedIgnored()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromBinaryDataWithFileName");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(0, 6);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("This is a sample text from a binary array", body);
        }

        [Fact]
        public async Task FileFromBinaryData_ReturnsFileWithFileName_IfRangeHeaderValid()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromBinaryDataWithFileName_WithEtag");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(0, 6);
            httpRequestMessage.Headers.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"Etag\""));

            // Act
            var response = await Client.SendAsync(httpRequestMessage);
            var body = await response.Content.ReadAsStringAsync();

            // Assert           
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            Assert.NotNull(body);
            Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
            Assert.Equal("This is", body);
        }

        [Fact]
        public async Task FileFromBinaryData_ReturnsFileWithFileName_IfRangeHeaderInvalid_RangeRequestIgnored()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/DownloadFiles/DownloadFromBinaryDataWithFileName_WithEtag");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(0, 6);
            httpRequestMessage.Headers.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"NotEtag\""));

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("This is a sample text from a binary array", body);
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

        [Theory]
        [InlineData(0, 6, "Sample ")]
        [InlineData(20, 37, "embedded resource.")]
        [InlineData(7, 50, "text file as embedded resource.")]
        public async Task FileFromEmbeddedResources_ReturnsFileWithFileName_RangeRequest(long start, long end, string expectedBody)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/EmbeddedFiles/DownloadFileWithFileName");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(start, end);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal(expectedBody, body);
            var contentDisposition = response.Content.Headers.ContentDisposition.ToString();
            Assert.NotNull(contentDisposition);
            Assert.Equal("attachment; filename=downloadName.txt; filename*=UTF-8''downloadName.txt", contentDisposition);
        }

        [Fact]
        public async Task FileFromEmbeddedResources_ReturnsFileWithFileName_RangeProcessingNotEnabled_RangeRequestedIgnored()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/EmbeddedFiles/DownloadFileWithFileName_RangeProcessingNotEnabled");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(0, 6);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Sample text file as embedded resource.", body);
        }

        [Fact]
        public async Task FileFromEmbeddedResources_ReturnsFileWithFileName_IfRangeHeaderValid_RangeRequest()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/EmbeddedFiles/DownloadFileWithFileName_WithEtag");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(0, 6);
            httpRequestMessage.Headers.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"Etag\""));

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);
            Assert.Equal("Sample ", body);
            var contentDisposition = response.Content.Headers.ContentDisposition.ToString();
            Assert.NotNull(contentDisposition);
            Assert.Equal("attachment; filename=downloadName.txt; filename*=UTF-8''downloadName.txt", contentDisposition);
        }

        [Fact]
        public async Task FileFromEmbeddedResources_ReturnsFileWithFileName_IfRangeHeaderInvalid_RangeRequestIgnored()
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/EmbeddedFiles/DownloadFileWithFileName_WithEtag");
            httpRequestMessage.Headers.Range = new RangeHeaderValue(0, 6);
            httpRequestMessage.Headers.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"NotEtag\""));

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Sample text file as embedded resource.", body);
            var contentDisposition = response.Content.Headers.ContentDisposition.ToString();
            Assert.NotNull(contentDisposition);
            Assert.Equal("attachment; filename=downloadName.txt; filename*=UTF-8''downloadName.txt", contentDisposition);
        }

        [Theory]
        [InlineData("0-6")]
        [InlineData("bytes = ")]
        [InlineData("bytes = 1-4, 5-11")]
        public async Task FileFromEmbeddedResources_ReturnsFileWithFileName_RangeRequestIgnored(string rangeString)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/EmbeddedFiles/DownloadFileWithFileName");
            httpRequestMessage.Headers.TryAddWithoutValidation("Range", rangeString);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Sample text file as embedded resource.", body);
            var contentDisposition = response.Content.Headers.ContentDisposition.ToString();
            Assert.NotNull(contentDisposition);
            Assert.Equal("attachment; filename=downloadName.txt; filename*=UTF-8''downloadName.txt", contentDisposition);
        }

        [Theory]
        [InlineData("bytes = 45-46")]
        [InlineData("bytes = -0")]
        public async Task FileFromEmbeddedResources_ReturnsFileWithFileName_RangeRequestNotSatisfiable(string rangeString)
        {
            // Arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost/EmbeddedFiles/DownloadFileWithFileName");
            httpRequestMessage.Headers.TryAddWithoutValidation("Range", rangeString);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
            var body = await response.Content.ReadAsStringAsync();
            Assert.Empty(body);
            var contentDisposition = response.Content.Headers.ContentDisposition.ToString();
            Assert.NotNull(contentDisposition);
            Assert.Equal("attachment; filename=downloadName.txt; filename*=UTF-8''downloadName.txt", contentDisposition);
        }
    }
}
