//------------------------------------------------------------------------------
// <copyright file="TraceEventType.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

#if !NET45

using System;
using System.ComponentModel;

namespace System.Diagnostics
{
    internal enum TraceEventType
    {
        Critical = 0x01,
        Error = 0x02,
        Warning = 0x04,
        Information = 0x08,
        Verbose = 0x10,

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        Start = 0x0100,
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        Stop = 0x0200,
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        Suspend = 0x0400,
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        Resume = 0x0800,
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        Transfer = 0x1000,
    }
}

#endif