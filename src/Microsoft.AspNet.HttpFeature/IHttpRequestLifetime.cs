using System.Threading;

namespace Microsoft.AspNet.HttpFeature
{
    public interface IHttpRequestLifetime
    {
        CancellationToken OnRequestAborted { get; }
        void Abort();
    }
}