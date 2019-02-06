// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.IO.Pipelines;
using System.IO.Pipelines.Tests;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Http
{
    public class HttpResponseWritingExtensionsTests
    {
        [Fact]
        public async Task WritingText_WriteText()
        {
            HttpContext context = CreateRequest();
            await context.Response.WriteAsync("Hello World");

            Assert.Equal(11, context.Response.Body.Length);
        }

        [Fact]
        public async Task WritingText_MultipleWrites()
        {
            HttpContext context = CreateRequest();
            await context.Response.WriteAsync("Hello World");
            await context.Response.WriteAsync("Hello World");

            Assert.Equal(22, context.Response.Body.Length);
        }

        [Theory]
        [MemberData(nameof(Encodings))]
        public async Task WritingTextThatRequiresMultipleSegmentsWorks(Encoding encoding)
        {
            // Need to change the StreamPipeWriter with a capped MemoryPool
            var memoryPool = new TestMemoryPool(maxBufferSize: 16);
            var outputStream = new MemoryStream();
            var streamPipeWriter = new StreamPipeWriter(outputStream, minimumSegmentSize: 0, memoryPool);

            HttpContext context = new DefaultHttpContext();
            context.Response.BodyPipe = streamPipeWriter;

            var inputString = "丂丂丂丂丂丂丂丂丂丂丂丂丂丂丂";
            var expected = encoding.GetBytes(inputString);
            await context.Response.WriteAsync(inputString, encoding);

            outputStream.Position = 0;
            var actual = new byte[expected.Length];
            var length = outputStream.Read(actual);

            var res1 = encoding.GetString(actual);
            var res2 = encoding.GetString(expected);
            Assert.Equal(expected.Length, length);
            Assert.Equal(expected, actual);
            streamPipeWriter.Complete();
        }

        public static TheoryData<Encoding> Encodings =>
                 new TheoryData<Encoding>
                 {
                     { Encoding.ASCII },
                     { Encoding.BigEndianUnicode },
                     { Encoding.Unicode },
                     { Encoding.UTF32 },
                     { Encoding.UTF7 },
                     { Encoding.UTF8 }
                 };

        [Theory]
        [MemberData(nameof(Encodings))]
        public async Task WritingTextWithPassedInEncodingWorks(Encoding encoding)
        {
            HttpContext context = CreateRequest();

            var inputString = "丂丂丂丂丂";
            var expected = encoding.GetBytes(inputString);
            await context.Response.WriteAsync(inputString, encoding);

            context.Response.Body.Position = 0;
            var actual = new byte[expected.Length];
            var length = context.Response.Body.Read(actual);

            Assert.Equal(expected.Length, length);
            Assert.Equal(expected, actual);
        }

        private HttpContext CreateRequest()
        {
            HttpContext context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            return context;
        }
    }
}
