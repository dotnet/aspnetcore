// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal class UserProxy<THub> : IClientProxy where THub : Hub
    {
        private readonly string _userId;
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public UserProxy(HubLifetimeManager<THub> lifetimeManager, string userId)
        {
            _lifetimeManager = lifetimeManager;
            _userId = userId;
        }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            return _lifetimeManager.SendUserAsync(_userId, method, args, cancellationToken);
        }
    }

    internal class MultipleUserProxy<THub> : IClientProxy where THub : Hub
    {
        private readonly IReadOnlyList<string> _userIds;
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public MultipleUserProxy(HubLifetimeManager<THub> lifetimeManager, IReadOnlyList<string> userIds)
        {
            _lifetimeManager = lifetimeManager;
            _userIds = userIds;
        }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            return _lifetimeManager.SendUsersAsync(_userIds, method, args, cancellationToken);
        }
    }

    internal class GroupProxy<THub> : IClientProxy where THub : Hub
    {
        private readonly string _groupName;
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public GroupProxy(HubLifetimeManager<THub> lifetimeManager, string groupName)
        {
            _lifetimeManager = lifetimeManager;
            _groupName = groupName;
        }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            return _lifetimeManager.SendGroupAsync(_groupName, method, args, cancellationToken);
        }
    }

    internal class MultipleGroupProxy<THub> : IClientProxy where THub : Hub
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private readonly IReadOnlyList<string> _groupNames;

        public MultipleGroupProxy(HubLifetimeManager<THub> lifetimeManager, IReadOnlyList<string> groupNames)
        {
            _lifetimeManager = lifetimeManager;
            _groupNames = groupNames;
        }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            return _lifetimeManager.SendGroupsAsync(_groupNames, method, args, cancellationToken);
        }
    }

    internal class GroupExceptProxy<THub> : IClientProxy where THub : Hub
    {
        private readonly string _groupName;
        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private readonly IReadOnlyList<string> _excludedConnectionIds;

        public GroupExceptProxy(HubLifetimeManager<THub> lifetimeManager, string groupName, IReadOnlyList<string> excludedConnectionIds)
        {
            _lifetimeManager = lifetimeManager;
            _groupName = groupName;
            _excludedConnectionIds = excludedConnectionIds;
        }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            return _lifetimeManager.SendGroupExceptAsync(_groupName, method, args, _excludedConnectionIds, cancellationToken);
        }
    }

    internal class AllClientProxy<THub> : IClientProxy where THub : Hub
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public AllClientProxy(HubLifetimeManager<THub> lifetimeManager)
        {
            _lifetimeManager = lifetimeManager;
        }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            return _lifetimeManager.SendAllAsync(method, args, cancellationToken);
        }
    }

    internal class AllClientsExceptProxy<THub> : IClientProxy where THub : Hub
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private readonly IReadOnlyList<string> _excludedConnectionIds;

        public AllClientsExceptProxy(HubLifetimeManager<THub> lifetimeManager, IReadOnlyList<string> excludedConnectionIds)
        {
            _lifetimeManager = lifetimeManager;
            _excludedConnectionIds = excludedConnectionIds;
        }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            return _lifetimeManager.SendAllExceptAsync(method, args, _excludedConnectionIds, cancellationToken);
        }
    }

    internal class SingleClientProxy<THub> : IClientProxy where THub : Hub
    {
        private readonly string _connectionId;
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public SingleClientProxy(HubLifetimeManager<THub> lifetimeManager, string connectionId)
        {
            _lifetimeManager = lifetimeManager;
            _connectionId = connectionId;
        }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            return _lifetimeManager.SendConnectionAsync(_connectionId, method, args, cancellationToken);
        }
    }

    internal class MultipleClientProxy<THub> : IClientProxy where THub : Hub
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private readonly IReadOnlyList<string> _connectionIds;

        public MultipleClientProxy(HubLifetimeManager<THub> lifetimeManager, IReadOnlyList<string> connectionIds)
        {
            _lifetimeManager = lifetimeManager;
            _connectionIds = connectionIds;
        }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            return _lifetimeManager.SendConnectionsAsync(_connectionIds, method, args, cancellationToken);
        }
    }
}
