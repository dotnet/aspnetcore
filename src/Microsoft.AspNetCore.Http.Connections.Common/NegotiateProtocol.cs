// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Internal;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Http.Connections
{
    public static class NegotiateProtocol
    {
        private const string ConnectionIdPropertyName = "connectionId";
        private const string UrlPropertyName = "url";
        private const string AccessTokenPropertyName = "accessToken";
        private const string AvailableTransportsPropertyName = "availableTransports";
        private const string TransportPropertyName = "transport";
        private const string TransferFormatsPropertyName = "transferFormats";
        private const string ErrorPropertyName = "error";
        // Used to detect ASP.NET SignalR Server connection attempt
        private const string ProtocolVersionPropertyName = "ProtocolVersion";

        public static void WriteResponse(NegotiationResponse response, IBufferWriter<byte> output)
        {
            var textWriter = Utf8BufferTextWriter.Get(output);
            try
            {
                using (var jsonWriter = JsonUtils.CreateJsonTextWriter(textWriter))
                {
                    jsonWriter.WriteStartObject();

                    if (!string.IsNullOrEmpty(response.Url))
                    {
                        jsonWriter.WritePropertyName(UrlPropertyName);
                        jsonWriter.WriteValue(response.Url);
                    }

                    if (!string.IsNullOrEmpty(response.AccessToken))
                    {
                        jsonWriter.WritePropertyName(AccessTokenPropertyName);
                        jsonWriter.WriteValue(response.AccessToken);
                    }

                    if (!string.IsNullOrEmpty(response.ConnectionId))
                    {
                        jsonWriter.WritePropertyName(ConnectionIdPropertyName);
                        jsonWriter.WriteValue(response.ConnectionId);
                    }

                    jsonWriter.WritePropertyName(AvailableTransportsPropertyName);
                    jsonWriter.WriteStartArray();

                    if (response.AvailableTransports != null)
                    {
                        foreach (var availableTransport in response.AvailableTransports)
                        {
                            jsonWriter.WriteStartObject();
                            jsonWriter.WritePropertyName(TransportPropertyName);
                            jsonWriter.WriteValue(availableTransport.Transport);
                            jsonWriter.WritePropertyName(TransferFormatsPropertyName);
                            jsonWriter.WriteStartArray();

                            if (availableTransport.TransferFormats != null)
                            {
                                foreach (var transferFormat in availableTransport.TransferFormats)
                                {
                                    jsonWriter.WriteValue(transferFormat);
                                }
                            }

                            jsonWriter.WriteEndArray();
                            jsonWriter.WriteEndObject();
                        }
                    }

                    jsonWriter.WriteEndArray();
                    jsonWriter.WriteEndObject();

                    jsonWriter.Flush();
                }
            }
            finally
            {
                Utf8BufferTextWriter.Return(textWriter);
            }
        }

        public static NegotiationResponse ParseResponse(Stream content)
        {
            try
            {
                using (var reader = JsonUtils.CreateJsonTextReader(new StreamReader(content)))
                {
                    JsonUtils.CheckRead(reader);
                    JsonUtils.EnsureObjectStart(reader);

                    string connectionId = null;
                    string url = null;
                    string accessToken = null;
                    List<AvailableTransport> availableTransports = null;
                    string error = null;

                    var completed = false;
                    while (!completed && JsonUtils.CheckRead(reader))
                    {
                        switch (reader.TokenType)
                        {
                            case JsonToken.PropertyName:
                                var memberName = reader.Value.ToString();

                                switch (memberName)
                                {
                                    case UrlPropertyName:
                                        url = JsonUtils.ReadAsString(reader, UrlPropertyName);
                                        break;
                                    case AccessTokenPropertyName:
                                        accessToken = JsonUtils.ReadAsString(reader, AccessTokenPropertyName);
                                        break;
                                    case ConnectionIdPropertyName:
                                        connectionId = JsonUtils.ReadAsString(reader, ConnectionIdPropertyName);
                                        break;
                                    case AvailableTransportsPropertyName:
                                        JsonUtils.CheckRead(reader);
                                        JsonUtils.EnsureArrayStart(reader);

                                        availableTransports = new List<AvailableTransport>();
                                        while (JsonUtils.CheckRead(reader))
                                        {
                                            if (reader.TokenType == JsonToken.StartObject)
                                            {
                                                availableTransports.Add(ParseAvailableTransport(reader));
                                            }
                                            else if (reader.TokenType == JsonToken.EndArray)
                                            {
                                                break;
                                            }
                                        }
                                        break;
                                    case ErrorPropertyName:
                                        error = JsonUtils.ReadAsString(reader, ErrorPropertyName);
                                        break;
                                    case ProtocolVersionPropertyName:
                                        throw new InvalidOperationException("Detected a connection attempt to an ASP.NET SignalR Server. This client only supports connecting to an ASP.NET Core SignalR Server. See https://aka.ms/signalr-core-differences for details.");
                                    default:
                                        reader.Skip();
                                        break;
                                }
                                break;
                            case JsonToken.EndObject:
                                completed = true;
                                break;
                            default:
                                throw new InvalidDataException($"Unexpected token '{reader.TokenType}' when reading negotiation response JSON.");
                        }
                    }

                    if (url == null && error == null)
                    {
                        // if url isn't specified or there isn't an error, connectionId and available transports are required
                        if (connectionId == null)
                        {
                            throw new InvalidDataException($"Missing required property '{ConnectionIdPropertyName}'.");
                        }

                        if (availableTransports == null)
                        {
                            throw new InvalidDataException($"Missing required property '{AvailableTransportsPropertyName}'.");
                        }
                    }

                    return new NegotiationResponse
                    {
                        ConnectionId = connectionId,
                        Url = url,
                        AccessToken = accessToken,
                        AvailableTransports = availableTransports,
                        Error = error,
                    };
                }
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Invalid negotiation response received.", ex);
            }
        }

        private static AvailableTransport ParseAvailableTransport(JsonTextReader reader)
        {
            var availableTransport = new AvailableTransport();

            while (JsonUtils.CheckRead(reader))
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        var memberName = reader.Value.ToString();

                        switch (memberName)
                        {
                            case TransportPropertyName:
                                availableTransport.Transport = JsonUtils.ReadAsString(reader, TransportPropertyName);
                                break;
                            case TransferFormatsPropertyName:
                                JsonUtils.CheckRead(reader);
                                JsonUtils.EnsureArrayStart(reader);

                                var completed = false;
                                availableTransport.TransferFormats = new List<string>();
                                while (!completed && JsonUtils.CheckRead(reader))
                                {
                                    switch (reader.TokenType)
                                    {
                                        case JsonToken.String:
                                            availableTransport.TransferFormats.Add(reader.Value.ToString());
                                            break;
                                        case JsonToken.EndArray:
                                            completed = true;
                                            break;
                                        default:
                                            throw new InvalidDataException($"Unexpected token '{reader.TokenType}' when reading transfer formats JSON.");
                                    }
                                }
                                break;
                            default:
                                reader.Skip();
                                break;
                        }
                        break;
                    case JsonToken.EndObject:
                        if (availableTransport.Transport == null)
                        {
                            throw new InvalidDataException($"Missing required property '{TransportPropertyName}'.");
                        }

                        if (availableTransport.TransferFormats == null)
                        {
                            throw new InvalidDataException($"Missing required property '{TransferFormatsPropertyName}'.");
                        }

                        return availableTransport;
                    default:
                        throw new InvalidDataException($"Unexpected token '{reader.TokenType}' when reading available transport JSON.");
                }
            }

            throw new InvalidDataException("Unexpected end when reading JSON.");
        }
    }
}
