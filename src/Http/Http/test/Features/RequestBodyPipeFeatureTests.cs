// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.IO.Pipelines;
using Xunit;

namespace Microsoft.AspNetCore.Http.Features
{
    public class RequestBodyPipeFeatureTests
    {
        [Fact]
        public void RequestBodyReturnsStreamPipeReader()
        {
            var context = new DefaultHttpContext();
            var expectedStream = new MemoryStream();
            context.Request.Body = expectedStream;

            var provider = new RequestBodyPipeFeature(context);

            var pipeBody = provider.RequestBodyPipe;

            Assert.True(pipeBody is StreamPipeReader);
            Assert.Equal(expectedStream, (pipeBody as StreamPipeReader).InnerStream);
        }

        [Fact]
        public void RequestBodySetPipeReaderReturnsSameValue()
        {
            var context = new DefaultHttpContext();

            var provider = new RequestBodyPipeFeature(context);

            var pipeReader = new Pipe().Reader;
            provider.RequestBodyPipe = pipeReader;

            Assert.Equal(pipeReader, provider.RequestBodyPipe);
        }

        [Fact]
        public void RequestBodyGetPipeReaderAfterSettingBodyTwice()
        {
            var context = new DefaultHttpContext();

            var expectedStream = new MemoryStream();
            context.Request.Body = new MemoryStream();

            var provider = new RequestBodyPipeFeature(context);

            var pipeBody = provider.RequestBodyPipe;
            // Requery the PipeReader after setting the body again.
            context.Request.Body = expectedStream;
            pipeBody = provider.RequestBodyPipe;

            Assert.True(pipeBody is StreamPipeReader);
            Assert.Equal(expectedStream, (pipeBody as StreamPipeReader).InnerStream);
        }
    }
}
