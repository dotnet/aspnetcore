// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace System.Net.Http.HPack
{
    internal readonly struct HeaderField
    {
        // http://httpwg.org/specs/rfc7541.html#rfc.section.4.1
        public const int RfcOverhead = 32;

        public HeaderField(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            Debug.Assert(name.Length > 0);

            // TODO: We're allocating here on every new table entry.
            // That means a poorly-behaved server could cause us to allocate repeatedly.
            // We should revisit our allocation strategy here so we don't need to allocate per entry
            // and we have a cap to how much allocation can happen per dynamic table
            // (without limiting the number of table entries a server can provide within the table size limit).
            Name = name.ToArray();
            Value = value.ToArray();
        }

        public byte[] Name { get; }

        public byte[] Value { get; }

        public int Length => GetLength(Name.Length, Value.Length);

        public static int GetLength(int nameLength, int valueLength) => nameLength + valueLength + RfcOverhead;

        public override string ToString()
        {
            if (Name != null)
            {
                return Encoding.Latin1.GetString(Name) + ": " + Encoding.Latin1.GetString(Value);
            }
            else
            {
                return "<empty>";
            }
        }
    }
}
