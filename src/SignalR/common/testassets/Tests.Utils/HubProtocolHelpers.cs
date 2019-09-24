// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Tests
{
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
}
