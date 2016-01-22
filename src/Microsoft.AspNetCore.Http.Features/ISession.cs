// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Http.Features
{
    public interface ISession
    {
        Task LoadAsync();

        Task CommitAsync();

        bool TryGetValue(string key, out byte[] value);

        void Set(string key, byte[] value);

        void Remove(string key);

        void Clear();

        IEnumerable<string> Keys { get; }
    }
}