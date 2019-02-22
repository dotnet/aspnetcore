// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    public class HubCallerClients : IHubCallerClients
    {
        private readonly string _connectionId;
        private readonly IHubClients _hubClients;
        private readonly string[] _currentConnectionId;

        public HubCallerClients(IHubClients hubClients, string connectionId)
        {
            _connectionId = connectionId;
            _hubClients = hubClients;
            _currentConnectionId = new[] { _connectionId };
        }

        public ClientProxy Caller => _hubClients.Client(_connectionId);

        public ClientProxy Others => _hubClients.AllExcept(_currentConnectionId);

        public ClientProxy All => _hubClients.All;

        public ClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
        {
            return _hubClients.AllExcept(excludedConnectionIds);
        }

        public ClientProxy Client(string connectionId)
        {
            return _hubClients.Client(connectionId);
        }

        public ClientProxy Group(string groupName)
        {
            return _hubClients.Group(groupName);
        }

        public ClientProxy Groups(IReadOnlyList<string> groupNames)
        {
            return _hubClients.Groups(groupNames);
        }

        public ClientProxy OthersInGroup(string groupName)
        {
            return _hubClients.GroupExcept(groupName, _currentConnectionId);
        }

        public ClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
        {
            return _hubClients.GroupExcept(groupName, excludedConnectionIds);
        }

        public ClientProxy User(string userId)
        {
           return _hubClients.User(userId);
        }

        public ClientProxy Clients(IReadOnlyList<string> connectionIds)
        {
            return _hubClients.Clients(connectionIds);
        }

        public ClientProxy Users(IReadOnlyList<string> userIds)
        {
            return _hubClients.Users(userIds);
        }
    }
}
