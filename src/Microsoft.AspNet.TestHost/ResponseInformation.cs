using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.HttpFeature;

namespace Microsoft.AspNet.TestHost
{
    internal class ResponseInformation : IHttpResponseInformation
    {
        public ResponseInformation()
        {
            Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            Body = new MemoryStream();
        }

        public int StatusCode { get; set; }

        public string ReasonPhrase { get; set; }

        public IDictionary<string, string[]> Headers { get; set; }

        public Stream Body { get; set; }

        public void OnSendingHeaders(Action<object> callback, object state)
        {
            // TODO: Figure out how to implement this thing.
        }
    }
}
