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
        // Use C#7.3's ReadOnlySpan<byte> optimization for static data https://vcsjones.com/2019/02/01/csharp-readonly-span-bytes-static/
        private const string ConnectionIdPropertyName = "connectionId";
        private static ReadOnlySpan<byte> ConnectionIdPropertyNameBytes => new byte[] { (byte)'c', (byte)'o', (byte)'n', (byte)'n', (byte)'e', (byte)'c', (byte)'t', (byte)'i', (byte)'o', (byte)'n', (byte)'I', (byte)'d' };
        private const string UrlPropertyName = "url";
        private static ReadOnlySpan<byte> UrlPropertyNameBytes => new byte[] { (byte)'u', (byte)'r', (byte)'l' };
        private const string AccessTokenPropertyName = "accessToken";
        private static ReadOnlySpan<byte> AccessTokenPropertyNameBytes => new byte[] { (byte)'a', (byte)'c', (byte)'c', (byte)'e', (byte)'s', (byte)'s', (byte)'T', (byte)'o', (byte)'k', (byte)'e', (byte)'n' };
        private const string AvailableTransportsPropertyName = "availableTransports";
        private static ReadOnlySpan<byte> AvailableTransportsPropertyNameBytes => new byte[] { (byte)'a', (byte)'v', (byte)'a', (byte)'i', (byte)'l', (byte)'a', (byte)'b', (byte)'l', (byte)'e', (byte)'T', (byte)'r', (byte)'a', (byte)'n', (byte)'s', (byte)'p', (byte)'o', (byte)'r', (byte)'t', (byte)'s' };
        private const string TransportPropertyName = "transport";
        private static ReadOnlySpan<byte> TransportPropertyNameBytes => new byte[] { (byte)'t', (byte)'r', (byte)'a', (byte)'n', (byte)'s', (byte)'p', (byte)'o', (byte)'r', (byte)'t' };
        private const string TransferFormatsPropertyName = "transferFormats";
        private static ReadOnlySpan<byte> TransferFormatsPropertyNameBytes => new byte[] { (byte)'t', (byte)'r', (byte)'a', (byte)'n', (byte)'s', (byte)'f', (byte)'e', (byte)'r', (byte)'F', (byte)'o', (byte)'r', (byte)'m', (byte)'a', (byte)'t', (byte)'s' };
        private const string ErrorPropertyName = "error";
        private static ReadOnlySpan<byte> ErrorPropertyNameBytes => new byte[] { (byte)'e', (byte)'r', (byte)'r', (byte)'o', (byte)'r' };

        // Used to detect ASP.NET SignalR Server connection attempt
        private const string ProtocolVersionPropertyName = "ProtocolVersion";
        private static ReadOnlySpan<byte> ProtocolVersionPropertyNameBytes => new byte[] { (byte)'P', (byte)'r', (byte)'o', (byte)'t', (byte)'o', (byte)'c', (byte)'o', (byte)'l', (byte)'V', (byte)'e', (byte)'r', (byte)'s', (byte)'i', (byte)'o', (byte)'n' };

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

                reader.CheckRead();
                reader.EnsureObjectStart();

                string connectionId = null;
                string url = null;
                string accessToken = null;
                List<AvailableTransport> availableTransports = null;
                string error = null;

                var completed = false;
                while (!completed && reader.CheckRead())
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            var memberName = reader.ValueSpan;

                            if (memberName.SequenceEqual(UrlPropertyNameBytes))
                            {
                                url = reader.ReadAsString(UrlPropertyName);
                            }
                            else if (memberName.SequenceEqual(AccessTokenPropertyNameBytes))
                            {
                                accessToken = reader.ReadAsString(AccessTokenPropertyName);
                            }
                            else if (memberName.SequenceEqual(ConnectionIdPropertyNameBytes))
                            {
                                connectionId = reader.ReadAsString(ConnectionIdPropertyName);
                            }
                            else if (memberName.SequenceEqual(AvailableTransportsPropertyNameBytes))
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
                            else if (memberName.SequenceEqual(ErrorPropertyNameBytes))
                            {
                                error = reader.ReadAsString(ErrorPropertyName);
                            }
                            else if (memberName.SequenceEqual(ProtocolVersionPropertyNameBytes))
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

        /// <summary>
        /// <para>
        ///     This method is obsolete and will be removed in a future version.
        ///     The recommended alternative is <see cref="ParseResponse(ReadOnlySpan{byte})" />.
        /// </para>
        /// </summary>
        [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is ParseResponse(ReadOnlySpan{byte}).")]
        public static NegotiationResponse ParseResponse(Stream content) =>
            throw new NotSupportedException("This method is obsolete and will be removed in a future version. The recommended alternative is ParseResponse(ReadOnlySpan{byte}).");

        private static AvailableTransport ParseAvailableTransport(ref Utf8JsonReader reader)
        {
            var availableTransport = new AvailableTransport();

            while (reader.CheckRead())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        var memberName = reader.ValueSpan;

                        if (memberName.SequenceEqual(TransportPropertyNameBytes))
                        {
                            availableTransport.Transport = reader.ReadAsString(TransportPropertyName);
                        }
                        else if (memberName.SequenceEqual(TransferFormatsPropertyNameBytes))
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
