using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.HttpFeature;

namespace Microsoft.AspNet.TestHost
{
    internal class RequestInformation : IHttpRequestInformation
    {
        public RequestInformation()
        {
            Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            PathBase = "";
            Body = Stream.Null;
            Protocol = "HTTP/1.1";
        }

        public Stream Body { get; set; }

        public IDictionary<string, string[]> Headers { get; set; }

        public string Method { get; set; }

        public string Path { get; set; }

        public string PathBase { get; set; }

        public string Protocol { get; set; }

        public string QueryString { get; set; }

        public string Scheme { get; set; }
    }
}
