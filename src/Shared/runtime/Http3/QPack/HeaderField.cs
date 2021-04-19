// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http.QPack
{
    internal readonly struct HeaderField
    {
        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1.1.3-1
        // public for internal use in aspnetcore
        public const int RfcOverhead = 32;

        public HeaderField(byte[] name, byte[] value)
        {
            Name = name;
            Value = value;
        }

        public byte[] Name { get; }

        public byte[] Value { get; }

        public int Length => Name.Length + Value.Length;
    }
}
