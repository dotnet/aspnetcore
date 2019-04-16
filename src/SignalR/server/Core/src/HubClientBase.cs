// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    public abstract class HubClientsBase : HubClientsBase<IClientProxy>
    {
        public override abstract IClientProxy All { get; }

        public override abstract IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds);

        public override abstract IClientProxy Client(string connectionId);

        public override abstract IClientProxy Clients(IReadOnlyList<string> connectionIds);

        public override abstract IClientProxy Group(string groupName);

        public override abstract IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds);

        public override abstract IClientProxy Groups(IReadOnlyList<string> groupNames);

        public override abstract IClientProxy User(string userId);

        public override abstract IClientProxy Users(IReadOnlyList<string> userIds);
    }
}
