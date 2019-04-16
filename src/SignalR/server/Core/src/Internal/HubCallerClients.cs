// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    public class HubCallerClients : HubCallerClientsBase
    {
        private readonly string _connectionId;
        private readonly HubClientsBase _hubClients;
        private readonly string[] _currentConnectionId;

        public HubCallerClients(HubClientsBase hubClients, string connectionId)
        {
            _connectionId = connectionId;
            _hubClients = hubClients;
            _currentConnectionId = new[] { _connectionId };
        }

        public override IClientProxy Caller => _hubClients.Client(_connectionId);

        public override IClientProxy Others => _hubClients.AllExcept(_currentConnectionId);

        public override IClientProxy All => _hubClients.All;

        public override IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
        {
            return _hubClients.AllExcept(excludedConnectionIds);
        }

        public override IClientProxy Client(string connectionId)
        {
            return _hubClients.Client(connectionId);
        }

        public override IClientProxy Group(string groupName)
        {
            return _hubClients.Group(groupName);
        }

        public override IClientProxy Groups(IReadOnlyList<string> groupNames)
        {
            return _hubClients.Groups(groupNames);
        }

        public override IClientProxy OthersInGroup(string groupName)
        {
            return _hubClients.GroupExcept(groupName, _currentConnectionId);
        }

        public override IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
        {
            return _hubClients.GroupExcept(groupName, excludedConnectionIds);
        }

        public override IClientProxy User(string userId)
        {
           return _hubClients.User(userId);
        }

        public override IClientProxy Clients(IReadOnlyList<string> connectionIds)
        {
            return _hubClients.Clients(connectionIds);
        }

        public override IClientProxy Users(IReadOnlyList<string> userIds)
        {
            return _hubClients.Users(userIds);
        }
    }
}
