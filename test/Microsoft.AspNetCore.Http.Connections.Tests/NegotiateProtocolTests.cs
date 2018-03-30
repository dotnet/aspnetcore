using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Http.Connections.Tests
{
    public class NegotiateProtocolTests
    {
        [Theory]
        [InlineData("{\"connectionId\":\"123\",\"availableTransports\":[]}", "123", new string[0])]
        [InlineData("{\"connectionId\":\"\",\"availableTransports\":[]}", "", new string[0])]
        [InlineData("{\"connectionId\":\"123\",\"availableTransports\":[{\"transport\":\"test\",\"transferFormats\":[]}]}", "123", new [] { "test"})]
        public void ParsingNegotiateResponseMessageSuccessForValid(string json, string connectionId, string[] availableTransports)
        {
            var responseData = Encoding.UTF8.GetBytes(json);
            var ms = new MemoryStream(responseData);
            var response = NegotiateProtocol.ParseResponse(ms);

            Assert.Equal(connectionId, response.ConnectionId);
            Assert.Equal(availableTransports.Length, response.AvailableTransports.Count);

            var responseTransports = response.AvailableTransports.Select(t => t.Transport).ToList();

            Assert.Equal(availableTransports, responseTransports);
        }

        [Theory]
        [InlineData("null", "Unexpected JSON Token Type 'Null'. Expected a JSON Object.")]
        [InlineData("[]", "Unexpected JSON Token Type 'Array'. Expected a JSON Object.")]
        [InlineData("{\"availableTransports\":[]}", "Missing required property 'connectionId'.")]
        [InlineData("{\"connectionId\":123,\"availableTransports\":[]}", "Expected 'connectionId' to be of type String.")]
        [InlineData("{\"connectionId\":\"123\",\"availableTransports\":null}", "Unexpected JSON Token Type 'Null'. Expected a JSON Array.")]
        [InlineData("{\"connectionId\":\"123\",\"availableTransports\":[{\"transferFormats\":[]}]}", "Missing required property 'transport'.")]
        [InlineData("{\"connectionId\":\"123\",\"availableTransports\":[{\"transport\":\"test\"}]}", "Missing required property 'transferFormats'.")]
        public void ParsingNegotiateResponseMessageThrowsForInvalid(string payload, string expectedMessage)
        {
            var responseData = Encoding.UTF8.GetBytes(payload);
            var ms = new MemoryStream(responseData);

            var exception = Assert.Throws<InvalidDataException>(() => NegotiateProtocol.ParseResponse(ms));

            Assert.Equal(expectedMessage, exception.InnerException.Message);
        }
    }
}
