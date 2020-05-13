// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http.QPack
{
    internal readonly struct HeaderField
    {
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
