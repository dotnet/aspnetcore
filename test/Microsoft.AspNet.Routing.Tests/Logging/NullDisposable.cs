// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Routing
{
    public class NullDisposable : IDisposable
    {
        public static NullDisposable Instance = new NullDisposable();

        public void Dispose()
        {
            // intentionally does nothing
        }
    }
}