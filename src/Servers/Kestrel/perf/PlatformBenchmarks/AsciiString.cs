// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace PlatformBenchmarks
{
    public readonly struct AsciiString : IEquatable<AsciiString>
    {
        private readonly byte[] _data;

        public AsciiString(string s) => _data = Encoding.ASCII.GetBytes(s);

        public int Length => _data.Length;

        public ReadOnlySpan<byte> AsSpan() => _data;

        public static implicit operator ReadOnlySpan<byte>(AsciiString str) => str._data;
        public static implicit operator byte[] (AsciiString str) => str._data;

        public static implicit operator AsciiString(string str) => new AsciiString(str);

        public override string ToString() => HttpUtilities.GetAsciiStringNonNullCharacters(_data);
        public static explicit operator string(AsciiString str) => str.ToString();

        public bool Equals(AsciiString other) => ReferenceEquals(_data, other._data) || SequenceEqual(_data, other._data);
        private bool SequenceEqual(byte[] data1, byte[] data2) => new Span<byte>(data1).SequenceEqual(data2);

        public static bool operator ==(AsciiString a, AsciiString b) => a.Equals(b);
        public static bool operator !=(AsciiString a, AsciiString b) => !a.Equals(b);
        public override bool Equals(object other) => (other is AsciiString) && Equals((AsciiString)other);

        public override int GetHashCode()
        {
            // Copied from x64 version of string.GetLegacyNonRandomizedHashCode()
            // https://github.com/dotnet/coreclr/blob/master/src/mscorlib/src/System/String.Comparison.cs
            var data = _data;
            int hash1 = 5381;
            int hash2 = hash1;
            foreach (int b in data)
            {
                hash1 = ((hash1 << 5) + hash1) ^ b;
            }
            return hash1 + (hash2 * 1566083941);
        }

    }
}
