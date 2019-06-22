# Microsoft.IdentityModel.Protocols

``` diff
-namespace Microsoft.IdentityModel.Protocols {
 {
-    public abstract class AuthenticationProtocolMessage {
 {
-        protected AuthenticationProtocolMessage();

-        public string IssuerAddress { get; set; }

-        public IDictionary<string, string> Parameters { get; }

-        public string PostTitle { get; set; }

-        public string ScriptButtonText { get; set; }

-        public string ScriptDisabledText { get; set; }

-        public virtual string BuildFormPost();

-        public virtual string BuildRedirectUrl();

-        public virtual string GetParameter(string parameter);

-        public virtual void RemoveParameter(string parameter);

-        public void SetParameter(string parameter, string value);

-        public virtual void SetParameters(NameValueCollection nameValueCollection);

-    }
-    public class ConfigurationManager<T> : IConfigurationManager<T> where T : class {
 {
-        public static readonly TimeSpan DefaultAutomaticRefreshInterval;

-        public static readonly TimeSpan DefaultRefreshInterval;

-        public static readonly TimeSpan MinimumAutomaticRefreshInterval;

-        public static readonly TimeSpan MinimumRefreshInterval;

-        public ConfigurationManager(string metadataAddress, IConfigurationRetriever<T> configRetriever);

-        public ConfigurationManager(string metadataAddress, IConfigurationRetriever<T> configRetriever, IDocumentRetriever docRetriever);

-        public ConfigurationManager(string metadataAddress, IConfigurationRetriever<T> configRetriever, HttpClient httpClient);

-        public TimeSpan AutomaticRefreshInterval { get; set; }

-        public TimeSpan RefreshInterval { get; set; }

-        public Task<T> GetConfigurationAsync();

-        public Task<T> GetConfigurationAsync(CancellationToken cancel);

-        public void RequestRefresh();

-    }
-    public class FileDocumentRetriever : IDocumentRetriever {
 {
-        public FileDocumentRetriever();

-        public Task<string> GetDocumentAsync(string address, CancellationToken cancel);

-    }
-    public class HttpDocumentRetriever : IDocumentRetriever {
 {
-        public HttpDocumentRetriever();

-        public HttpDocumentRetriever(HttpClient httpClient);

-        public bool RequireHttps { get; set; }

-        public Task<string> GetDocumentAsync(string address, CancellationToken cancel);

-    }
-    public interface IConfigurationManager<T> where T : class {
 {
-        Task<T> GetConfigurationAsync(CancellationToken cancel);

-        void RequestRefresh();

-    }
-    public interface IConfigurationRetriever<T> {
 {
-        Task<T> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel);

-    }
-    public interface IDocumentRetriever {
 {
-        Task<string> GetDocumentAsync(string address, CancellationToken cancel);

-    }
-    public class StaticConfigurationManager<T> : IConfigurationManager<T> where T : class {
 {
-        public StaticConfigurationManager(T configuration);

-        public Task<T> GetConfigurationAsync(CancellationToken cancel);

-        public void RequestRefresh();

-    }
-    public class X509CertificateValidationMode {
 {
-        public X509CertificateValidationMode();

-    }
-}
```

