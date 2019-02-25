// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.WebUtilities.Test
{
    public class FormPipeReaderTests
    {
        // TODO add more tests for encodings
        // And multisegment
        [Fact]
        public async Task ReadFormAsync_EmptyKeyAtEndAllowed()
        {
            var bodyPipe = await MakePipeReader("=bar");

            var formCollection = await ReadFormAsync(new FormPipeReader(bodyPipe));

            Assert.Equal("bar", formCollection[""].ToString());
        }

        [Fact]
        public async Task ReadFormAsync_EmptyKeyWithAdditionalEntryAllowed()
        {
            var bodyPipe = await MakePipeReader("=bar&baz=2");

            var formCollection = await ReadFormAsync(new FormPipeReader(bodyPipe));

            Assert.Equal("bar", formCollection[""].ToString());
            Assert.Equal("2", formCollection["baz"].ToString());
        }

        [Fact]
        public async Task ReadFormAsync_EmptyValuedAtEndAllowed()
        {
            var bodyPipe = await MakePipeReader("foo=");

            var formCollection = await ReadFormAsync(new FormPipeReader(bodyPipe));

            Assert.Equal("", formCollection["foo"].ToString());
        }

        [Fact]
        public async Task ReadFormAsync_EmptyValuedWithAdditionalEntryAllowed()
        {
            var bodyPipe = await MakePipeReader("foo=&baz=2");

            var formCollection = await ReadFormAsync(new FormPipeReader(bodyPipe));

            Assert.Equal("", formCollection["foo"].ToString());
            Assert.Equal("2", formCollection["baz"].ToString());
        }

        [Fact]
        public async Task ReadFormAsync_ValueCountLimitMet_Success()
        {
            var bodyPipe = await MakePipeReader("foo=1&bar=2&baz=3");

            var formCollection = await ReadFormAsync(new FormPipeReader(bodyPipe) { ValueCountLimit = 3 });

            Assert.Equal("1", formCollection["foo"].ToString());
            Assert.Equal("2", formCollection["bar"].ToString());
            Assert.Equal("3", formCollection["baz"].ToString());
            Assert.Equal(3, formCollection.Count);
        }

        [Fact]
        public async Task ReadFormAsync_ValueCountLimitExceeded_Throw()
        {
            var bodyPipe = await MakePipeReader("foo=1&baz=2&bar=3&baz=4&baf=5");

            var exception = await Assert.ThrowsAsync<InvalidDataException>(
                () => ReadFormAsync(new FormPipeReader(bodyPipe) { ValueCountLimit = 3 }));
            Assert.Equal("Form value count limit 3 exceeded.", exception.Message);
        }

        [Fact]
        public async Task ReadFormAsync_ValueCountLimitExceededSameKey_Throw()
        {
            var bodyPipe = await MakePipeReader("baz=1&baz=2&baz=3&baz=4");

            var exception = await Assert.ThrowsAsync<InvalidDataException>(
                () => ReadFormAsync(new FormPipeReader(bodyPipe) { ValueCountLimit = 3 }));
            Assert.Equal("Form value count limit 3 exceeded.", exception.Message);
        }

        [Fact]
        public async Task ReadFormAsync_KeyLengthLimitMet_Success()
        {
            var bodyPipe = await MakePipeReader("foo=1&bar=2&baz=3&baz=4");

            var formCollection = await ReadFormAsync(new FormPipeReader(bodyPipe) { KeyLengthLimit = 10 });

            Assert.Equal("1", formCollection["foo"].ToString());
            Assert.Equal("2", formCollection["bar"].ToString());
            Assert.Equal("3,4", formCollection["baz"].ToString());
            Assert.Equal(3, formCollection.Count);
        }

        [Fact]
        public async Task Asdf() // TODO what does this do?
        {
            var bodyPipe = await MakePipeReader("foo");

            var formCollection = await ReadFormAsync(new FormPipeReader(bodyPipe) { KeyLengthLimit = 10 });

            Assert.Equal("1", formCollection["foo"].ToString());
            Assert.Equal("2", formCollection["bar"].ToString());
            Assert.Equal("3,4", formCollection["baz"].ToString());
            Assert.Equal(3, formCollection.Count);
        }

        [Fact]
        public async Task ReadFormAsync_KeyLengthLimitExceeded_Throw()
        {
            var bodyPipe = await MakePipeReader("foo=1&baz1234567890=2");

            var exception = await Assert.ThrowsAsync<InvalidDataException>(
                () => ReadFormAsync(new FormPipeReader(bodyPipe) { KeyLengthLimit = 10 }));
            Assert.Equal("Form key or value length limit 10 exceeded.", exception.Message);
        }

        [Fact]
        public async Task ReadFormAsync_ValueLengthLimitMet_Success()
        {
            var bodyPipe = await MakePipeReader("foo=1&bar=1234567890&baz=3&baz=4");

            var formCollection = await ReadFormAsync(new FormPipeReader(bodyPipe) { ValueLengthLimit = 10 });

            Assert.Equal("1", formCollection["foo"].ToString());
            Assert.Equal("1234567890", formCollection["bar"].ToString());
            Assert.Equal("3,4", formCollection["baz"].ToString());
            Assert.Equal(3, formCollection.Count);
        }

        [Fact]
        public async Task ReadFormAsync_ValueLengthLimitExceeded_Throw()
        {
            var bodyPipe = await MakePipeReader("foo=1&baz=1234567890123");

            var exception = await Assert.ThrowsAsync<InvalidDataException>(
                () => ReadFormAsync(new FormPipeReader(bodyPipe) { ValueLengthLimit = 10 }));
            Assert.Equal("Form key or value length limit 10 exceeded.", exception.Message);
        }

        [Fact]
        public void TestReadOnlySequence()
        {
            var singleSegmentReadOnlySequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("Hello world"));
            var formPipeReader = new FormPipeReader(null);
            KeyValueAccumulator accumulator = default;

            formPipeReader.TryParseFormValues(ref singleSegmentReadOnlySequence, ref accumulator, false);
        }

        // TODO test FF

        // https://en.wikipedia.org/wiki/Percent-encoding
        [Theory]
        [InlineData("++=hello", "  ", "hello")]
        [InlineData("a=1+1", "a", "1 1")]
        [InlineData("%22%25%2D%2E%3C%3E%5C%5E%5F%60%7B%7C%7D%7E=%22%25%2D%2E%3C%3E%5C%5E%5F%60%7B%7C%7D%7E", "\"%-.<>\\^_`{|}~", "\"%-.<>\\^_`{|}~")]
        [InlineData("a=%41", "a", "A")] // ascii encoded hex
        [InlineData("a=%C3%A1", "a", "\u00e1")] // utf8 code points
        [InlineData("a=%u20AC", "a", "%u20AC")] // utf16 not supported
        public async Task ReadForm_Decoding(string formData, string key, string expectedValue)
        {
            var bodyPipe = await MakePipeReader(text: formData);

            var form = await ReadFormAsync(new FormPipeReader(bodyPipe));

            Assert.Equal(expectedValue, form[key]);
        }

        protected virtual Task<Dictionary<string, StringValues>> ReadFormAsync(FormPipeReader reader)
        {
            return reader.ReadFormAsync();
        }

        private static async Task<PipeReader> MakePipeReader(string text)
        {
            var formContent = Encoding.UTF8.GetBytes(text);
            Pipe bodyPipe = new Pipe();

            // Bleh buffering doesn't do anything rn.
            await bodyPipe.Writer.WriteAsync(formContent);

            // Complete the writer so the reader will complete after processing all data.
            // TODO tests will need to call complete at different times. Make that work well.
            bodyPipe.Writer.Complete();
            return bodyPipe.Reader;
        }
    }
}
