using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Abstractions
{
    public abstract class HttpResponse
    {
        // TODO - review IOwinResponse for completeness

        public abstract HttpContext HttpContext { get; }
        public abstract int StatusCode { get; set; }
        public abstract Stream Body { get; set; }

        public abstract string ContentType { get; set; }

        public abstract Task WriteAsync(string data);
    }
}
