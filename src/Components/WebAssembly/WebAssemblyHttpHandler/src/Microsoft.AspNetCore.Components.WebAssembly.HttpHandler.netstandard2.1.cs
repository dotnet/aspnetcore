using System;
using System.Net.Http;

namespace System.Net.Http
{
    public partial class WebAssemblyHttpHandler : System.Net.Http.HttpMessageHandler
    {
        public WebAssemblyHttpHandler() { }
        protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
