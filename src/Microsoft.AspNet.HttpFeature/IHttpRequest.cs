using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNet.Interfaces
{
    public interface IHttpRequest
    {
        string Protocol { get; set; }
        string Scheme { get; set; }
        string Method { get; set; }
        string PathBase { get; set; }
        string Path { get; set; }
        string QueryString { get; set; }
        IDictionary<string, string[]> Headers { get; set; }
        Stream Body { get; set; }
        Uri Uri { get; }
    }
}
