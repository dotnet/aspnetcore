// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Http
{
    public interface ISessionCollection : IEnumerable<KeyValuePair<string, byte[]>>
    {
        byte[] this[string key] { get; set; }

        bool TryGetValue(string key, out byte[] value);

        void Set(string key, ArraySegment<byte> value);

        void Remove(string key);

        void Clear();
    }
}