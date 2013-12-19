using System;
using System.IO;

namespace Microsoft.AspNet.Abstractions
{
    public abstract class HttpRequestBase
    {
        // TODO - review IOwinRequest for properties

        public abstract HttpContextBase HttpContext { get; }

        public abstract Uri Uri { get;  }
        public abstract PathString PathBase { get; set; }
        public abstract PathString Path { get; set; }
        public abstract QueryString QueryString { get; set; }
        public abstract Stream Body { get; set; }
    }
}
