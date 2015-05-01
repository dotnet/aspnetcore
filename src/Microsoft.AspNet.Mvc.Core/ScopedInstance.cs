// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc
{
    public class ScopedInstance<T> : IScopedInstance<T>
    {
        public T Value { get; set; }

        public void Dispose()
        {
            var disposable = Value as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}