// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.AzureStorage.Test
{
    public class AzureBlobXmlRepositoryTests
    {
        [Fact]
        public void StoreCreatesBlobWhenNotExist()
        {
            AccessCondition downloadCondition = null;
            AccessCondition uploadCondition = null;
            byte[] bytes = null;
            BlobProperties properties = new BlobProperties();

            var mock = new Mock<ICloudBlob>();
            mock.SetupGet(c => c.Properties).Returns(properties);
            mock.Setup(c => c.UploadFromByteArrayAsync(
                    It.IsAny<byte[]>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<AccessCondition>(),
                    It.IsAny<BlobRequestOptions>(),
                    It.IsAny<OperationContext>()))
                .Returns(async (byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext) =>
                {
                    bytes = buffer.Skip(index).Take(count).ToArray();
                    uploadCondition = accessCondition;
                    await Task.Yield();
                });

            var repository = new AzureBlobXmlRepository(() => mock.Object);
            repository.StoreElement(new XElement("Element"), null);

            Assert.Null(downloadCondition);
            Assert.Equal("*", uploadCondition.IfNoneMatchETag);
            Assert.Equal("application/xml; charset=utf-8", properties.ContentType);
            var element = "<Element />";

            Assert.Equal(bytes, GetEnvelopedContent(element));
        }

        [Fact]
        public void StoreUpdatesWhenExistsAndNewerExists()
        {
            AccessCondition downloadCondition = null;
            byte[] bytes = null;
            BlobProperties properties = new BlobProperties();

            var mock = new Mock<ICloudBlob>();
            mock.SetupGet(c => c.Properties).Returns(properties);
            mock.Setup(c => c.DownloadToStreamAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<AccessCondition>(),
                    null,
                    null))
                .Returns(async (Stream target, AccessCondition condition, BlobRequestOptions options, OperationContext context) =>
                {
                    var data = GetEnvelopedContent("<Element1 />");
                    await target.WriteAsync(data, 0, data.Length);
                })
                .Verifiable();

            mock.Setup(c => c.UploadFromByteArrayAsync(
                    It.IsAny<byte[]>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.Is((AccessCondition cond) => cond.IfNoneMatchETag == "*"),
                    It.IsAny<BlobRequestOptions>(),
                    It.IsAny<OperationContext>()))
                .Throws(new StorageException(new RequestResult { HttpStatusCode = 412 }, null, null))
                .Verifiable();

            mock.Setup(c => c.UploadFromByteArrayAsync(
                    It.IsAny<byte[]>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.Is((AccessCondition cond) => cond.IfNoneMatchETag != "*"),
                    It.IsAny<BlobRequestOptions>(),
                    It.IsAny<OperationContext>()))
                .Returns(async (byte[] buffer, int index, int count, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext) =>
                {
                    bytes = buffer.Skip(index).Take(count).ToArray();
                    await Task.Yield();
                })
                .Verifiable();

            var repository = new AzureBlobXmlRepository(() => mock.Object);
            repository.StoreElement(new XElement("Element2"), null);

            mock.Verify();
            Assert.Null(downloadCondition);
            Assert.Equal(bytes, GetEnvelopedContent("<Element1 /><Element2 />"));
        }

        private static byte[] GetEnvelopedContent(string element)
        {
            return Encoding.UTF8.GetBytes($"﻿<?xml version=\"1.0\" encoding=\"utf-8\"?><repository>{element}</repository>");
        }
    }
}
