// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class MetadataCache
    {
        // Store 1000 entries -- arbitrary number
        private const int CacheSize = 1000;
        private readonly ConcurrentLruCache<string, MetadataCacheEntry> _metadataCache =
            new ConcurrentLruCache<string, MetadataCacheEntry>(CacheSize, StringComparer.OrdinalIgnoreCase);

        // For testing purposes only.
        internal ConcurrentLruCache<string, MetadataCacheEntry> Cache => _metadataCache;

        internal Metadata GetMetadata(string fullPath)
        {
            var timestamp = GetFileTimeStamp(fullPath);

            // Check if we have an entry in the dictionary.
            if (_metadataCache.TryGetValue(fullPath, out var entry))
            {
                if (timestamp.HasValue && timestamp.Value == entry.Timestamp)
                {
                    // The file has not changed since we cached it. Return the cached entry.
                    return entry.Metadata;
                }
                else
                {
                    // The file has changed recently. Remove the cache entry.
                    _metadataCache.Remove(fullPath);
                }
            }

            Metadata metadata;
            using (var fileStream = File.OpenRead(fullPath))
            {
                metadata = AssemblyMetadata.CreateFromStream(fileStream, PEStreamOptions.PrefetchMetadata);
            }

            _metadataCache.GetOrAdd(fullPath, new MetadataCacheEntry(timestamp.Value, metadata));

            return metadata;
        }

        private static DateTime? GetFileTimeStamp(string fullPath)
        {
            try
            {
                Debug.Assert(Path.IsPathRooted(fullPath));

                return File.GetLastWriteTimeUtc(fullPath);
            }
            catch (Exception e)
            {
                // There are several exceptions that can occur here: NotSupportedException or PathTooLongException
                // for a bad path, UnauthorizedAccessException for access denied, etc. Rather than listing them all,
                // just catch all exceptions and log.
                ServerLogger.LogException(e, $"Error getting timestamp of file {fullPath}.");

                return null;
            }
        }

        internal struct MetadataCacheEntry
        {
            public MetadataCacheEntry(DateTime timestamp, Metadata metadata)
            {
                Debug.Assert(timestamp.Kind == DateTimeKind.Utc);

                Timestamp = timestamp;
                Metadata = metadata;
            }

            public DateTime Timestamp { get; }

            public Metadata Metadata { get; }
        }
    }
}
