// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Net.Http.Server
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SSPIHandle
    {
        private IntPtr handleHi;
        private IntPtr handleLo;

        public bool IsZero
        {
            get { return handleHi == IntPtr.Zero && handleLo == IntPtr.Zero; }
        }

        internal void SetToInvalid()
        {
            handleHi = IntPtr.Zero;
            handleLo = IntPtr.Zero;
        }

        public override string ToString()
        {
            return handleHi.ToString("x") + ":" + handleLo.ToString("x");
        }
    }
}
