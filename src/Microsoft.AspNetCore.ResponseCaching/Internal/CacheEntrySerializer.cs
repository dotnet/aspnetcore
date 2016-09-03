// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    internal static class CacheEntrySerializer
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
        // Type (char: 'R' for CachedResponse, 'V' for CachedVaryRules)
        // Type-dependent data (see CachedResponse and CachedVaryRules)
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

            var type = reader.ReadChar();

            if (type == 'R')
            {
                var cachedResponse = ReadCachedResponse(reader);
                return cachedResponse;
            }
            else if (type == 'V')
            {
                var cachedVaryRules = ReadCachedVaryRules(reader);
                return cachedVaryRules;
            }

            // Unable to read as CachedResponse or CachedVaryRules
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
        // ContainsVaryRules (bool)
        // If containing vary rules:
        //   Headers count
        //   Headers if count > 0 (comma separated string)
        //   Params count
        //   Params if count > 0 (comma separated string)
        private static CachedVaryRules ReadCachedVaryRules(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
            {
                return new CachedVaryRules();
            }

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

            return new CachedVaryRules { VaryRules = new VaryRules() { Headers = headers, Params = param } };
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
                writer.Write('R');
                WriteCachedResponse(writer, entry as CachedResponse);
            }
            else if (entry is CachedVaryRules)
            {
                writer.Write('V');
                WriteCachedVaryRules(writer, entry as CachedVaryRules);
            }
            else
            {
                throw new NotSupportedException($"Unrecognized entry format for {nameof(entry)}.");
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
                writer.Write(header.Value);
            }

            writer.Write(entry.Body.Length);
            writer.Write(entry.Body);
        }

        // See serialization format above
        private static void WriteCachedVaryRules(BinaryWriter writer, CachedVaryRules varyRules)
        {
            if (varyRules.VaryRules == null)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);
                writer.Write(varyRules.VaryRules.Headers.Count);
                foreach (var header in varyRules.VaryRules.Headers)
                {
                    writer.Write(header);
                }

                writer.Write(varyRules.VaryRules.Params.Count);
                foreach (var param in varyRules.VaryRules.Params)
                {
                    writer.Write(param);
                }
            }
        }
    }
}
