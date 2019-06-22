# System.Net.Http.Handlers

``` diff
-namespace System.Net.Http.Handlers {
 {
-    public class HttpProgressEventArgs : ProgressChangedEventArgs {
 {
-        public HttpProgressEventArgs(int progressPercentage, object userToken, long bytesTransferred, Nullable<long> totalBytes);

-        public long BytesTransferred { get; private set; }

-        public Nullable<long> TotalBytes { get; private set; }

-    }
-    public class ProgressMessageHandler : DelegatingHandler {
 {
-        public ProgressMessageHandler();

-        public ProgressMessageHandler(HttpMessageHandler innerHandler);

-        public event EventHandler<HttpProgressEventArgs> HttpReceiveProgress;

-        public event EventHandler<HttpProgressEventArgs> HttpSendProgress;

-        protected internal virtual void OnHttpRequestProgress(HttpRequestMessage request, HttpProgressEventArgs e);

-        protected internal virtual void OnHttpResponseProgress(HttpRequestMessage request, HttpProgressEventArgs e);

-        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);

-    }
-}
```

