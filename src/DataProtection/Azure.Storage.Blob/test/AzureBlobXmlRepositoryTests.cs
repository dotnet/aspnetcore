// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.Azure.Storage.Blob.Test
{
    public class AzureBlobXmlRepositoryTests
    {
        [Fact]
        public void StoreCreatesBlobWhenNotExist()
        {
            BlobRequestConditions uploadConditions = null;
            byte[] bytes = null;
            string contentType = null;

            var mock = new Mock<BlobClient>();

            mock.Setup(c => c.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobHttpHeaders>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<BlobRequestConditions>(),
                    It.IsAny<IProgress<long>>(),
                    It.IsAny<AccessTier?>(),
                    It.IsAny<StorageTransferOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(async (Stream strm, BlobHttpHeaders headers, IDictionary<string, string> metaData, BlobRequestConditions conditions, IProgress<long> progress, AccessTier? access, StorageTransferOptions transfer, CancellationToken token) =>
                {
                    using var memoryStream = new MemoryStream();
                    strm.CopyTo(memoryStream);
                    bytes = memoryStream.ToArray();
                    uploadConditions = conditions;
                    contentType = headers?.ContentType;

                    await Task.Yield();

                    var mockResponse = new Mock<Response<BlobContentInfo>>();
                    var mockContentInfo = new Mock<BlobContentInfo>();
                    mockContentInfo.Setup(c => c.ETag).Returns(ETag.All);
                    mockResponse.Setup(c => c.Value).Returns(mockContentInfo.Object);
                    return mockResponse.Object;
                });

            var repository = new AzureBlobXmlRepository(() => mock.Object);
            repository.StoreElement(new XElement("Element"), null);

            Assert.Equal("*", uploadConditions.IfNoneMatch.ToString());
            Assert.Equal("application/xml; charset=utf-8", contentType);
            var element = "<Element />";

            Assert.Equal(bytes, GetEnvelopedContent(element));
        }

        [Fact]
        public void StoreUpdatesWhenExistsAndNewerExists()
        {
            byte[] bytes = null;

            var mock = new Mock<BlobClient>();
            mock.Setup(c => c.GetPropertiesAsync(
                    It.IsAny<BlobRequestConditions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(async (BlobRequestConditions conditions, CancellationToken token) =>
                {
                    var mockResponse = new Mock<Response<BlobProperties>>();
                    mockResponse.Setup(c => c.Value).Returns(new BlobProperties());

                    await Task.Yield();
                    return mockResponse.Object;
                });

            mock.Setup(c => c.DownloadToAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobRequestConditions>(),
                    It.IsAny<StorageTransferOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(async (Stream target, BlobRequestConditions conditions, StorageTransferOptions options, CancellationToken token) =>
                {
                    var data = GetEnvelopedContent("<Element1 />");
                    await target.WriteAsync(data, 0, data.Length);

                    return new Mock<Response>().Object;
                })
                .Verifiable();

            mock.Setup(c => c.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobHttpHeaders>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.Is((BlobRequestConditions conditions) => conditions.IfNoneMatch == ETag.All),
                    It.IsAny<IProgress<long>>(),
                    It.IsAny<AccessTier?>(),
                    It.IsAny<StorageTransferOptions>(),
                    It.IsAny<CancellationToken>()))
                .Throws(new RequestFailedException(status: 412, message: ""))
                .Verifiable();

            mock.Setup(c => c.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobHttpHeaders>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.Is((BlobRequestConditions conditions) => conditions.IfNoneMatch != ETag.All),
                    It.IsAny<IProgress<long>>(),
                    It.IsAny<AccessTier?>(),
                    It.IsAny<StorageTransferOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(async (Stream strm, BlobHttpHeaders headers, IDictionary<string, string> metaData, BlobRequestConditions conditions, IProgress<long> progress, AccessTier? access, StorageTransferOptions transfer, CancellationToken token) =>
                {
                    using var memoryStream = new MemoryStream();
                    strm.CopyTo(memoryStream);
                    bytes = memoryStream.ToArray();

                    await Task.Yield();

                    var mockResponse = new Mock<Response<BlobContentInfo>>();
                    var mockContentInfo = new Mock<BlobContentInfo>();
                    mockContentInfo.Setup(c => c.ETag).Returns(ETag.All);
                    mockResponse.Setup(c => c.Value).Returns(mockContentInfo.Object);
                    return mockResponse.Object;
                })
                .Verifiable();

            var repository = new AzureBlobXmlRepository(() => mock.Object);
            repository.StoreElement(new XElement("Element2"), null);

            mock.Verify();
            Assert.Equal(bytes, GetEnvelopedContent("<Element1 /><Element2 />"));
        }

        private static byte[] GetEnvelopedContent(string element)
        {
            return Encoding.UTF8.GetBytes($"﻿<?xml version=\"1.0\" encoding=\"utf-8\"?><repository>{element}</repository>");
        }
    }
}
