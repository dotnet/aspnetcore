// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Server.Kestrel.Tests
{
    public class DisposableStack<T> : Stack<T>, IDisposable
        where T : IDisposable
    {
        public void Dispose()
        {
            while (Count > 0)
            {
                Pop()?.Dispose();
            }
        }
    }
}
