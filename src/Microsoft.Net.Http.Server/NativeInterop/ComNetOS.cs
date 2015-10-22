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

namespace Microsoft.Net.Http.Server
{
    internal static class ComNetOS
    {
        // Minimum support for Windows 7 is assumed.
        internal static readonly bool IsWin8orLater;

        static ComNetOS()
        {
#if DOTNET5_4
            // TODO: SkipIOCPCallbackOnSuccess doesn't work on Win7. Need a way to detect Win7 vs 8+.
            IsWin8orLater = false;
#else
            var win8Version = new Version(6, 2);
            IsWin8orLater = (Environment.OSVersion.Version >= win8Version);
#endif
        }
    }
}
