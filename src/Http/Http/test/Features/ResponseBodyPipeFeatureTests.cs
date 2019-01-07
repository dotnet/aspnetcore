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
            var features = new FeatureCollection();
            var response = new HttpResponseFeature();
            var expectedStream = new MemoryStream();
            response.Body = expectedStream;
            features[typeof(IHttpResponseFeature)] = response;

            var provider = new ResponseBodyPipeFeature(features);

            var pipeBody = provider.ResponseBodyPipe;

            Assert.True(pipeBody is StreamPipeWriter);
            Assert.Equal(expectedStream, (pipeBody as StreamPipeWriter).InnerStream);
        }

        [Fact]
        public void ResponseBodySetPipeReaderReturnsSameValue()
        {
            var features = new FeatureCollection();
            var response = new HttpResponseFeature();
            features[typeof(IHttpResponseFeature)] = response;

            var provider = new ResponseBodyPipeFeature(features);

            var pipeWriter = new Pipe().Writer;
            provider.ResponseBodyPipe = pipeWriter;

            Assert.Equal(pipeWriter, provider.ResponseBodyPipe);
        }

        [Fact]
        public void ResponseBodyGetPipeWriterAfterSettingBodyTwice()
        {
            var features = new FeatureCollection();
            var response = new HttpResponseFeature();
            var expectedStream = new MemoryStream();
            response.Body = new MemoryStream();
            features[typeof(IHttpResponseFeature)] = response;

            var provider = new ResponseBodyPipeFeature(features);

            var pipeBody = provider.ResponseBodyPipe;
            response.Body = expectedStream;
            pipeBody = provider.ResponseBodyPipe;

            Assert.True(pipeBody is StreamPipeWriter);
            Assert.Equal(expectedStream, (pipeBody as StreamPipeWriter).InnerStream);
        }
    }
}
