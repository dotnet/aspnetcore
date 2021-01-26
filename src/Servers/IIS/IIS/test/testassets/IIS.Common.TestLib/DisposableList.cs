// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public class DisposableList<T> : List<T>, IDisposable where T : IDisposable
    {
        public DisposableList() : base() { }

        public DisposableList(IEnumerable<T> collection) : base(collection) { }

        public DisposableList(int capacity) : base(capacity) { }

        public void Dispose()
        {
            foreach (var item in this)
            {
                item?.Dispose();
            }
        }
    }
}
