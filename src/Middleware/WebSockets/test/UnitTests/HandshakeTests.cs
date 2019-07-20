// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.WebSockets.Tests
{
    public class HandshakeTests
    {
        [Theory]
        [MemberData(nameof(SecWebsocketKeys))]
        public void CreatesCorrectResponseKey(string key)
        {
            var response = HandshakeHelpers.CreateResponseKey(key);

            using var sha1 = SHA1.Create();
            var requestKey = key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            var hashedBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(requestKey));
            var expectedResponse = Convert.ToBase64String(hashedBytes);
            Assert.Equal(expectedResponse, response);
        }

        [Theory]
        [InlineData("VUfWn1u2Ot0AICM6f+/8Zg==")]
        public void AcceptsValidRequestKeys(string key)
        {
            Assert.True(HandshakeHelpers.IsRequestKeyValid(key));
        }

        [Theory]
        [InlineData("+/UH3kSoKpZsAG1dbr0gt/s=")]
        [InlineData("klbuwX3CkgUIA8x23owW")]
        [InlineData("")]
        [InlineData("24 length not base64 str")]
        public void RejectsInvalidRequestKeys(string key)
        {
            Assert.False(HandshakeHelpers.IsRequestKeyValid(key));
        }

        public static IEnumerable<object[]> SecWebsocketKeys
        {
            get
            {
                var random = new Random();
                for (var i = 0; i < 10; i++)
                {
                    byte[] buffer = new byte[16];
                    random.NextBytes(buffer);
                    var base64String = Convert.ToBase64String(buffer);
                    yield return new string[] { base64String };
                }
            }
        }
    }
}
