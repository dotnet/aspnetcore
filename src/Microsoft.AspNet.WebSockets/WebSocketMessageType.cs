//------------------------------------------------------------------------------
// <copyright file="WebSocketMessageType.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.AspNet.WebSockets
{
    public enum WebSocketMessageType
    {
        Text = 0x1,
        Binary = 0x2,
        Close = 0x8,
    }
}