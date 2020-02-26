// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
