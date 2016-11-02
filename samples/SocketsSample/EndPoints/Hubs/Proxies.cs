using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using SocketsSample.Hubs;

namespace SocketsSample.EndPoints.Hubs
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

        public Task Invoke(string method, params object[] args)
        {
            return _lifetimeManager.InvokeUser(_userId, method, args);
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

        public Task Invoke(string method, params object[] args)
        {
            return _lifetimeManager.InvokeGroup(_groupName, method, args);
        }
    }

    public class AllClientProxy<THub> : IClientProxy
    {
        private readonly HubLifetimeManager<THub> _lifetimeManager;

        public AllClientProxy(HubLifetimeManager<THub> lifetimeManager)
        {
            _lifetimeManager = lifetimeManager;
        }

        public Task Invoke(string method, params object[] args)
        {
            // TODO: More than just chat
            return _lifetimeManager.InvokeAll(method, args);
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

        public Task Invoke(string method, params object[] args)
        {
            return _lifetimeManager.InvokeConnection(_connectionId, method, args);
        }
    }

    public class GroupManager<THub> : IGroupManager
    {
        private readonly Connection _connection;
        private HubLifetimeManager<THub> _lifetimeManager;

        public GroupManager(Connection connection, HubLifetimeManager<THub> lifetimeManager)
        {
            _connection = connection;
            _lifetimeManager = lifetimeManager;
        }

        public void Add(string groupName)
        {
            _lifetimeManager.AddGroup(_connection, groupName);
        }

        public void Remove(string groupName)
        {
            _lifetimeManager.RemoveGroup(_connection, groupName);
        }
    }
}
