// -----------------------------------------------------------------------
// <copyright file="SSPIHandle.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Net.Server
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
