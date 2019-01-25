// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Http.Connections
{
    public static class NegotiateProtocol
    {
        private const string ConnectionIdPropertyName = "connectionId";
        private static readonly byte[] ConnectionIdPropertyNameBytes = Encoding.UTF8.GetBytes(ConnectionIdPropertyName);
        private const string UrlPropertyName = "url";
        private static readonly byte[] UrlPropertyNameBytes = Encoding.UTF8.GetBytes(UrlPropertyName);
        private const string AccessTokenPropertyName = "accessToken";
        private static readonly byte[] AccessTokenPropertyNameBytes = Encoding.UTF8.GetBytes(AccessTokenPropertyName);
        private const string AvailableTransportsPropertyName = "availableTransports";
        private static readonly byte[] AvailableTransportsPropertyNameBytes = Encoding.UTF8.GetBytes(AvailableTransportsPropertyName);
        private const string TransportPropertyName = "transport";
        private static readonly byte[] TransportPropertyNameBytes = Encoding.UTF8.GetBytes(TransportPropertyName);
        private const string TransferFormatsPropertyName = "transferFormats";
        private static readonly byte[] TransferFormatsPropertyNameBytes = Encoding.UTF8.GetBytes(TransferFormatsPropertyName);
        private const string ErrorPropertyName = "error";
        private static readonly byte[] ErrorPropertyNameBytes = Encoding.UTF8.GetBytes(ErrorPropertyName);

        // Used to detect ASP.NET SignalR Server connection attempt
        private const string ProtocolVersionPropertyName = "ProtocolVersion";
        private static readonly byte[] ProtocolVersionPropertyNameBytes = Encoding.UTF8.GetBytes(ProtocolVersionPropertyName);

        public static void WriteResponse(NegotiationResponse response, IBufferWriter<byte> output)
        {
            var writer = new Utf8JsonWriter(output, new JsonWriterState(new JsonWriterOptions() { SkipValidation = true }));
            writer.WriteStartObject();

            if (!string.IsNullOrEmpty(response.Url))
            {
                writer.WriteString(UrlPropertyNameBytes, response.Url, escape: false);
            }

            if (!string.IsNullOrEmpty(response.AccessToken))
            {
                writer.WriteString(AccessTokenPropertyNameBytes, response.AccessToken, escape: false);
            }

            if (!string.IsNullOrEmpty(response.ConnectionId))
            {
                writer.WriteString(ConnectionIdPropertyNameBytes, response.ConnectionId, escape: false);
            }

            writer.WriteStartArray(AvailableTransportsPropertyNameBytes, escape: false);

            if (response.AvailableTransports != null)
            {
                foreach (var availableTransport in response.AvailableTransports)
                {
                    writer.WriteStartObject();
                    if (availableTransport.Transport != null)
                    {
                        writer.WriteString(TransportPropertyNameBytes, availableTransport.Transport, escape: false);
                    }
                    else
                    {
                        // Might be able to remove this after https://github.com/dotnet/corefx/issues/34632 is resolved
                        writer.WriteNull(TransportPropertyNameBytes, escape: false);
                    }
                    writer.WriteStartArray(TransferFormatsPropertyNameBytes, escape: false);

                    if (availableTransport.TransferFormats != null)
                    {
                        foreach (var transferFormat in availableTransport.TransferFormats)
                        {
                            writer.WriteStringValue(transferFormat, escape: false);
                        }
                    }

                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }
            }

            writer.WriteEndArray();
            writer.WriteEndObject();

            writer.Flush(isFinalBlock: true);
        }

        public static NegotiationResponse ParseResponse(ReadOnlySpan<byte> content)
        {
            try
            {
                var reader = new Utf8JsonReader(content, isFinalBlock: true, state: default);

                CheckRead(ref reader);
                EnsureObjectStart(ref reader);

                string connectionId = null;
                string url = null;
                string accessToken = null;
                List<AvailableTransport> availableTransports = null;
                string error = null;

                var completed = false;
                while (!completed && CheckRead(ref reader))
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            var memberName = reader.ValueSpan;

                            if (memberName.SequenceEqual(UrlPropertyNameBytes))
                            {
                                url = ReadAsString(ref reader, UrlPropertyNameBytes);
                            }
                            else if (memberName.SequenceEqual(AccessTokenPropertyNameBytes))
                            {
                                accessToken = ReadAsString(ref reader, AccessTokenPropertyNameBytes);
                            }
                            else if (memberName.SequenceEqual(ConnectionIdPropertyNameBytes))
                            {
                                connectionId = ReadAsString(ref reader, ConnectionIdPropertyNameBytes);
                            }
                            else if (memberName.SequenceEqual(AvailableTransportsPropertyNameBytes))
                            {
                                CheckRead(ref reader);
                                EnsureArrayStart(ref reader);

                                availableTransports = new List<AvailableTransport>();
                                while (CheckRead(ref reader))
                                {
                                    if (reader.TokenType == JsonTokenType.StartObject)
                                    {
                                        availableTransports.Add(ParseAvailableTransport(ref reader));
                                    }
                                    else if (reader.TokenType == JsonTokenType.EndArray)
                                    {
                                        break;
                                    }
                                }
                            }
                            else if (memberName.SequenceEqual(ErrorPropertyNameBytes))
                            {
                                error = ReadAsString(ref reader, ErrorPropertyNameBytes);
                            }
                            else if (memberName.SequenceEqual(ProtocolVersionPropertyNameBytes))
                            {
                                throw new InvalidOperationException("Detected a connection attempt to an ASP.NET SignalR Server. This client only supports connecting to an ASP.NET Core SignalR Server. See https://aka.ms/signalr-core-differences for details.");
                            }
                            else
                            {
                                Skip(ref reader);
                            }
                            break;
                        case JsonTokenType.EndObject:
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
            catch (Exception ex)
            {
                throw new InvalidDataException("Invalid negotiation response received.", ex);
            }
        }

        public static NegotiationResponse ParseResponse(Stream content)
        {
            var buffer = new byte[content.Length];
            content.Read(buffer, 0, buffer.Length);
            return ParseResponse(buffer);
        }

        private static AvailableTransport ParseAvailableTransport(ref Utf8JsonReader reader)
        {
            var availableTransport = new AvailableTransport();

            while (CheckRead(ref reader))
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        var memberName = reader.ValueSpan;

                        if (memberName.SequenceEqual(TransportPropertyNameBytes))
                        {
                            availableTransport.Transport = ReadAsString(ref reader, TransportPropertyNameBytes);
                        }
                        else if (memberName.SequenceEqual(TransferFormatsPropertyNameBytes))
                        {
                            CheckRead(ref reader);
                            EnsureArrayStart(ref reader);

                            var completed = false;

                            availableTransport.TransferFormats = new List<string>();
                            while (!completed && CheckRead(ref reader))
                            {
                                switch (reader.TokenType)
                                {
                                    case JsonTokenType.String:
                                        availableTransport.TransferFormats.Add(reader.GetString());
                                        break;
                                    case JsonTokenType.EndArray:
                                        completed = true;
                                        break;
                                    default:
                                        throw new InvalidDataException($"Unexpected token '{reader.TokenType}' when reading transfer formats JSON.");
                                }
                            }
                        }
                        else
                        {
                            Skip(ref reader);
                        }
                        break;
                    case JsonTokenType.EndObject:
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

        private static bool CheckRead(ref Utf8JsonReader reader)
        {
            if (!reader.Read())
            {
                throw new InvalidDataException("Unexpected end when reading JSON.");
            }

            return true;
        }

        private static void EnsureObjectStart(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new InvalidDataException($"Unexpected JSON Token Type '{GetTokenString(reader.TokenType)}'. Expected a JSON Object.");
            }
        }

        private static string GetTokenString(JsonTokenType tokenType)
        {
            switch (tokenType)
            {
                case JsonTokenType.None:
                    break;
                case JsonTokenType.StartObject:
                    return "StartObject";
                case JsonTokenType.StartArray:
                    return "Array";
                case JsonTokenType.PropertyName:
                    return "PropertyName";
                default:
                    break;
            }
            return tokenType.ToString();
        }

        private static void EnsureArrayStart(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new InvalidDataException($"Unexpected JSON Token Type '{GetTokenString(reader.TokenType)}'. Expected a JSON Array.");
            }
        }

        private static void Skip(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                reader.Read();
            }

            if (reader.TokenType == JsonTokenType.StartObject || reader.TokenType == JsonTokenType.StartArray)
            {
                int depth = reader.CurrentDepth;
                while (reader.Read() && depth < reader.CurrentDepth)
                {
                }
            }
        }

        private static string ReadAsString(ref Utf8JsonReader reader, byte[] propertyName)
        {
            reader.Read();
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new InvalidDataException($"Expected '{Encoding.UTF8.GetString(propertyName)}' to be of type {JsonTokenType.String}.");
            }

            return reader.GetString();
        }
    }
}
