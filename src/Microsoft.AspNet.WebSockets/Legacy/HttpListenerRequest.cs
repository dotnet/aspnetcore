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