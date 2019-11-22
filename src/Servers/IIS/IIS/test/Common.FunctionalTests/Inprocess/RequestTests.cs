// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess
{
    [Collection(IISTestSiteCollection.Name)]
    [RequiresNewHandler]
    public class RequestInProcessTests
    {
        private readonly IISTestSiteFixture _fixture;

        public RequestInProcessTests(IISTestSiteFixture fixture)
        {
            _fixture = fixture;
        }

        [ConditionalFact]
        public async Task RequestPath_UrlUnescaping()
        {
            // Must start with '/'
            var stringBuilder = new StringBuilder("/RequestPath/");
            for (var i = 32; i < 127; i++)
            {
                if (i == 43) continue; // %2B "+" gives a 404.11 (URL_DOUBLE_ESCAPED)
                stringBuilder.Append("%");
                stringBuilder.Append(i.ToString("X2"));
            }
            var rawPath = stringBuilder.ToString();
            var response = await SendSocketRequestAsync(rawPath);
            Assert.Equal(200, response.Status);
            // '/' %2F is an exception, un-escaping it would change the structure of the path
            Assert.Equal("/ !\"#$%&'()*,-.%2F0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~", response.Body);
        }

        [ConditionalFact]
        public async Task Request_WithDoubleSlashes_LeftAlone()
        {
            var rawPath = "/RequestPath//a/b//c";
            var response = await SendSocketRequestAsync(rawPath);
            Assert.Equal(200, response.Status);
            Assert.Equal("//a/b//c", response.Body);
        }

        [ConditionalTheory]
        [RequiresNewHandler]
        [InlineData("/RequestPath/a/b/../c", "/a/c")]
        [InlineData("/RequestPath/a/b/./c", "/a/b/c")]
        public async Task Request_WithNavigation_Removed(string input, string expectedPath)
        {
            var response = await SendSocketRequestAsync(input);
            Assert.Equal(200, response.Status);
            Assert.Equal(expectedPath, response.Body);
        }

        [ConditionalTheory]
        [RequiresNewHandler]
        [InlineData("/RequestPath/a/b/%2E%2E/c", "/a/c")]
        [InlineData("/RequestPath/a/b/%2E/c", "/a/b/c")]
        public async Task Request_WithEscapedNavigation_Removed(string input, string expectedPath)
        {
            var response = await SendSocketRequestAsync(input);
            Assert.Equal(200, response.Status);
            Assert.Equal(expectedPath, response.Body);
        }

        [ConditionalFact]
        public async Task Request_ControlCharacters_400()
        {
            for (var i = 0; i < 32; i++)
            {
                if (i == 9 || i == 10) continue; // \t and \r are allowed by Http.Sys.
                var response = await SendSocketRequestAsync("/" + (char)i);
                Assert.True(string.Equals(400, response.Status), i.ToString("X2") + ";" + response);
            }
        }

        [ConditionalFact]
        public async Task Request_EscapedControlCharacters_400()
        {
            for (var i = 0; i < 32; i++)
            {
                var response = await SendSocketRequestAsync("/%" + i.ToString("X2"));
                Assert.True(string.Equals(400, response.Status), i.ToString("X2") + ";" + response);
            }
        }

        private async Task<(int Status, string Body)> SendSocketRequestAsync(string path)
        {
            using (var connection = _fixture.CreateTestConnection())
            {
                await connection.Send(
                    "GET " + path + " HTTP/1.1",
                    "Host: " + _fixture.Client.BaseAddress.Authority,
                    "",
                    "");
                var headers = await connection.ReceiveHeaders();
                var status = int.Parse(headers[0].Substring(9, 3));
                if (headers.Contains("Transfer-Encoding: chunked"))
                {
                    var bytes0 = await connection.ReceiveChunk();
                    Assert.False(bytes0.IsEmpty);
                    return (status, Encoding.UTF8.GetString(bytes0.Span));
                }
                var length = int.Parse(headers.Single(h => h.StartsWith("Content-Length: ")).Substring("Content-Length: ".Length));
                var bytes1 = await connection.Receive(length);
                return (status, Encoding.ASCII.GetString(bytes1.Span));
            }
        }
    }
}
