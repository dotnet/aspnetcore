// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal
{
    // The size of the enum is defined by the protocol. Do not change it. If you need more than 255 items,
    // add an additional enum.
    public enum GroupAction : byte
    {
        // These numbers are used by the protocol, do not change them and always use explicit assignment
        // when adding new items to this enum. 0 is intentionally omitted
        Add = 1,
        Remove = 2,
    }
}
