// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class HPackEncoderTests
    {
        [Fact]
        public void EncodesHeadersInSinglePayloadWhenSpaceAvailable()
        {
            var encoder = new HPackEncoder();

            var statusCode = 200;
            var headers = new []
            {
                new KeyValuePair<string, string>("date", "Mon, 24 Jul 2017 19:22:30 GMT"),
                new KeyValuePair<string, string>("content-type", "text/html; charset=utf-8"),
                new KeyValuePair<string, string>("server", "Kestrel")
            };

            var expectedPayload = new byte[]
            {
                0x88, 0x00, 0x04, 0x64, 0x61, 0x74, 0x65, 0x1d,
                0x4d, 0x6f, 0x6e, 0x2c, 0x20, 0x32, 0x34, 0x20,
                0x4a, 0x75, 0x6c, 0x20, 0x32, 0x30, 0x31, 0x37,
                0x20, 0x31, 0x39, 0x3a, 0x32, 0x32, 0x3a, 0x33,
                0x30, 0x20, 0x47, 0x4d, 0x54, 0x00, 0x0c, 0x63,
                0x6f, 0x6e, 0x74, 0x65, 0x6e, 0x74, 0x2d, 0x74,
                0x79, 0x70, 0x65, 0x18, 0x74, 0x65, 0x78, 0x74,
                0x2f, 0x68, 0x74, 0x6d, 0x6c, 0x3b, 0x20, 0x63,
                0x68, 0x61, 0x72, 0x73, 0x65, 0x74, 0x3d, 0x75,
                0x74, 0x66, 0x2d, 0x38, 0x00, 0x06, 0x73, 0x65,
                0x72, 0x76, 0x65, 0x72, 0x07, 0x4b, 0x65, 0x73,
                0x74, 0x72, 0x65, 0x6c
            };

            var payload = new byte[1024];
            Assert.True(encoder.BeginEncode(statusCode, headers, payload, out var length));
            Assert.Equal(expectedPayload.Length, length);

            for (var i = 0; i < length; i++)
            {
                Assert.True(expectedPayload[i] == payload[i], $"{expectedPayload[i]} != {payload[i]} at {i} (len {length})");
            }

            Assert.Equal(expectedPayload, new ArraySegment<byte>(payload, 0, length));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EncodesHeadersInMultiplePayloadsWhenSpaceNotAvailable(bool exactSize)
        {
            var encoder = new HPackEncoder();

            var statusCode = 200;
            var headers = new []
            {
                new KeyValuePair<string, string>("date", "Mon, 24 Jul 2017 19:22:30 GMT"),
                new KeyValuePair<string, string>("content-type", "text/html; charset=utf-8"),
                new KeyValuePair<string, string>("server", "Kestrel")
            };

            var expectedStatusCodePayload = new byte[]
            {
                0x88
            };

            var expectedDateHeaderPayload = new byte[]
            {
                0x00, 0x04, 0x64, 0x61, 0x74, 0x65, 0x1d, 0x4d,
                0x6f, 0x6e, 0x2c, 0x20, 0x32, 0x34, 0x20, 0x4a,
                0x75, 0x6c, 0x20, 0x32, 0x30, 0x31, 0x37, 0x20,
                0x31, 0x39, 0x3a, 0x32, 0x32, 0x3a, 0x33, 0x30,
                0x20, 0x47, 0x4d, 0x54
            };

            var expectedContentTypeHeaderPayload = new byte[]
            {
                0x00, 0x0c, 0x63, 0x6f, 0x6e, 0x74, 0x65, 0x6e,
                0x74, 0x2d, 0x74, 0x79, 0x70, 0x65, 0x18, 0x74,
                0x65, 0x78, 0x74, 0x2f, 0x68, 0x74, 0x6d, 0x6c,
                0x3b, 0x20, 0x63, 0x68, 0x61, 0x72, 0x73, 0x65,
                0x74, 0x3d, 0x75, 0x74, 0x66, 0x2d, 0x38
            };

            var expectedServerHeaderPayload = new byte[]
            {
                0x00, 0x06, 0x73, 0x65, 0x72, 0x76, 0x65, 0x72,
                0x07, 0x4b, 0x65, 0x73, 0x74, 0x72, 0x65, 0x6c
            };

            Span<byte> payload = new byte[1024];
            var offset = 0;

            // When !exactSize, slices are one byte short of fitting the next header
            var sliceLength = expectedStatusCodePayload.Length + (exactSize ? 0 : expectedDateHeaderPayload.Length - 1);
            Assert.False(encoder.BeginEncode(statusCode, headers, payload.Slice(offset, sliceLength), out var length));
            Assert.Equal(expectedStatusCodePayload.Length, length);
            Assert.Equal(expectedStatusCodePayload, payload.Slice(0, length).ToArray());

            offset += length;

            sliceLength = expectedDateHeaderPayload.Length + (exactSize ? 0 : expectedContentTypeHeaderPayload.Length - 1);
            Assert.False(encoder.Encode(payload.Slice(offset, sliceLength), out length));
            Assert.Equal(expectedDateHeaderPayload.Length, length);
            Assert.Equal(expectedDateHeaderPayload, payload.Slice(offset, length).ToArray());

            offset += length;

            sliceLength = expectedContentTypeHeaderPayload.Length + (exactSize ? 0 : expectedServerHeaderPayload.Length - 1);
            Assert.False(encoder.Encode(payload.Slice(offset, sliceLength), out length));
            Assert.Equal(expectedContentTypeHeaderPayload.Length, length);
            Assert.Equal(expectedContentTypeHeaderPayload, payload.Slice(offset, length).ToArray());

            offset += length;

            sliceLength = expectedServerHeaderPayload.Length;
            Assert.True(encoder.Encode(payload.Slice(offset, sliceLength), out length));
            Assert.Equal(expectedServerHeaderPayload.Length, length);
            Assert.Equal(expectedServerHeaderPayload, payload.Slice(offset, length).ToArray());
        }
    }
}
