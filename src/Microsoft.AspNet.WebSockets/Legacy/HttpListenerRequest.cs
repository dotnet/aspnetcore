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
// <copyright file="HttpListenerRequest.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

/*
namespace Microsoft.Net
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Globalization;
    using System.Text;
    using System.Security.Principal;
    using System.Security.Cryptography.X509Certificates;
    using System.Net;
    using Microsoft.AspNet.WebSockets;

    public sealed unsafe class HttpListenerRequest {

        public bool IsWebSocketRequest 
        {
            get
            {
                if (!WebSocketProtocolComponent.IsSupported)
                {
                    return false;
                }

                bool foundConnectionUpgradeHeader = false;
                if (string.IsNullOrEmpty(this.Headers[HttpKnownHeaderNames.Connection]) || string.IsNullOrEmpty(this.Headers[HttpKnownHeaderNames.Upgrade]))
                {
                    return false; 
                }

                foreach (string connection in this.Headers.GetValues(HttpKnownHeaderNames.Connection)) 
                {
                    if (string.Compare(connection, HttpKnownHeaderNames.Upgrade, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        foundConnectionUpgradeHeader = true;
                        break;
                    }
                }

                if (!foundConnectionUpgradeHeader)
                {
                    return false; 
                }

                foreach (string upgrade in this.Headers.GetValues(HttpKnownHeaderNames.Upgrade))
                {
                    if (string.Compare(upgrade, WebSocketHelpers.WebSocketUpgradeToken, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return true;
                    }
                }

                return false; 
            }
        }
    }
}
*/