// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Http.Connections.Tests;

public class NegotiateProtocolTests
{
    [Theory]
    [InlineData("{\"connectionId\":\"123\",\"availableTransports\":[]}", "123", new string[0], null, null, 0, null)]
    [InlineData("{\"connectionId\":\"\",\"availableTransports\":[]}", "", new string[0], null, null, 0, null)]
    [InlineData("{\"url\": \"http://foo.com/chat\"}", null, null, "http://foo.com/chat", null, 0, null)]
    [InlineData("{\"url\": \"http://foo.com/chat\", \"accessToken\": \"token\"}", null, null, "http://foo.com/chat", "token", 0, null)]
    [InlineData("{\"connectionId\":\"123\",\"availableTransports\":[{\"transport\":\"test\",\"transferFormats\":[]}]}", "123", new[] { "test" }, null, null, 0, null)]
    [InlineData("{\"connectionId\":\"123\",\"availableTransports\":[{\"\\u0074ransport\":\"test\",\"transferFormats\":[]}]}", "123", new[] { "test" }, null, null, 0, null)]
    [InlineData("{\"negotiateVersion\":123,\"connectionId\":\"123\",\"connectionToken\":\"789\",\"availableTransports\":[{\"\\u0074ransport\":\"test\",\"transferFormats\":[]}]}", "123", new[] { "test" }, null, null, 123, "789")]
    [InlineData("{\"negotiateVersion\":123,\"negotiateVersion\":321, \"connectionToken\":\"789\",\"connectionId\":\"123\",\"availableTransports\":[]}", "123", new string[0], null, null, 321, "789")]
    [InlineData("{\"ignore\":123,\"negotiateVersion\":123, \"connectionToken\":\"789\",\"connectionId\":\"123\",\"availableTransports\":[]}", "123", new string[0], null, null, 123, "789")]
    [InlineData("{\"connectionId\":\"123\",\"availableTransports\":[],\"negotiateVersion\":123, \"connectionToken\":\"789\"}", "123", new string[0], null, null, 123, "789")]
    [InlineData("{\"connectionId\":\"123\",\"connectionToken\":\"789\",\"availableTransports\":[]}", "123", new string[0], null, null, 0, "789")]
    [InlineData("{\"connectionToken\":\"789\",\"connectionId\":\"123\",\"availableTransports\":[],\"negotiateVersion\":123}", "123", new string[0], null, null, 123, "789")]
    [InlineData("{\"connectionToken\":\"789\",\"connectionId\":\"123\",\"availableTransports\":[],\"negotiateVersion\":123, \"connectionToken\":\"987\"}", "123", new string[0], null, null, 123, "987")]
    public void ParsingNegotiateResponseMessageSuccessForValid(string json, string connectionId, string[] availableTransports, string url, string accessToken, int version, string connectionToken)
    {
        var responseData = Encoding.UTF8.GetBytes(json);
        var response = NegotiateProtocol.ParseResponse(responseData);

        Assert.Equal(connectionId, response.ConnectionId);
        Assert.Equal(availableTransports?.Length, response.AvailableTransports?.Count);
        Assert.Equal(url, response.Url);
        Assert.Equal(accessToken, response.AccessToken);
        Assert.Equal(version, response.Version);
        Assert.Equal(connectionToken, response.ConnectionToken);

        if (response.AvailableTransports != null)
        {
            var responseTransports = response.AvailableTransports.Select(t => t.Transport).ToList();

            Assert.Equal(availableTransports, responseTransports);
        }
    }

    [Theory]
    [InlineData("null", "Unexpected JSON Token Type 'Null'. Expected a JSON Object.")]
    [InlineData("[]", "Unexpected JSON Token Type 'Array'. Expected a JSON Object.")]
    [InlineData("{\"availableTransports\":[]}", "Missing required property 'connectionId'.")]
    [InlineData("{\"connectionId\":123,\"availableTransports\":[]}", "Expected 'connectionId' to be of type String.")]
    [InlineData("{\"connectionId\":\"123\",\"availableTransports\":null}", "Unexpected JSON Token Type 'Null'. Expected a JSON Array.")]
    [InlineData("{\"connectionId\":\"123\",\"availableTransports\":[{\"transferFormats\":[]}]}", "Missing required property 'transport'.")]
    [InlineData("{\"connectionId\":\"123\",\"availableTransports\":[{\"transport\":\"test\"}]}", "Missing required property 'transferFormats'.")]
    [InlineData("{\"connectionId\":\"123\",\"negotiateVersion\":123,\"availableTransports\":[]}", "Missing required property 'connectionToken'.")]
    public void ParsingNegotiateResponseMessageThrowsForInvalid(string payload, string expectedMessage)
    {
        var responseData = Encoding.UTF8.GetBytes(payload);

        var exception = Assert.Throws<InvalidDataException>(() => NegotiateProtocol.ParseResponse(responseData));

        Assert.Equal(expectedMessage, exception.InnerException.Message);
    }

    [Fact]
    public void ParsingAspNetSignalRResponseThrowsError()
    {
        var payload = "{\"Url\":\"/signalr\"," +
            "\"ConnectionToken\":\"X97dw3uxW4NPPggQsYVcNcyQcuz4w2\"," +
            "\"ConnectionId\":\"05265228-1e2c-46c5-82a1-6a5bcc3f0143\"," +
            "\"KeepAliveTimeout\":10.0," +
            "\"DisconnectTimeout\":5.0," +
            "\"TryWebSockets\":true," +
            "\"ProtocolVersion\":\"1.5\"," +
            "\"TransportConnectTimeout\":30.0," +
            "\"LongPollDelay\":0.0}";

        var responseData = Encoding.UTF8.GetBytes(payload);

        var exception = Assert.Throws<InvalidDataException>(() => NegotiateProtocol.ParseResponse(responseData));

        Assert.Equal("Detected a connection attempt to an ASP.NET SignalR Server. This client only supports connecting to an ASP.NET Core SignalR Server. See https://aka.ms/signalr-core-differences for details.", exception.InnerException.Message);
    }

    [Fact]
    public void WriteNegotiateResponseWithNullAvailableTransports()
    {
        using (MemoryBufferWriter writer = new MemoryBufferWriter())
        {
            NegotiateProtocol.WriteResponse(new NegotiationResponse(), writer);

            string json = Encoding.UTF8.GetString(writer.ToArray());

            Assert.Equal("{\"negotiateVersion\":0,\"availableTransports\":[]}", json);
        }
    }

    [Fact]
    public void WriteNegotiateResponseWithNullTransferFormats()
    {
        using (MemoryBufferWriter writer = new MemoryBufferWriter())
        {
            NegotiateProtocol.WriteResponse(new NegotiationResponse
            {
                AvailableTransports = new List<AvailableTransport>
                    {
                        new AvailableTransport()
                    }
            }, writer);

            string json = Encoding.UTF8.GetString(writer.ToArray());

            Assert.Equal("{\"negotiateVersion\":0,\"availableTransports\":[{\"transport\":null,\"transferFormats\":[]}]}", json);
        }
    }
}
