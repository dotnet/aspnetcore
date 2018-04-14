// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    public class CloseMessage : HubMessage
    {
        public static readonly CloseMessage Empty = new CloseMessage(null);

        public string Error { get; }

        public CloseMessage(string error)
        {
            Error = error;
        }
    }
}
