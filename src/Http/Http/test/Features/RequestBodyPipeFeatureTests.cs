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
            var features = new FeatureCollection();
            var request = new HttpRequestFeature();
            var expectedStream = new MemoryStream();
            request.Body = expectedStream;
            features[typeof(IHttpRequestFeature)] = request;

            var provider = new RequestBodyPipeFeature(features);

            var pipeBody = provider.PipeReader;

            Assert.True(pipeBody is StreamPipeReader);
            Assert.Equal(expectedStream, (pipeBody as StreamPipeReader).InnerStream);
        }

        [Fact]
        public void RequestBodySetPipeReaderReturnsSameValue()
        {
            var features = new FeatureCollection();
            var request = new HttpRequestFeature();
            features[typeof(IHttpRequestFeature)] = request;

            var provider = new RequestBodyPipeFeature(features);

            var pipeReader = new Pipe().Reader;
            provider.PipeReader = pipeReader;

            Assert.Equal(pipeReader, provider.PipeReader);
        }
    }
}
