using System.Threading.Tasks;

namespace Microsoft.AspNet.NodeServices {
    public abstract class NodeHost : System.IDisposable
    {
        public abstract Task<string> Invoke(NodeInvocationInfo invocationInfo);
        
        public abstract void Dispose();
    }
}
