// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
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
        public async Task RequestBodyReadCanWorkWithPipe()
        {
            var expectedString = "abcdef";
            var provider = InitializeFeatureWithData(expectedString);

            var data = await provider.RequestBodyPipe.ReadAsync();
            Assert.Equal(expectedString, GetStringFromReadResult(data));
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
        public void RequestBodySetPipeReadReturnsUserSetValueAlways()
        {
            var context = new DefaultHttpContext();

            var provider = new RequestBodyPipeFeature(context);

            var expectedPipeReader = new Pipe().Reader;
            provider.RequestBodyPipe = expectedPipeReader;

            // Because the user set the RequestBodyPipe, this will return the user set pipeReader
            context.Request.Body = new MemoryStream();

            Assert.Equal(expectedPipeReader, provider.RequestBodyPipe);
        }

        [Fact]
        public async Task RequestBodyDoesNotAffectUserSetPipe()
        {
            var expectedString = "abcdef";
            var provider = InitializeFeatureWithData("hahaha");
            provider.RequestBodyPipe = await GetPipeReaderWithData(expectedString);

            var data = await provider.RequestBodyPipe.ReadAsync();
            Assert.Equal(expectedString, GetStringFromReadResult(data));
        }

        [Fact]
        public void RequestBodyGetPipeReaderAfterSettingBodyTwice()
        {
            var context = new DefaultHttpContext();

            context.Request.Body = new MemoryStream();

            var provider = new RequestBodyPipeFeature(context);

            var pipeBody = provider.RequestBodyPipe;

            // Requery the PipeReader after setting the body again.
            var expectedStream = new MemoryStream();
            context.Request.Body = expectedStream;
            pipeBody = provider.RequestBodyPipe;

            Assert.True(pipeBody is StreamPipeReader);
            Assert.Equal(expectedStream, (pipeBody as StreamPipeReader).InnerStream);
        }

        [Fact]
        public async Task RequestBodyGetsDataFromSecondStream()
        {
            var context = new DefaultHttpContext();
            context.Request.Body = new MemoryStream(Encoding.ASCII.GetBytes("hahaha"));
            var provider = new RequestBodyPipeFeature(context);
            var _ = provider.RequestBodyPipe;

            var expectedString = "abcdef";
            context.Request.Body = new MemoryStream(Encoding.ASCII.GetBytes(expectedString));
            var data = await provider.RequestBodyPipe.ReadAsync();
            Assert.Equal(expectedString, GetStringFromReadResult(data));
        }

        private RequestBodyPipeFeature InitializeFeatureWithData(string input)
        {
            var context = new DefaultHttpContext();
            context.Request.Body = new MemoryStream(Encoding.ASCII.GetBytes(input));
            return new RequestBodyPipeFeature(context);
        }

        private static string GetStringFromReadResult(ReadResult data)
        {
            return Encoding.ASCII.GetString(data.Buffer.ToArray());
        }

        private async Task<PipeReader> GetPipeReaderWithData(string input)
        {
            var pipe = new Pipe();
            await pipe.Writer.WriteAsync(Encoding.ASCII.GetBytes(input));
            return pipe.Reader;
        }
    }
}
