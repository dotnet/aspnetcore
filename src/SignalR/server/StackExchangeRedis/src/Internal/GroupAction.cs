// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal;

// The size of the enum is defined by the protocol. Do not change it. If you need more than 255 items,
// add an additional enum.
internal enum GroupAction : byte
{
    // These numbers are used by the protocol, do not change them and always use explicit assignment
    // when adding new items to this enum. 0 is intentionally omitted
    Add = 1,
    Remove = 2,
}
