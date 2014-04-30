using System.Threading;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.HttpFeature
{
    [AssemblyNeutral]
    public interface IHttpRequestLifetime
    {
        CancellationToken OnRequestAborted { get; }
        void Abort();
    }
}