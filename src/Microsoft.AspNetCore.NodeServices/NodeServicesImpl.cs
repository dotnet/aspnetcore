using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices.HostingModels;

namespace Microsoft.AspNetCore.NodeServices
{
    /// <summary>
    /// Default implementation of INodeServices. This is the primary API surface through which developers
    /// make use of this package. It provides simple "InvokeAsync" methods that dispatch calls to the
    /// correct Node instance, creating and destroying those instances as needed.
    ///
    /// If a Node instance dies (or none was yet created), this class takes care of creating a new one.
    /// If a Node instance signals that it needs to be restarted (e.g., because a file changed), then this
    /// class will create a new instance and dispatch future calls to it, while keeping the old instance
    /// alive for a defined period so that any in-flight RPC calls can complete. This latter feature is
    /// analogous to the "connection draining" feature implemented by HTTP load balancers.
    ///
    /// TODO: Implement everything in the preceding paragraph.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.NodeServices.INodeServices" />
    internal class NodeServicesImpl : INodeServices
    {
        private NodeServicesOptions _options;
        private Func<INodeInstance> _nodeInstanceFactory;
        private INodeInstance _currentNodeInstance;
        private object _currentNodeInstanceAccessLock = new object();

        internal NodeServicesImpl(NodeServicesOptions options, Func<INodeInstance> nodeInstanceFactory)
        {
            _options = options;
            _nodeInstanceFactory = nodeInstanceFactory;
        }

        public Task<T> InvokeAsync<T>(string moduleName, params object[] args)
        {
            return InvokeExportAsync<T>(moduleName, null, args);
        }

        public Task<T> InvokeExportAsync<T>(string moduleName, string exportedFunctionName, params object[] args)
        {
            var nodeInstance = GetOrCreateCurrentNodeInstance();
            return nodeInstance.InvokeExportAsync<T>(moduleName, exportedFunctionName, args);
        }

        public void Dispose()
        {
            lock (_currentNodeInstanceAccessLock)
            {
                if (_currentNodeInstance != null)
                {
                    _currentNodeInstance.Dispose();
                    _currentNodeInstance = null;
                }
            }
        }

        private INodeInstance GetOrCreateCurrentNodeInstance()
        {
            var instance = _currentNodeInstance;
            if (instance == null)
            {
                lock (_currentNodeInstanceAccessLock)
                {
                    instance = _currentNodeInstance;
                    if (instance == null)
                    {
                        instance = _currentNodeInstance = CreateNewNodeInstance();
                    }
                }
            }

            return instance;
        }

        private INodeInstance CreateNewNodeInstance()
        {
            return _nodeInstanceFactory();
        }

        // Obsolete method - will be removed soon
        public Task<T> Invoke<T>(string moduleName, params object[] args)
        {
            return InvokeAsync<T>(moduleName, args);
        }

        // Obsolete method - will be removed soon
        public Task<T> InvokeExport<T>(string moduleName, string exportedFunctionName, params object[] args)
        {
            return InvokeExportAsync<T>(moduleName, exportedFunctionName, args);
        }
    }
}