// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    internal static class ResponseCacheEntrySerializer
    {
        private const int FormatVersion = 1;

        internal static IResponseCacheEntry Deserialize(byte[] serializedEntry)
        {
            if (serializedEntry == null)
            {
                return null;
            }

            using (var memory = new MemoryStream(serializedEntry))
            {
                using (var reader = new BinaryReader(memory))
                {
                    return Read(reader);
                }
            }
        }

        internal static byte[] Serialize(IResponseCacheEntry entry)
        {
            using (var memory = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memory))
                {
                    Write(writer, entry);
                    writer.Flush();
                    return memory.ToArray();
                }
            }
        }

        // Serialization Format
        // Format version (int)
        // Type (char: 'R' for CachedResponse, 'V' for CachedVaryByRules)
        // Type-dependent data (see CachedResponse and CachedVaryByRules)
        private static IResponseCacheEntry Read(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.ReadInt32() != FormatVersion)
            {
                return null;
            }

            var type = reader.ReadChar();

            if (type == 'R')
            {
                return ReadCachedResponse(reader);
            }
            else if (type == 'V')
            {
                return ReadCachedVaryByRules(reader);
            }

            // Unable to read as CachedResponse or CachedVaryByRules
            return null;
        }

        // Serialization Format
        // Creation time - UtcTicks (long)
        // Status code (int)
        // Header count (int)
        // Header(s)
        //   Key (string)
        //   ValueCount (int)
        //   Value(s)
        //     Value (string)
        // BodyLength (int)
        // Body (byte[])
        private static CachedResponse ReadCachedResponse(BinaryReader reader)
        {
            var created = new DateTimeOffset(reader.ReadInt64(), TimeSpan.Zero);
            var statusCode = reader.ReadInt32();
            var headerCount = reader.ReadInt32();
            var headers = new HeaderDictionary();
            for (var index = 0; index < headerCount; index++)
            {
                var key = reader.ReadString();
                var headerValueCount = reader.ReadInt32();
                if (headerValueCount > 1)
                {
                    var headerValues = new string[headerValueCount];
                    for (var valueIndex = 0; valueIndex < headerValueCount; valueIndex++)
                    {
                        headerValues[valueIndex] = reader.ReadString();
                    }
                    headers[key] = headerValues;
                }
                else if (headerValueCount == 1)
                {
                    headers[key] = reader.ReadString();
                }
            }

            var bodyLength = reader.ReadInt32();
            var bodyBytes = reader.ReadBytes(bodyLength);

            return new CachedResponse
            {
                Created = created,
                StatusCode = statusCode,
                Headers = headers,
                Body = new MemoryStream(bodyBytes, writable: false)
            };
        }

        // Serialization Format
        // VaryKeyPrefix (string)
        // Headers count
        // Header(s) (comma separated string)
        // QueryKey count
        // QueryKey(s) (comma separated string)
        private static CachedVaryByRules ReadCachedVaryByRules(BinaryReader reader)
        {
            var varyKeyPrefix = reader.ReadString();

            var headerCount = reader.ReadInt32();
            var headers = new string[headerCount];
            for (var index = 0; index < headerCount; index++)
            {
                headers[index] = reader.ReadString();
            }
            var queryKeysCount = reader.ReadInt32();
            var queryKeys = new string[queryKeysCount];
            for (var index = 0; index < queryKeysCount; index++)
            {
                queryKeys[index] = reader.ReadString();
            }

            return new CachedVaryByRules { VaryByKeyPrefix = varyKeyPrefix, Headers = headers, QueryKeys = queryKeys };
        }

        // See serialization format above
        private static void Write(BinaryWriter writer, IResponseCacheEntry entry)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            writer.Write(FormatVersion);

            if (entry is CachedResponse)
            {
                writer.Write('R');
                WriteCachedResponse(writer, (CachedResponse)entry);
            }
            else if (entry is CachedVaryByRules)
            {
                writer.Write('V');
                WriteCachedVaryByRules(writer, (CachedVaryByRules)entry);
            }
            else
            {
                throw new NotSupportedException($"Unrecognized entry type for {nameof(entry)}.");
            }
        }

        // See serialization format above
        private static void WriteCachedResponse(BinaryWriter writer, CachedResponse entry)
        {
            writer.Write(entry.Created.UtcTicks);
            writer.Write(entry.StatusCode);
            writer.Write(entry.Headers.Count);
            foreach (var header in entry.Headers)
            {
                writer.Write(header.Key);
                writer.Write(header.Value.Count);
                foreach (var headerValue in header.Value)
                {
                    writer.Write(headerValue);
                }
            }

            if (entry.Body.CanSeek)
            {
                if (entry.Body.Length > int.MaxValue)
                {
                    throw new NotSupportedException($"{nameof(entry.Body)} is too large to serialized.");
                }

                var bodyLength = (int)entry.Body.Length;
                var bodyBytes = new byte[bodyLength];
                var bytesRead = entry.Body.Read(bodyBytes, 0, bodyLength);

                if (bytesRead != bodyLength)
                {
                    throw new InvalidOperationException($"Failed to fully read {nameof(entry.Body)}.");
                }

                writer.Write(bodyLength);
                writer.Write(bodyBytes);
            }
            else
            {
                var stream = new MemoryStream();
                entry.Body.CopyTo(stream);

                if (stream.Length > int.MaxValue)
                {
                    throw new NotSupportedException($"{nameof(entry.Body)} is too large to serialized.");
                }

                var bodyLength = (int)stream.Length;
                writer.Write(bodyLength);
                writer.Write(stream.ToArray());

            }
        }

        // See serialization format above
        private static void WriteCachedVaryByRules(BinaryWriter writer, CachedVaryByRules varyByRules)
        {
            writer.Write(varyByRules.VaryByKeyPrefix);

            writer.Write(varyByRules.Headers.Count);
            foreach (var header in varyByRules.Headers)
            {
                writer.Write(header);
            }

            writer.Write(varyByRules.QueryKeys.Count);
            foreach (var queryKey in varyByRules.QueryKeys)
            {
                writer.Write(queryKey);
            }
        }
    }
}
