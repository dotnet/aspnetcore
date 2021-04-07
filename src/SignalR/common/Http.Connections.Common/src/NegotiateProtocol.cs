// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Http.Connections
{
    /// <summary>
    /// The protocol for reading and writing negotiate requests and responses.
    /// </summary>
    public static class NegotiateProtocol
    {
        private const string ConnectionIdPropertyName = "connectionId";
        private static JsonEncodedText ConnectionIdPropertyNameBytes = JsonEncodedText.Encode(ConnectionIdPropertyName);
        private const string ConnectionTokenPropertyName = "connectionToken";
        private static JsonEncodedText ConnectionTokenPropertyNameBytes = JsonEncodedText.Encode(ConnectionTokenPropertyName);
        private const string UrlPropertyName = "url";
        private static JsonEncodedText UrlPropertyNameBytes = JsonEncodedText.Encode(UrlPropertyName);
        private const string AccessTokenPropertyName = "accessToken";
        private static JsonEncodedText AccessTokenPropertyNameBytes = JsonEncodedText.Encode(AccessTokenPropertyName);
        private const string AvailableTransportsPropertyName = "availableTransports";
        private static JsonEncodedText AvailableTransportsPropertyNameBytes = JsonEncodedText.Encode(AvailableTransportsPropertyName);
        private const string TransportPropertyName = "transport";
        private static JsonEncodedText TransportPropertyNameBytes = JsonEncodedText.Encode(TransportPropertyName);
        private const string TransferFormatsPropertyName = "transferFormats";
        private static JsonEncodedText TransferFormatsPropertyNameBytes = JsonEncodedText.Encode(TransferFormatsPropertyName);
        private const string ErrorPropertyName = "error";
        private static JsonEncodedText ErrorPropertyNameBytes = JsonEncodedText.Encode(ErrorPropertyName);
        private const string NegotiateVersionPropertyName = "negotiateVersion";
        private static JsonEncodedText NegotiateVersionPropertyNameBytes = JsonEncodedText.Encode(NegotiateVersionPropertyName);

        // Use C#7.3's ReadOnlySpan<byte> optimization for static data https://vcsjones.com/2019/02/01/csharp-readonly-span-bytes-static/
        // Used to detect ASP.NET SignalR Server connection attempt
        private static ReadOnlySpan<byte> ProtocolVersionPropertyNameBytes => new byte[] { (byte)'P', (byte)'r', (byte)'o', (byte)'t', (byte)'o', (byte)'c', (byte)'o', (byte)'l', (byte)'V', (byte)'e', (byte)'r', (byte)'s', (byte)'i', (byte)'o', (byte)'n' };

        /// <summary>
        /// Writes the <paramref name="response"/> to the <paramref name="output"/>.
        /// </summary>
        /// <param name="response">The negotiation response generated in response to a negotiation request.</param>
        /// <param name="output">Where the <paramref name="response"/> is written to as Json.</param>
        public static void WriteResponse(NegotiationResponse response, IBufferWriter<byte> output)
        {
            var reusableWriter = ReusableUtf8JsonWriter.Get(output);

            try
            {
                var writer = reusableWriter.GetJsonWriter();
                writer.WriteStartObject();

                // If we already have an error its due to a protocol version incompatibility.
                // We can just write the error and complete the JSON object and return.
                if (!string.IsNullOrEmpty(response.Error))
                {
                    writer.WriteString(ErrorPropertyNameBytes, response.Error);
                    writer.WriteEndObject();
                    writer.Flush();
                    Debug.Assert(writer.CurrentDepth == 0);
                    return;
                }

                writer.WriteNumber(NegotiateVersionPropertyNameBytes, response.Version);

                if (!string.IsNullOrEmpty(response.Url))
                {
                    writer.WriteString(UrlPropertyNameBytes, response.Url);
                }

                if (!string.IsNullOrEmpty(response.AccessToken))
                {
                    writer.WriteString(AccessTokenPropertyNameBytes, response.AccessToken);
                }

                if (!string.IsNullOrEmpty(response.ConnectionId))
                {
                    writer.WriteString(ConnectionIdPropertyNameBytes, response.ConnectionId);
                }

                if (response.Version > 0 && !string.IsNullOrEmpty(response.ConnectionToken))
                {
                    writer.WriteString(ConnectionTokenPropertyNameBytes, response.ConnectionToken);
                }

                writer.WriteStartArray(AvailableTransportsPropertyNameBytes);

                if (response.AvailableTransports != null)
                {
                    var transportCount = response.AvailableTransports.Count;
                    for (var i = 0; i < transportCount; ++i)
                    {
                        var availableTransport = response.AvailableTransports[i];
                        writer.WriteStartObject();
                        if (availableTransport.Transport != null)
                        {
                            writer.WriteString(TransportPropertyNameBytes, availableTransport.Transport);
                        }
                        else
                        {
                            // Might be able to remove this after https://github.com/dotnet/corefx/issues/34632 is resolved
                            writer.WriteNull(TransportPropertyNameBytes);
                        }
                        writer.WriteStartArray(TransferFormatsPropertyNameBytes);

                        if (availableTransport.TransferFormats != null)
                        {
                            var formatCount = availableTransport.TransferFormats.Count;
                            for (var j = 0; j < formatCount; ++j)
                            {
                                writer.WriteStringValue(availableTransport.TransferFormats[j]);
                            }
                        }

                        writer.WriteEndArray();
                        writer.WriteEndObject();
                    }
                }

                writer.WriteEndArray();
                writer.WriteEndObject();

                writer.Flush();
                Debug.Assert(writer.CurrentDepth == 0);
            }
            finally
            {
                ReusableUtf8JsonWriter.Return(reusableWriter);
            }
        }

        /// <summary>
        /// Parses a <see cref="NegotiationResponse"/> from the <paramref name="content"/> as Json.
        /// </summary>
        /// <param name="content">The bytes of a Json payload that represents a <see cref="NegotiationResponse"/>.</param>
        /// <returns>The parsed <see cref="NegotiationResponse"/>.</returns>
        public static NegotiationResponse ParseResponse(ReadOnlySpan<byte> content)
        {
            try
            {
                var reader = new Utf8JsonReader(content, isFinalBlock: true, state: default);

                reader.CheckRead();
                reader.EnsureObjectStart();

                string? connectionId = null;
                string? connectionToken = null;
                string? url = null;
                string? accessToken = null;
                List<AvailableTransport>? availableTransports = null;
                string? error = null;
                int version = 0;

                var completed = false;
                while (!completed && reader.CheckRead())
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            if (reader.ValueTextEquals(UrlPropertyNameBytes.EncodedUtf8Bytes))
                            {
                                url = reader.ReadAsString(UrlPropertyName);
                            }
                            else if (reader.ValueTextEquals(AccessTokenPropertyNameBytes.EncodedUtf8Bytes))
                            {
                                accessToken = reader.ReadAsString(AccessTokenPropertyName);
                            }
                            else if (reader.ValueTextEquals(ConnectionIdPropertyNameBytes.EncodedUtf8Bytes))
                            {
                                connectionId = reader.ReadAsString(ConnectionIdPropertyName);
                            }
                            else if (reader.ValueTextEquals(ConnectionTokenPropertyNameBytes.EncodedUtf8Bytes))
                            {
                                connectionToken = reader.ReadAsString(ConnectionTokenPropertyName);
                            }
                            else if (reader.ValueTextEquals(NegotiateVersionPropertyNameBytes.EncodedUtf8Bytes))
                            {
                                version = reader.ReadAsInt32(NegotiateVersionPropertyName).GetValueOrDefault();
                            }
                            else if (reader.ValueTextEquals(AvailableTransportsPropertyNameBytes.EncodedUtf8Bytes))
                            {
                                reader.CheckRead();
                                reader.EnsureArrayStart();

                                availableTransports = new List<AvailableTransport>();
                                while (reader.CheckRead())
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
                            else if (reader.ValueTextEquals(ErrorPropertyNameBytes.EncodedUtf8Bytes))
                            {
                                error = reader.ReadAsString(ErrorPropertyName);
                            }
                            else if (reader.ValueTextEquals(ProtocolVersionPropertyNameBytes))
                            {
                                throw new InvalidOperationException("Detected a connection attempt to an ASP.NET SignalR Server. This client only supports connecting to an ASP.NET Core SignalR Server. See https://aka.ms/signalr-core-differences for details.");
                            }
                            else
                            {
                                reader.Skip();
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

                    if (version > 0)
                    {
                        if (connectionToken == null)
                        {
                            throw new InvalidDataException($"Missing required property '{ConnectionTokenPropertyName}'.");
                        }
                    }

                    if (availableTransports == null)
                    {
                        throw new InvalidDataException($"Missing required property '{AvailableTransportsPropertyName}'.");
                    }
                }

                return new NegotiationResponse
                {
                    ConnectionId = connectionId,
                    ConnectionToken = connectionToken,
                    Url = url,
                    AccessToken = accessToken,
                    AvailableTransports = availableTransports,
                    Error = error,
                    Version = version
                };
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Invalid negotiation response received.", ex);
            }
        }

        private static AvailableTransport ParseAvailableTransport(ref Utf8JsonReader reader)
        {
            var availableTransport = new AvailableTransport();

            while (reader.CheckRead())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        if (reader.ValueTextEquals(TransportPropertyNameBytes.EncodedUtf8Bytes))
                        {
                            availableTransport.Transport = reader.ReadAsString(TransportPropertyName);
                        }
                        else if (reader.ValueTextEquals(TransferFormatsPropertyNameBytes.EncodedUtf8Bytes))
                        {
                            reader.CheckRead();
                            reader.EnsureArrayStart();

                            var completed = false;

                            availableTransport.TransferFormats = new List<string>();
                            while (!completed && reader.CheckRead())
                            {
                                switch (reader.TokenType)
                                {
                                    case JsonTokenType.String:
                                        availableTransport.TransferFormats.Add(reader.GetString()!);
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
                            reader.Skip();
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
    }
}
