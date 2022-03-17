// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace System.Net.Http.QPack
{
    internal readonly struct HeaderField
    {
        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1.1.3-1
        // public for internal use in aspnetcore
        public const int RfcOverhead = 32;

        public HeaderField(byte[] name, byte[] value)
        {
            Debug.Assert(name.Length > 0);

            Name = name;
            Value = value;
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
