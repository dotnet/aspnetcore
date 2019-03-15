using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal class Http2Stream<TContext> : Http2Stream
    {
        private readonly IHttpApplication<TContext> _application;

        public Http2Stream(IHttpApplication<TContext> application, Http2StreamContext context) : base(context)
        {
            _application = application;
        }

        public override void Execute()
        {
            // REVIEW: Should we store this in a field for easy debugging?
            _ = ProcessRequestsAsync(_application);
        }
    }
}
