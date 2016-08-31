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
// <copyright file="_ListenerAsyncResult.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Net.Http.Server
{
    // Note this type should only be used while the request buffer remains pinned
    internal class CookedUrl
    {
        private readonly HttpApi.HTTP_COOKED_URL _nativeCookedUrl;

        internal CookedUrl(HttpApi.HTTP_COOKED_URL nativeCookedUrl)
        {
            _nativeCookedUrl = nativeCookedUrl;
        }

        internal unsafe string GetFullUrl()
        {
            if (_nativeCookedUrl.pFullUrl != null && _nativeCookedUrl.FullUrlLength > 0)
            {
                return Marshal.PtrToStringUni((IntPtr)_nativeCookedUrl.pFullUrl, _nativeCookedUrl.FullUrlLength / 2);
            }
            return null;
        }

        internal unsafe string GetHost()
        {
            if (_nativeCookedUrl.pHost != null && _nativeCookedUrl.HostLength > 0)
            {
                return Marshal.PtrToStringUni((IntPtr)_nativeCookedUrl.pHost, _nativeCookedUrl.HostLength / 2);
            }
            return null;
        }

        internal unsafe string GetAbsPath()
        {
            if (_nativeCookedUrl.pAbsPath != null && _nativeCookedUrl.AbsPathLength > 0)
            {
                return Marshal.PtrToStringUni((IntPtr)_nativeCookedUrl.pAbsPath, _nativeCookedUrl.AbsPathLength / 2);
            }
            return null;
        }

        internal unsafe string GetQueryString()
        {
            if (_nativeCookedUrl.pQueryString != null && _nativeCookedUrl.QueryStringLength > 0)
            {
                return Marshal.PtrToStringUni((IntPtr)_nativeCookedUrl.pQueryString, _nativeCookedUrl.QueryStringLength / 2);
            }
            return null;
        }
    }
}
