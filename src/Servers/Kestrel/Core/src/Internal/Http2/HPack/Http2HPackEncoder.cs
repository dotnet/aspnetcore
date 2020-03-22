// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.HPack;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
    internal class Http2HPackEncoder
    {
        // Internal for testing
        internal readonly HPackHeaderEntry Head;

        private readonly bool _disableDynamicCompression;
        private readonly HPackHeaderEntry[] _headerBuckets;
        private readonly byte _hashMask;
        private uint _headerTableSize;
        private uint _maxHeaderTableSize;
        private bool _pendingTableSizeUpdate;
        private HPackHeaderEntry _removed;

        public Http2HPackEncoder(bool disableDynamicCompression = false, uint maxHeaderTableSize = Http2PeerSettings.DefaultHeaderTableSize)
        {
            _disableDynamicCompression = disableDynamicCompression;
            _maxHeaderTableSize = maxHeaderTableSize;
            Head = new HPackHeaderEntry();
            Head.Initialize(-1, string.Empty, string.Empty, int.MaxValue, null);
            // Bucket count balances memory usage and the expected low number of headers (constrained by the header table size).
            // Performance with different bucket counts hasn't been measured in detail.
            _headerBuckets = new HPackHeaderEntry[16];
            _hashMask = (byte)(_headerBuckets.Length - 1);
            Head.Before = Head.After = Head;
        }

        public void UpdateMaxHeaderTableSize(uint maxHeaderTableSize)
        {
            if (_maxHeaderTableSize != maxHeaderTableSize)
            {
                _maxHeaderTableSize = maxHeaderTableSize;
                _pendingTableSizeUpdate = true;

                // Check capacity and remove entries that exceed the new capacity
                EnsureCapacity(0);
            }
        }

        /// <summary>
        /// Begin encoding headers in the first HEADERS frame.
        /// </summary>
        public bool BeginEncodeHeaders(int statusCode, Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, out int length)
        {
            length = 0;

            if (_pendingTableSizeUpdate)
            {
                if (!HPackEncoder.EncodeDynamicTableSizeUpdate((int)_maxHeaderTableSize, buffer, out var sizeUpdateLength))
                {
                    throw new HPackEncodingException(SR.net_http_hpack_encode_failure);
                }
                length += sizeUpdateLength;
                _pendingTableSizeUpdate = false;
            }

            if (!EncodeStatusHeader(statusCode, buffer.Slice(length), out var statusCodeLength))
            {
                throw new HPackEncodingException(SR.net_http_hpack_encode_failure);
            }
            length += statusCodeLength;

            if (!headersEnumerator.MoveNext())
            {
                return true;
            }

            // We're ok with not throwing if no headers were encoded because we've already encoded the status.
            // There is a small chance that the header will encode if there is no other content in the next HEADERS frame.
            var done = EncodeHeadersCore(headersEnumerator, buffer.Slice(length), throwIfNoneEncoded: false, out var headersLength);
            length += headersLength;
            return done;
        }

        /// <summary>
        /// Begin encoding headers in the first HEADERS frame.
        /// </summary>
        public bool BeginEncodeHeaders(Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, out int length)
        {
            length = 0;

            if (_pendingTableSizeUpdate)
            {
                if (!HPackEncoder.EncodeDynamicTableSizeUpdate((int)_maxHeaderTableSize, buffer, out var sizeUpdateLength))
                {
                    throw new HPackEncodingException(SR.net_http_hpack_encode_failure);
                }
                length += sizeUpdateLength;
                _pendingTableSizeUpdate = false;
            }

            if (!headersEnumerator.MoveNext())
            {
                return true;
            }

            var done = EncodeHeadersCore(headersEnumerator, buffer.Slice(length), throwIfNoneEncoded: true, out var headersLength);
            length += headersLength;
            return done;
        }

        /// <summary>
        /// Continue encoding headers in the next HEADERS frame. The enumerator should already have a current value.
        /// </summary>
        public bool ContinueEncodeHeaders(Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, out int length)
        {
            return EncodeHeadersCore(headersEnumerator, buffer, throwIfNoneEncoded: true, out length);
        }

        private bool EncodeStatusHeader(int statusCode, Span<byte> buffer, out int length)
        {
            switch (statusCode)
            {
                case 200:
                case 204:
                case 206:
                case 304:
                case 400:
                case 404:
                case 500:
                    // Status codes which exist in the HTTP/2 StaticTable.
                    return HPackEncoder.EncodeIndexedHeaderField(H2StaticTable.StatusIndex[statusCode], buffer, out length);
                default:
                    const string name = ":status";
                    var value = StatusCodes.ToStatusString(statusCode);
                    return EncodeHeader(buffer, H2StaticTable.Status200, name, value, out length);
            }
        }

        private bool EncodeHeadersCore(Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, bool throwIfNoneEncoded, out int length)
        {
            var currentLength = 0;
            do
            {
                if (!EncodeHeader(
                    buffer.Slice(currentLength),
                    headersEnumerator.HPackStaticTableId,
                    headersEnumerator.Current.Key,
                    headersEnumerator.Current.Value,
                    out var headerLength))
                {
                    // If the header wasn't written, and no headers have been written, then the header is too large.
                    // Throw an error to avoid an infinite loop of attempting to write large header.
                    if (currentLength == 0 && throwIfNoneEncoded)
                    {
                        throw new HPackEncodingException(SR.net_http_hpack_encode_failure);
                    }

                    length = currentLength;
                    return false;
                }

                currentLength += headerLength;
            }
            while (headersEnumerator.MoveNext());

            length = currentLength;
            return true;
        }

        private bool EncodeHeader(Span<byte> buffer, int staticTableIndex, string name, string value, out int bytesWritten)
        {
            // Never index sensitive value.
            if (IsSensitive(staticTableIndex, name))
            {
                var index = ResolveDynamicTableIndex(staticTableIndex, name);

                return index == -1
                    ? HPackEncoder.EncodeLiteralHeaderFieldNeverIndexingNewName(name, value, buffer, out bytesWritten)
                    : HPackEncoder.EncodeLiteralHeaderFieldNeverIndexing(index, value, buffer, out bytesWritten);
            }

            // No dynamic table. Only use the static table.
            if (_disableDynamicCompression || _maxHeaderTableSize == 0 || IsNotDynamicallyIndexed(staticTableIndex))
            {
                return staticTableIndex == -1
                    ? HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingNewName(name, value, buffer, out bytesWritten)
                    : HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexing(staticTableIndex, value, buffer, out bytesWritten);
            }

            // Header is greater than the maximum table size.
            // Don't attempt to add dynamic header as all existing dynamic headers will be removed.
            if (HeaderField.GetLength(name.Length, value.Length) > _maxHeaderTableSize)
            {
                var index = ResolveDynamicTableIndex(staticTableIndex, name);

                return index == -1
                    ? HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingNewName(name, value, buffer, out bytesWritten)
                    : HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexing(index, value, buffer, out bytesWritten);
            }

            return EncodeDynamicHeader(buffer, staticTableIndex, name, value, out bytesWritten);
        }

        private bool IsSensitive(int staticTableIndex, string name)
        {
            // Set-Cookie could contain sensitive data.
            if (staticTableIndex == H2StaticTable.SetCookie)
            {
                return true;
            }
            if (string.Equals(name, "Content-Disposition", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private bool IsNotDynamicallyIndexed(int staticTableIndex)
        {
            // Content-Length is added to static content. Content length is different for each
            // file, and is unlikely to be reused because of browser caching.
            return staticTableIndex == H2StaticTable.ContentLength;
        }

        private int ResolveDynamicTableIndex(int staticTableIndex, string name)
        {
            if (staticTableIndex != -1)
            {
                // Prefer static table index.
                return staticTableIndex;
            }

            return CalculateDynamicTableIndex(name);
        }

        private bool EncodeDynamicHeader(Span<byte> buffer, int staticTableIndex, string name, string value, out int bytesWritten)
        {
            var headerField = GetEntry(name, value);
            if (headerField != null)
            {
                // Already exists in dynamic table. Write index.
                var index = CalculateDynamicTableIndex(headerField.Index);
                return HPackEncoder.EncodeIndexedHeaderField(index, buffer, out bytesWritten);
            }
            else
            {
                // Doesn't exist in dynamic table. Add new entry to dynamic table.
                var headerSize = (uint)HeaderField.GetLength(name.Length, value.Length);

                var index = ResolveDynamicTableIndex(staticTableIndex, name);
                var success = index == -1
                    ? HPackEncoder.EncodeLiteralHeaderFieldIndexingNewName(name, value, buffer, out bytesWritten)
                    : HPackEncoder.EncodeLiteralHeaderFieldIndexing(index, value, buffer, out bytesWritten);

                if (success)
                {
                    EnsureCapacity(headerSize);
                    AddHeaderEntry(name, value, headerSize);
                }

                return success;
            }
        }

        /// <summary>
        /// Ensure there is capacity for the new header. If there is not enough capacity then remove
        /// existing headers until space is available.
        /// </summary>
        private void EnsureCapacity(uint headerSize)
        {
            Debug.Assert(headerSize <= _maxHeaderTableSize, "Header is bigger than dynamic table size.");

            while (_maxHeaderTableSize - _headerTableSize < headerSize)
            {
                var removed = RemoveHeaderEntry();
                // Removed entries are tracked to be reused.
                PushRemovedEntry(removed);
            }
        }

        private HPackHeaderEntry GetEntry(string name, string value)
        {
            if (_headerTableSize == 0)
            {
                return null;
            }
            var hash = name.GetHashCode();
            var bucketIndex = CalculateBucketIndex(hash);
            for (var e = _headerBuckets[bucketIndex]; e != null; e = e.Next)
            {
                // We've already looked up entries based on a hash of the name.
                // Compare value before name as it is more likely to be different.
                if (e.Hash == hash &&
                    string.Equals(value, e.Value, StringComparison.Ordinal) &&
                    string.Equals(name, e.Name, StringComparison.Ordinal))
                {
                    return e;
                }
            }
            return null;
        }

        private int CalculateDynamicTableIndex(string name)
        {
            if (_headerTableSize == 0)
            {
                return -1;
            }
            var hash = name.GetHashCode();
            var bucketIndex = CalculateBucketIndex(hash);
            for (var e = _headerBuckets[bucketIndex]; e != null; e = e.Next)
            {
                if (e.Hash == hash && string.Equals(name, e.Name, StringComparison.Ordinal))
                {
                    return CalculateDynamicTableIndex(e.Index);
                }
            }
            return -1;
        }

        private int CalculateDynamicTableIndex(int index)
        {
            return index == -1 ? -1 : index - Head.Before.Index + 1 + H2StaticTable.Count;
        }

        private void AddHeaderEntry(string name, string value, uint headerSize)
        {
            Debug.Assert(headerSize <= _maxHeaderTableSize, "Header is bigger than dynamic table size.");
            Debug.Assert(headerSize <= _maxHeaderTableSize - _headerTableSize, "Not enough room in dynamic table.");

            var hash = name.GetHashCode();
            var bucketIndex = CalculateBucketIndex(hash);
            var oldEntry = _headerBuckets[bucketIndex];
            // Attempt to reuse removed entry
            var newEntry = PopRemovedEntry() ?? new HPackHeaderEntry();
            newEntry.Initialize(hash, name, value, Head.Before.Index - 1, oldEntry);
            _headerBuckets[bucketIndex] = newEntry;
            newEntry.AddBefore(Head);
            _headerTableSize += headerSize;
        }

        private void PushRemovedEntry(HPackHeaderEntry removed)
        {
            if (_removed != null)
            {
                removed.Next = _removed;
            }
            _removed = removed;
        }

        private HPackHeaderEntry PopRemovedEntry()
        {
            if (_removed != null)
            {
                var removed = _removed;
                _removed = _removed.Next;
                return removed;
            }

            return null;
        }

        /// <summary>
        /// Remove the oldest entry.
        /// </summary>
        private HPackHeaderEntry RemoveHeaderEntry()
        {
            if (_headerTableSize == 0)
            {
                return null;
            }
            var eldest = Head.After;
            var hash = eldest.Hash;
            var bucketIndex = CalculateBucketIndex(hash);
            var prev = _headerBuckets[bucketIndex];
            var e = prev;
            while (e != null)
            {
                HPackHeaderEntry next = e.Next;
                if (e == eldest)
                {
                    if (prev == eldest)
                    {
                        _headerBuckets[bucketIndex] = next;
                    }
                    else
                    {
                        prev.Next = next;
                    }
                    _headerTableSize -= eldest.CalculateSize();
                    eldest.Remove();
                    return eldest;
                }
                prev = e;
                e = next;
            }
            return null;
        }

        private int CalculateBucketIndex(int hash)
        {
            return hash & _hashMask;
        }
    }
}
