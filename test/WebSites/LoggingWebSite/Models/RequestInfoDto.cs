using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;

namespace LoggingWebSite
{
    public class RequestInfoDto
    {
        public Guid RequestID { get; set; }

        public string Host { get; set; }

        public string Path { get; set; }

        public string ContentType { get; set; }

        public string Scheme { get; set; }

        public int StatusCode { get; set; }

        public string Method { get; set; }

        public string Protocol { get; set; }

        public IEnumerable<KeyValuePair<string, string[]>> Headers { get; set; }

        public string Query { get; set; }
        
        public IEnumerable<KeyValuePair<string, string[]>> Cookies { get; set; }
    }
}