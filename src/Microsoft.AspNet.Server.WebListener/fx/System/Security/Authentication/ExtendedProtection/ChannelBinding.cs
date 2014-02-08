//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//
//------------------------------------------------------------------------------

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

#if !NET45

namespace System.Security.Authentication.ExtendedProtection
{
    internal abstract class ChannelBinding : SafeHandleZeroOrMinusOneIsInvalid
    {
        protected ChannelBinding()
            : base(true)
        {
        }

        protected ChannelBinding(bool ownsHandle)
            : base(ownsHandle)
        {
        }

        public abstract int Size
        {
            get;
        }
    }
}

#endif