// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    internal static class DefaultResponseCacheSerializer
    {
        private const int FormatVersion = 1;

        public static object Deserialize(byte[] serializedEntry)
        {
            using (var memory = new MemoryStream(serializedEntry))
            {
                using (var reader = new BinaryReader(memory))
                {
                    return Read(reader);
                }
            }
        }

        public static byte[] Serialize(object entry)
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
        // Type (string)
        // Type-dependent data (see CachedResponse and CachedVaryBy)
        public static object Read(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.ReadInt32() != FormatVersion)
            {
                return null;
            }

            var type = reader.ReadString();

            if (string.Equals(nameof(CachedResponse), type))
            {
                var cachedResponse = ReadCachedResponse(reader);
                return cachedResponse;
            }
            else if (string.Equals(nameof(CachedVaryBy), type))
            {
                var cachedResponse = ReadCachedVaryBy(reader);
                return cachedResponse;
            }

            // Unable to read as CachedResponse or CachedVaryBy
            return null;
        }

        // Serialization Format
        // Creation time - UtcTicks (long)
        // Status code (int)
        // Header count (int)
        // Header(s)
        //   Key (string)
        //   Value (string)
        // Body length (int)
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
                var value = reader.ReadString();
                headers[key] = value;
            }
            var bodyLength = reader.ReadInt32();
            var body = reader.ReadBytes(bodyLength);

            return new CachedResponse { Created = created, StatusCode = statusCode, Headers = headers, Body = body };
        }

        // Serialization Format
        // Headers count 
        // Headers if count > 0 (comma separated string)
        // Params count 
        // Params if count > 0 (comma separated string)
        private static CachedVaryBy ReadCachedVaryBy(BinaryReader reader)
        {
            var headerCount = reader.ReadInt32();
            var headers = new string[headerCount];
            for (var index = 0; index < headerCount; index++)
            {
                headers[index] = reader.ReadString();
            }
            var paramCount = reader.ReadInt32();
            var param = new string[paramCount];
            for (var index = 0; index < paramCount; index++)
            {
                param[index] = reader.ReadString();
            }

            return new CachedVaryBy { Headers = headers, Params = param };
        }

        // See serialization format above
        public static void Write(BinaryWriter writer, object entry)
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
                WriteCachedResponse(writer, entry as CachedResponse);
            }
            else if (entry is CachedVaryBy)
            {
                WriteCachedVaryBy(writer, entry as CachedVaryBy);
            }
            else
            {
                throw new NotSupportedException($"Unrecognized entry format for {nameof(entry)}.");
            }
        }

        // See serialization format above
        private static void WriteCachedResponse(BinaryWriter writer, CachedResponse entry)
        {
            writer.Write(nameof(CachedResponse));
            writer.Write(entry.Created.UtcTicks);
            writer.Write(entry.StatusCode);
            writer.Write(entry.Headers.Count);
            foreach (var header in entry.Headers)
            {
                writer.Write(header.Key);
                writer.Write(header.Value);
            }

            writer.Write(entry.Body.Length);
            writer.Write(entry.Body);
        }

        // See serialization format above
        private static void WriteCachedVaryBy(BinaryWriter writer, CachedVaryBy entry)
        {
            writer.Write(nameof(CachedVaryBy));

            writer.Write(entry.Headers.Count);
            foreach (var header in entry.Headers)
            {
                writer.Write(header);
            }

            writer.Write(entry.Params.Count);
            foreach (var param in entry.Params)
            {
                writer.Write(param);
            }
        }
    }
}
