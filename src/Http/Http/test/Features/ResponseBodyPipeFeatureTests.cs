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

            var feature = new ResponseBodyPipeFeature(context);

            var pipeBody = feature.Writer;

            Assert.True(pipeBody is StreamPipeWriter);
            Assert.Equal(expectedStream, (pipeBody as StreamPipeWriter).InnerStream);
        }

        [Fact]
        public void ResponseBodySetPipeReaderReturnsSameValue()
        {
            var context = new DefaultHttpContext();
            var feature = new ResponseBodyPipeFeature(context);

            var pipeWriter = new Pipe().Writer;
            feature.Writer = pipeWriter;

            Assert.Equal(pipeWriter, feature.Writer);
        }

        [Fact]
        public void ResponseBodyGetPipeWriterAfterSettingBodyTwice()
        {
            var context = new DefaultHttpContext();
            var expectedStream = new MemoryStream();
            context.Response.Body = new MemoryStream();

            var feature = new ResponseBodyPipeFeature(context);

            var pipeBody = feature.Writer;
            context.Response.Body = expectedStream;
            pipeBody = feature.Writer;

            Assert.True(pipeBody is StreamPipeWriter);
            Assert.Equal(expectedStream, (pipeBody as StreamPipeWriter).InnerStream);
        }
    }
}
