using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketsSample.Hubs
{
    public class Hub
    {
        public IHubConnectionContext Clients { get; set; }
    }

    public interface IHubConnectionContext
    {
        IClientProxy All { get; }

        IClientProxy Client(string connectionId);
    }

    public class HubConnectionContext : IHubConnectionContext
    {
        private readonly HubEndpoint _endPoint;

        public HubConnectionContext(string hubName, HubEndpoint endpoint)
        {
            _endPoint = endpoint;
            All = new HubClientProxy(endpoint, hubName);
        }

        public IClientProxy All { get; }

        public IClientProxy Client(string connectionId)
        {
            return new HubClientProxy(_endPoint, connectionId);
        }
    }

    public class HubClientProxy : IClientProxy
    {
        private readonly HubEndpoint _endPoint;
        private readonly string _key;

        public HubClientProxy(HubEndpoint endPoint, string key)
        {
            _endPoint = endPoint;
            _key = key;
        }

        public Task Invoke(string method, params object[] args)
        {
            return _endPoint.Invoke(_key, method, args);
        }
    }

    public interface IClientProxy
    {
        /// <summary>
        /// Invokes a method on the connection(s) represented by the <see cref="IClientProxy"/> instance.
        /// </summary>
        /// <param name="method">name of the method to invoke</param>
        /// <param name="args">argumetns to pass to the client</param>
        /// <returns>A task that represents when the data has been sent to the client.</returns>
        Task Invoke(string method, params object[] args);
    }
}
