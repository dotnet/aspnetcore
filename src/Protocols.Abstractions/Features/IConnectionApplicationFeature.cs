using System;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Protocols.Features
{
    public interface IConnectionApplicationFeature
    {
        IPipeConnection Connection { get; set; }

        // TODO: Remove these (https://github.com/aspnet/KestrelHttpServer/issues/1772)
        // REVIEW: These are around for now because handling pipe events messes with the order
        // of operations an that breaks tons of tests. Instead, we preserve the existing semantics
        // and ordering.
        void Abort(Exception exception);

        void OnConnectionClosed(Exception exception);
    }
}
