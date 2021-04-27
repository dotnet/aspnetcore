using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HttpLogging
{
    public class HttpRequestLoggingContext
    {
        public HttpContext HttpContext { get; }
        // IDictionary<string, StringValues> -> does order matter
        // string -> com
        // order is contentious (maybe just be consistent between logs)
        // Path, Query as properties? Don't mix collections.

        public string? Path { get; set; } // null means don't log
        public string? Query { get; set; }
        public string? Scheme { get; set; }
        public string? Method { get; set; }

        public IHeaderDictionary Headers { get; }

        public IDictionary<string, string> Extra { get; }
    }

    public class HttpResponseLoggingContext
    {
        public HttpContext HttpContext { get; }
        // IDictionary<string, StringValues> -> does order matter
        // string -> com
        // order is contentious (maybe just be consistent between logs)
        // Path, Query as properties? Don't mix collections.

        public string? StatusCode { get; set; }

        public IHeaderDictionary Headers { get; }

        public IDictionary<string, string> Extra { get; }
    }
}
