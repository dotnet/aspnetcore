using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.HttpFeature
{
    [AssemblyNeutral]
    public interface IHttpBuffering
    {
        void DisableRequestBuffering();
        void DisableResponseBuffering();
    }
}
