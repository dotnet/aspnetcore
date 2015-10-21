// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.MemoryPool;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Infrastructure
{
    public class MemoryPoolHttpResponseStreamWriterFactoryTest
    {
        [Fact]
        public void CreateWriter_BuffersReturned_OnException()
        {
            // Arrange
            var bytePool = new Mock<IArraySegmentPool<byte>>(MockBehavior.Strict);
            bytePool
                .Setup(p => p.Lease(It.IsAny<int>()))
                .Returns(new LeasedArraySegment<byte>(new ArraySegment<byte>(new byte[4096]), bytePool.Object));
            bytePool
                .Setup(p => p.Return(It.IsAny<LeasedArraySegment<byte>>()))
                .Verifiable();

            var charPool = new Mock<IArraySegmentPool<char>>(MockBehavior.Strict);
            charPool
                .Setup(p => p.Lease(MemoryPoolHttpResponseStreamWriterFactory.DefaultBufferSize))
                .Returns(new LeasedArraySegment<char>(new ArraySegment<char>(new char[4096]), charPool.Object));
            charPool
                .Setup(p => p.Return(It.IsAny<LeasedArraySegment<char>>()))
                .Verifiable();

            var encoding = new Mock<Encoding>();
            encoding
                .Setup(e => e.GetEncoder())
                .Throws(new InvalidOperationException());

            var factory = new MemoryPoolHttpResponseStreamWriterFactory(bytePool.Object, charPool.Object);

            // Act
            Assert.Throws<InvalidOperationException>(() => factory.CreateWriter(new MemoryStream(), encoding.Object));

            // Assert
            bytePool.Verify();
            charPool.Verify();
        }
    }
}
