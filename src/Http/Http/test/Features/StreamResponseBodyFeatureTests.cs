// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Http.Features
{
    public class StreamResponseBodyFeatureTests
    {
        [Fact]
        public async Task CompleteAsyncCallsStartAsync()
        {
            // Arrange
            var stream = new MemoryStream();
            var streamResponseBodyFeature = new TestStreamResponseBodyFeature(stream);

            // Act
            await streamResponseBodyFeature.CompleteAsync();

            //Assert
            Assert.Equal(1, streamResponseBodyFeature.StartCalled);
        }

        [Fact]
        public async Task CompleteAsyncWontCallsStartAsyncIfAlreadyStarted()
        {
            // Arrange
            var stream = new MemoryStream();
            var streamResponseBodyFeature = new TestStreamResponseBodyFeature(stream);
            await streamResponseBodyFeature.StartAsync();

            // Act
            await streamResponseBodyFeature.CompleteAsync();

            //Assert
            Assert.Equal(1, streamResponseBodyFeature.StartCalled);
        }
    }

    public class TestStreamResponseBodyFeature : StreamResponseBodyFeature
    {
        public TestStreamResponseBodyFeature(Stream stream)
            : base(stream)
        {

        }

        public override Task StartAsync(CancellationToken cancellationToken = default)
        {
            StartCalled++;
            return base.StartAsync(cancellationToken);
        }

        public int StartCalled { get; private set; }
    }
}
