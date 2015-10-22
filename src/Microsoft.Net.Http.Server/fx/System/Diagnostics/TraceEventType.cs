// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

//------------------------------------------------------------------------------
// <copyright file="TraceEventType.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

#if DOTNET5_4

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
