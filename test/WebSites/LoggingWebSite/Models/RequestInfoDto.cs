using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace LoggingWebSite
{
    public class RequestInfoDto
    {
        public string RequestID { get; set; }

        public string Host { get; set; }

        public string Path { get; set; }

        public string ContentType { get; set; }

        public string Scheme { get; set; }

        public int StatusCode { get; set; }

        public string Method { get; set; }

        public string Protocol { get; set; }

        public IEnumerable<KeyValuePair<string, StringValues>> Headers { get; set; }

        public string Query { get; set; }

        public IEnumerable<KeyValuePair<string, string>> Cookies { get; set; }
    }
}