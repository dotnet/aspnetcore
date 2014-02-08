//------------------------------------------------------------------------------
// <copyright file="EntitySendFormat.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.AspNet.Server.WebListener
{
    internal enum EntitySendFormat
    {
        ContentLength = 0, // Content-Length: XXX
        Chunked = 1, // Transfer-Encoding: chunked
        /*
        Raw = 2, // the app is responsible for sending the correct headers and body encoding
        */
    }
}
