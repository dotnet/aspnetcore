using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class KestrelHttpContextFactory : HttpContextFactory
    {
        public KestrelHttpContextFactory(IOptions<FormOptions> formOptions)
            : this(formOptions, httpContextAccessor: null)
        {
        }


        public KestrelHttpContextFactory(IOptions<FormOptions> formOptions, IHttpContextAccessor httpContextAccessor) : base(formOptions, httpContextAccessor)
        {
        }

        protected override HttpContext CreateHttpContext(IFeatureCollection featureCollection)
        {
            if (featureCollection is HttpProtocol protocol)
            {
                Debug.Assert(protocol.HttpContext != null, "The HttpContext should be initialized by the request processing loop");
                return protocol.HttpContext;
            }

            // Since Kestrel is registered by default, we need to fallback to the default behavior
            return base.CreateHttpContext(featureCollection);
        }
    }
}
