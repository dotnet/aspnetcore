// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Options used to configure the specified hub type instances. These options override globally set options.
/// </summary>
/// <typeparam name="THub">The hub type to configure.</typeparam>
public class HubOptions<THub> : HubOptions where THub : Hub
{
    internal bool UserHasSetValues { get; set; }
}
