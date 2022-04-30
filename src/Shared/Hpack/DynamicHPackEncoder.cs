// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Text;

namespace System.Net.Http.HPack;

internal sealed class DynamicHPackEncoder
{
    public const int DefaultHeaderTableSize = 4096;

    // Internal for testing
    internal readonly EncoderHeaderEntry Head;

    private readonly bool _allowDynamicCompression;
    private readonly EncoderHeaderEntry[] _headerBuckets;
    private readonly byte _hashMask;
    private uint _headerTableSize;
    private uint _maxHeaderTableSize;
    private bool _pendingTableSizeUpdate;
    private EncoderHeaderEntry? _removed;

    internal uint TableSize => _headerTableSize;

    public DynamicHPackEncoder(bool allowDynamicCompression = true, uint maxHeaderTableSize = DefaultHeaderTableSize)
    {
        _allowDynamicCompression = allowDynamicCompression;
        _maxHeaderTableSize = maxHeaderTableSize;
        Head = new EncoderHeaderEntry();
        Head.Initialize(-1, string.Empty, string.Empty, 0, int.MaxValue, null);
        // Bucket count balances memory usage and the expected low number of headers (constrained by the header table size).
        // Performance with different bucket counts hasn't been measured in detail.
        _headerBuckets = new EncoderHeaderEntry[16];
        _hashMask = (byte)(_headerBuckets.Length - 1);
        Head.Before = Head.After = Head;
    }

    public void UpdateMaxHeaderTableSize(uint maxHeaderTableSize)
    {
        if (_maxHeaderTableSize != maxHeaderTableSize)
        {
            _maxHeaderTableSize = maxHeaderTableSize;

            // Dynamic table size update will be written next HEADERS frame
            _pendingTableSizeUpdate = true;

            // Check capacity and remove entries that exceed the new capacity
            EnsureCapacity(0);
        }
    }

    public bool EnsureDynamicTableSizeUpdate(Span<byte> buffer, out int length)
    {
        // Check if there is a table size update that should be encoded
        if (_pendingTableSizeUpdate)
        {
            bool success = HPackEncoder.EncodeDynamicTableSizeUpdate((int)_maxHeaderTableSize, buffer, out length);
            _pendingTableSizeUpdate = false;
            return success;
        }

        length = 0;
        return true;
    }

    public bool EncodeHeader(Span<byte> buffer, int staticTableIndex, HeaderEncodingHint encodingHint, string name, string value,
        Encoding? valueEncoding, out int bytesWritten)
    {
        Debug.Assert(!_pendingTableSizeUpdate, "Dynamic table size update should be encoded before headers.");

        // Never index sensitive value.
        if (encodingHint == HeaderEncodingHint.NeverIndex)
        {
            int index = ResolveDynamicTableIndex(staticTableIndex, name);

            return index == -1
                ? HPackEncoder.EncodeLiteralHeaderFieldNeverIndexingNewName(name, value, valueEncoding, buffer, out bytesWritten)
                : HPackEncoder.EncodeLiteralHeaderFieldNeverIndexing(index, value, valueEncoding, buffer, out bytesWritten);
        }

        // No dynamic table. Only use the static table.
        if (!_allowDynamicCompression || _maxHeaderTableSize == 0 || encodingHint == HeaderEncodingHint.IgnoreIndex)
        {
            return staticTableIndex == -1
                ? HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingNewName(name, value, valueEncoding, buffer, out bytesWritten)
                : HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexing(staticTableIndex, value, valueEncoding, buffer, out bytesWritten);
        }

        // Header is greater than the maximum table size.
        // Don't attempt to add dynamic header as all existing dynamic headers will be removed.
        var headerLength = HeaderField.GetLength(name.Length, valueEncoding?.GetByteCount(value) ?? value.Length);
        if (headerLength > _maxHeaderTableSize)
        {
            int index = ResolveDynamicTableIndex(staticTableIndex, name);

            return index == -1
                ? HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingNewName(name, value, valueEncoding, buffer, out bytesWritten)
                : HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexing(index, value, valueEncoding, buffer, out bytesWritten);
        }

        return EncodeDynamicHeader(buffer, staticTableIndex, name, value, headerLength, valueEncoding, out bytesWritten);
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

    private bool EncodeDynamicHeader(Span<byte> buffer, int staticTableIndex, string name, string value,
        int headerLength, Encoding? valueEncoding, out int bytesWritten)
    {
        EncoderHeaderEntry? headerField = GetEntry(name, value);
        if (headerField != null)
        {
            // Already exists in dynamic table. Write index.
            int index = CalculateDynamicTableIndex(headerField.Index);
            return HPackEncoder.EncodeIndexedHeaderField(index, buffer, out bytesWritten);
        }
        else
        {
            // Doesn't exist in dynamic table. Add new entry to dynamic table.

            int index = ResolveDynamicTableIndex(staticTableIndex, name);
            bool success = index == -1
                ? HPackEncoder.EncodeLiteralHeaderFieldIndexingNewName(name, value, valueEncoding, buffer, out bytesWritten)
                : HPackEncoder.EncodeLiteralHeaderFieldIndexing(index, value, valueEncoding, buffer, out bytesWritten);

            if (success)
            {
                uint headerSize = (uint)headerLength;
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
            EncoderHeaderEntry? removed = RemoveHeaderEntry();
            Debug.Assert(removed != null);

            // Removed entries are tracked to be reused.
            PushRemovedEntry(removed);
        }
    }

    private EncoderHeaderEntry? GetEntry(string name, string value)
    {
        if (_headerTableSize == 0)
        {
            return null;
        }
        int hash = name.GetHashCode();
        int bucketIndex = CalculateBucketIndex(hash);
        for (EncoderHeaderEntry? e = _headerBuckets[bucketIndex]; e != null; e = e.Next)
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
        int hash = name.GetHashCode();
        int bucketIndex = CalculateBucketIndex(hash);
        for (EncoderHeaderEntry? e = _headerBuckets[bucketIndex]; e != null; e = e.Next)
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
        return index == -1 ? -1 : index - Head.Before!.Index + 1 + H2StaticTable.Count;
    }

    private void AddHeaderEntry(string name, string value, uint headerSize)
    {
        Debug.Assert(headerSize <= _maxHeaderTableSize, "Header is bigger than dynamic table size.");
        Debug.Assert(headerSize <= _maxHeaderTableSize - _headerTableSize, "Not enough room in dynamic table.");

        int hash = name.GetHashCode();
        int bucketIndex = CalculateBucketIndex(hash);
        EncoderHeaderEntry? oldEntry = _headerBuckets[bucketIndex];
        // Attempt to reuse removed entry
        EncoderHeaderEntry? newEntry = PopRemovedEntry() ?? new EncoderHeaderEntry();
        newEntry.Initialize(hash, name, value, headerSize, Head.Before!.Index - 1, oldEntry);
        _headerBuckets[bucketIndex] = newEntry;
        newEntry.AddBefore(Head);
        _headerTableSize += headerSize;
    }

    private void PushRemovedEntry(EncoderHeaderEntry removed)
    {
        if (_removed != null)
        {
            removed.Next = _removed;
        }
        _removed = removed;
    }

    private EncoderHeaderEntry? PopRemovedEntry()
    {
        if (_removed != null)
        {
            EncoderHeaderEntry? removed = _removed;
            _removed = _removed.Next;
            return removed;
        }

        return null;
    }

    /// <summary>
    /// Remove the oldest entry.
    /// </summary>
    private EncoderHeaderEntry? RemoveHeaderEntry()
    {
        if (_headerTableSize == 0)
        {
            return null;
        }
        EncoderHeaderEntry? eldest = Head.After;
        int hash = eldest!.Hash;
        int bucketIndex = CalculateBucketIndex(hash);
        EncoderHeaderEntry? prev = _headerBuckets[bucketIndex];
        EncoderHeaderEntry? e = prev;
        while (e != null)
        {
            EncoderHeaderEntry? next = e.Next;
            if (e == eldest)
            {
                if (prev == eldest)
                {
                    _headerBuckets[bucketIndex] = next!;
                }
                else
                {
                    prev.Next = next;
                }
                _headerTableSize -= eldest.Size;
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

/// <summary>
/// Hint for how the header should be encoded as HPack. This value can be overriden.
/// For example, a header that is larger than the dynamic table won't be indexed.
/// </summary>
internal enum HeaderEncodingHint
{
    Index,
    IgnoreIndex,
    NeverIndex
}
