using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http3StreamTests : Http3TestBase
    {
        [Fact]
        public async Task HelloWorldTest()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "Custom"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
            };

            var requestStream = await InitializeConnectionAndStreamsAsync(_echoApplication);

            var doneWithHeaders = await requestStream.SendHeadersAsync(headers);
            await requestStream.SendDataAsync(Encoding.ASCII.GetBytes("Hello world"));

            var responseHeaders = await requestStream.ExpectHeadersAsync();
            var responseData = await requestStream.ExpectDataAsync();
            Assert.Equal("Hello world", Encoding.ASCII.GetString(responseData.ToArray()));
        }

        [Fact]
        public async Task RequestHeadersMaxRequestHeaderFieldSize_EndsStream()
        {
            var headers = new[]
            {
                new KeyValuePair<string, string>(HeaderNames.Method, "Custom"),
                new KeyValuePair<string, string>(HeaderNames.Path, "/"),
                new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>(HeaderNames.Authority, "localhost:80"),
                new KeyValuePair<string, string>("test", new string('a', 10000))
            };

            var requestStream = await InitializeConnectionAndStreamsAsync(_echoApplication);

            var doneWithHeaders = await requestStream.SendHeadersAsync(headers);
            await requestStream.SendDataAsync(Encoding.ASCII.GetBytes("Hello world"));

            // TODO figure out how to test errors for request streams that would be set on the Quic Stream.
            await requestStream.ExpectReceiveEndOfStream();
        }
    }
}
