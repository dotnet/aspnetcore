// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0.
// See THIRD-PARTY-NOTICES.TXT in the project root for license information.

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
            Name = new byte[name.Length];
            name.CopyTo(Name);

            Value = new byte[value.Length];
            value.CopyTo(Value);
        }

        public byte[] Name { get; }

        public byte[] Value { get; }

        public int Length => GetLength(Name.Length, Value.Length);

        public static int GetLength(int nameLength, int valueLength) => nameLength + valueLength + RfcOverhead;

        public override string ToString()
        {
            if (Name != null)
            {
                return Encoding.ASCII.GetString(Name) + ": " + Encoding.ASCII.GetString(Value);
            }
            else
            {
                return "<empty>";
            }
        }
    }
}
