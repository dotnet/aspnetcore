using System.IO;

namespace Microsoft.AspNet.Abstractions
{
    public abstract class HttpResponseBase
    {
        // TODO - review IOwinResponse for completeness

        public abstract HttpContextBase HttpContext { get; }
        public abstract int StatusCode { get; set; }
        public abstract Stream Body { get; set; }
    }
}
