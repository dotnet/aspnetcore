// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Server.Kestrel.Networking
{
    public abstract class UvReq : UvMemory
    {
        protected override bool ReleaseHandle()
        {
            DestroyMemory(handle);
            handle = IntPtr.Zero;
            return true;
        }
    }
}