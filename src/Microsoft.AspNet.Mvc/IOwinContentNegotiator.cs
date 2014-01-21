using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Net.Http.Formatting;

namespace Microsoft.AspNet.Mvc
{
    public interface IOwinContentNegotiator
    {
        ContentNegotiationResult Negotiate(Type type, IOwinContext context, IEnumerable<MediaTypeFormatter> formatters);
    }
}
