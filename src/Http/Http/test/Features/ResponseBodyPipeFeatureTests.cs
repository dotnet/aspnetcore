// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.IO.Pipelines;
using Xunit;

namespace Microsoft.AspNetCore.Http.Features
{
    public class ResponseBodyPipeFeatureTests
    {
        [Fact]
        public void ResponseBodyReturnsStreamPipeReader()
        {
            var context = new DefaultHttpContext();
            var expectedStream = new MemoryStream();
            context.Response.Body = expectedStream;

            var provider = new ResponseBodyPipeFeature(context);

            var pipeBody = provider.ResponseBodyPipe;

            Assert.True(pipeBody is StreamPipeWriter);
            Assert.Equal(expectedStream, (pipeBody as StreamPipeWriter).InnerStream);
        }

        [Fact]
        public void ResponseBodySetPipeReaderReturnsSameValue()
        {
            var context = new DefaultHttpContext();
            var provider = new ResponseBodyPipeFeature(context);

            var pipeWriter = new Pipe().Writer;
            provider.ResponseBodyPipe = pipeWriter;

            Assert.Equal(pipeWriter, provider.ResponseBodyPipe);
        }

        [Fact]
        public void ResponseBodyGetPipeWriterAfterSettingBodyTwice()
        {
            var context = new DefaultHttpContext();
            var expectedStream = new MemoryStream();
            context.Response.Body = new MemoryStream();

            var provider = new ResponseBodyPipeFeature(context);

            var pipeBody = provider.ResponseBodyPipe;
            context.Response.Body = expectedStream;
            pipeBody = provider.ResponseBodyPipe;

            Assert.True(pipeBody is StreamPipeWriter);
            Assert.Equal(expectedStream, (pipeBody as StreamPipeWriter).InnerStream);
        }
    }
}
