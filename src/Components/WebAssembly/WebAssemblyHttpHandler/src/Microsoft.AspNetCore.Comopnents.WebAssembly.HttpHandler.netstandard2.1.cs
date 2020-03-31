using System;
using System.Net.Http;

namespace WebAssembly.Net.Http.HttpClient
{
    public enum FetchCredentialsOption
    {
        Include = 2,
        Omit = 0,
        SameOrigin = 1,
    }
    public enum RequestCache
    {
        Default = 0,
        ForceCache = 4,
        NoCache = 3,
        NoStore = 1,
        OnlyIfCached = 5,
        Reload = 2,
    }
    public enum RequestMode
    {
        Cors = 2,
        Navigate = 3,
        NoCors = 1,
        SameOrigin = 0,
    }
    public partial class WasmHttpMessageHandler : System.Net.Http.HttpMessageHandler
    {
        public WasmHttpMessageHandler() { throw new NotSupportedException(); }
        public static WebAssembly.Net.Http.HttpClient.RequestCache Cache { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public static WebAssembly.Net.Http.HttpClient.FetchCredentialsOption DefaultCredentials { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public static WebAssembly.Net.Http.HttpClient.RequestMode Mode { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public static bool StreamingEnabled { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public static bool StreamingSupported { get { throw new NotSupportedException(); } }
        protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}