// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Tests;

public static class HubProtocolHelpers
{
    private static readonly IHubProtocol NewtonsoftJsonHubProtocol = new NewtonsoftJsonHubProtocol();

    private static readonly IHubProtocol MessagePackHubProtocol = new MessagePackHubProtocol();

    public static readonly List<string> AllProtocolNames = new List<string>
        {
            NewtonsoftJsonHubProtocol.Name,
            MessagePackHubProtocol.Name
        };

    public static readonly IList<IHubProtocol> AllProtocols = new List<IHubProtocol>()
        {
            NewtonsoftJsonHubProtocol,
            MessagePackHubProtocol
        };

    public static IHubProtocol GetHubProtocol(string name)
    {
        var protocol = AllProtocols.SingleOrDefault(p => p.Name == name);
        if (protocol == null)
        {
            throw new InvalidOperationException($"Could not find protocol with name '{name}'.");
        }

        return protocol;
    }
}
