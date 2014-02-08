// -----------------------------------------------------------------------
// <copyright file="SSPIHandle.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Security.Windows
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SSPIHandle
    {
        private IntPtr HandleHi;
        private IntPtr HandleLo;

        public bool IsZero
        {
            get { return HandleHi == IntPtr.Zero && HandleLo == IntPtr.Zero; }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal void SetToInvalid()
        {
            HandleHi = IntPtr.Zero;
            HandleLo = IntPtr.Zero;
        }

        public override string ToString()
        {
            return HandleHi.ToString("x") + ":" + HandleLo.ToString("x");
        }
    }
}
