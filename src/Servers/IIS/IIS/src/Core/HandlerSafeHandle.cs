// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Sources;

namespace Microsoft.AspNetCore.Server.IIS.Core
{
    internal class HandlerSafeHandle : SafeHandle, IValueTaskSource
    {
        public override bool IsInvalid => handle == IntPtr.Zero;

        public HandlerSafeHandle(IntPtr handle) : base(IntPtr.Zero, ownsHandle: true)
        {
            this.handle = handle;
        }

        protected override bool ReleaseHandle()
        {
            handle = IntPtr.Zero;
            return true;
        }
    }
}
