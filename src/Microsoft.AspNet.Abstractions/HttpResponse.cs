using System.IO;

namespace Microsoft.AspNet.Abstractions
{
    public abstract class HttpResponse
    {
        // TODO - review IOwinResponse for completeness

        public abstract HttpContext HttpContext { get; }
        public abstract int StatusCode { get; set; }
        public abstract Stream Body { get; set; }
    }
}
