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
// <copyright file="WebSocketReceiveResult.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.Contracts;
using System.Net.WebSockets;

namespace Microsoft.Net.WebSockets
{
    public static class WebSocketReceiveResultExtensions
    {
        internal static WebSocketReceiveResult DecrementAndClone(ref WebSocketReceiveResult original, int count)
        {
            Contract.Assert(count >= 0, "'count' MUST NOT be negative.");
            Contract.Assert(count <= original.Count, "'count' MUST NOT be bigger than 'this.Count'.");
            int remaining = original.Count - count;
            original = new WebSocketReceiveResult(remaining,
                original.MessageType,
                original.EndOfMessage,
                original.CloseStatus,
                original.CloseStatusDescription);
            return new WebSocketReceiveResult(count,
                original.MessageType,
                remaining == 0 && original.EndOfMessage,
                original.CloseStatus,
                original.CloseStatusDescription);
        }
    }
}