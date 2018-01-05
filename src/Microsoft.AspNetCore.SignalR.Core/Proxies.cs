// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public class UserProxy<THub> : IClientProxy
    {
        private readonly string _userId;
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public UserProxy(HubLifetimeManager<THub> lifetimeManager, string userId)
        {
            _lifetimeManager = lifetimeManager;
            _userId = userId;
        }

        public Task InvokeAsync(string method, params object[] args)
        {
            return _lifetimeManager.InvokeUserAsync(_userId, method, args);
        }
    }

    public class GroupProxy<THub> : IClientProxy
    {
        private readonly string _groupName;
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public GroupProxy(HubLifetimeManager<THub> lifetimeManager, string groupName)
        {
            _lifetimeManager = lifetimeManager;
            _groupName = groupName;
        }

        public Task InvokeAsync(string method, params object[] args)
        {
            return _lifetimeManager.InvokeGroupAsync(_groupName, method, args);
        }
    }

    public class MultipleGroupProxy<THub> : IClientProxy
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private IReadOnlyList<string> _groupNames;

        public MultipleGroupProxy(HubLifetimeManager<THub> lifetimeManager, IReadOnlyList<string> groupNames)
        {
            _lifetimeManager = lifetimeManager;
            _groupNames = groupNames;
        }

        public Task InvokeAsync(string method, params object[] args)
        {
            return _lifetimeManager.InvokeGroupsAsync(_groupNames, method, args);
        }
    }

    public class GroupExceptProxy<THub> : IClientProxy
    {
        private readonly string _groupName;
        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private readonly IReadOnlyList<string> _excludedIds;

        public GroupExceptProxy(HubLifetimeManager<THub> lifetimeManager, string groupName, IReadOnlyList<string> excludedIds)
        {
            _lifetimeManager = lifetimeManager;
            _groupName = groupName;
            _excludedIds = excludedIds;
        }

        public Task InvokeAsync(string method, params object[] args)
        {
            return _lifetimeManager.InvokeGroupExceptAsync(_groupName, method, args, _excludedIds);
        }
    }

    public class AllClientProxy<THub> : IClientProxy
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public AllClientProxy(HubLifetimeManager<THub> lifetimeManager)
        {
            _lifetimeManager = lifetimeManager;
        }

        public Task InvokeAsync(string method, params object[] args)
        {
            return _lifetimeManager.InvokeAllAsync(method, args);
        }
    }

    public class AllClientsExceptProxy<THub> : IClientProxy
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private IReadOnlyList<string> _excludedIds;

        public AllClientsExceptProxy(HubLifetimeManager<THub> lifetimeManager, IReadOnlyList<string> excludedIds)
        {
            _lifetimeManager = lifetimeManager;
            _excludedIds = excludedIds;
        }

        public Task InvokeAsync(string method, params object[] args)
        {
            return _lifetimeManager.InvokeAllExceptAsync(method, args, _excludedIds);
        }
    }

    public class SingleClientProxy<THub> : IClientProxy
    {
        private readonly string _connectionId;
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public SingleClientProxy(HubLifetimeManager<THub> lifetimeManager, string connectionId)
        {
            _lifetimeManager = lifetimeManager;
            _connectionId = connectionId;
        }

        public Task InvokeAsync(string method, params object[] args)
        {
            return _lifetimeManager.InvokeConnectionAsync(_connectionId, method, args);
        }
    }

    public class MultipleClientProxy<THub> : IClientProxy
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private IReadOnlyList<string> _connectionIds;

        public MultipleClientProxy(HubLifetimeManager<THub> lifetimeManager, IReadOnlyList<string> connectionIds)
        {
            _lifetimeManager = lifetimeManager;
            _connectionIds = connectionIds;
        }

        public Task InvokeAsync(string method, params object[] args)
        {
            return _lifetimeManager.InvokeConnectionsAsync(_connectionIds, method, args);
        }
    }

    public class GroupManager<THub> : IGroupManager
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public GroupManager(HubLifetimeManager<THub> lifetimeManager)
        {
            _lifetimeManager = lifetimeManager;
        }

        public Task AddAsync(string connectionId, string groupName)
        {
            return _lifetimeManager.AddGroupAsync(connectionId, groupName);
        }

        public Task RemoveAsync(string connectionId, string groupName)
        {
            return _lifetimeManager.RemoveGroupAsync(connectionId, groupName);
        }
    }
}
