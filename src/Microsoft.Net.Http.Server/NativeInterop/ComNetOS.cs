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

// -----------------------------------------------------------------------
// <copyright file="ComNetOS.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.Net.Http.Server
{
    internal static class ComNetOS
    {
        // Minimum support for Windows 7 is assumed.
        internal static readonly bool IsWin8orLater;

        static ComNetOS()
        {
            var win8Version = new Version(6, 2);

#if NETSTANDARD1_3
            IsWin8orLater = (new Version(RuntimeEnvironment.OperatingSystemVersion) >= win8Version);
#else
            IsWin8orLater = (Environment.OSVersion.Version >= win8Version);
#endif
        }
    }
}
