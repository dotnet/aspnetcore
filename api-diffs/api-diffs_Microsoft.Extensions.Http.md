# Microsoft.Extensions.Http

``` diff
 namespace Microsoft.Extensions.Http {
     public class HttpClientFactoryOptions {
         public HttpClientFactoryOptions();
         public TimeSpan HandlerLifetime { get; set; }
         public IList<Action<HttpClient>> HttpClientActions { get; }
         public IList<Action<HttpMessageHandlerBuilder>> HttpMessageHandlerBuilderActions { get; }
         public bool SuppressHandlerScope { get; set; }
     }
     public abstract class HttpMessageHandlerBuilder {
         protected HttpMessageHandlerBuilder();
         public abstract IList<DelegatingHandler> AdditionalHandlers { get; }
         public abstract string Name { get; set; }
         public abstract HttpMessageHandler PrimaryHandler { get; set; }
         public virtual IServiceProvider Services { get; }
         public abstract HttpMessageHandler Build();
         protected internal static HttpMessageHandler CreateHandlerPipeline(HttpMessageHandler primaryHandler, IEnumerable<DelegatingHandler> additionalHandlers);
     }
     public interface IHttpMessageHandlerBuilderFilter {
         Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next);
     }
     public interface ITypedHttpClientFactory<TClient> {
         TClient CreateClient(HttpClient httpClient);
     }
 }
```

