# Microsoft.AspNetCore.ResponseCompression

``` diff
 namespace Microsoft.AspNetCore.ResponseCompression {
     public class BrotliCompressionProvider : ICompressionProvider {
         public BrotliCompressionProvider(IOptions<BrotliCompressionProviderOptions> options);
         public string EncodingName { get; }
         public bool SupportsFlush { get; }
         public Stream CreateStream(Stream outputStream);
     }
     public class BrotliCompressionProviderOptions : IOptions<BrotliCompressionProviderOptions> {
         public BrotliCompressionProviderOptions();
         public CompressionLevel Level { get; set; }
         BrotliCompressionProviderOptions Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>.Value { get; }
     }
     public class CompressionProviderCollection : Collection<ICompressionProvider> {
         public CompressionProviderCollection();
         public void Add(Type providerType);
         public void Add<TCompressionProvider>() where TCompressionProvider : ICompressionProvider;
     }
     public class GzipCompressionProvider : ICompressionProvider {
         public GzipCompressionProvider(IOptions<GzipCompressionProviderOptions> options);
         public string EncodingName { get; }
         public bool SupportsFlush { get; }
         public Stream CreateStream(Stream outputStream);
     }
     public class GzipCompressionProviderOptions : IOptions<GzipCompressionProviderOptions> {
         public GzipCompressionProviderOptions();
         public CompressionLevel Level { get; set; }
         GzipCompressionProviderOptions Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>.Value { get; }
     }
     public interface ICompressionProvider {
         string EncodingName { get; }
         bool SupportsFlush { get; }
         Stream CreateStream(Stream outputStream);
     }
     public interface IResponseCompressionProvider {
         bool CheckRequestAcceptsCompression(HttpContext context);
         ICompressionProvider GetCompressionProvider(HttpContext context);
         bool ShouldCompressResponse(HttpContext context);
     }
     public class ResponseCompressionDefaults {
         public static readonly IEnumerable<string> MimeTypes;
         public ResponseCompressionDefaults();
     }
     public class ResponseCompressionMiddleware {
         public ResponseCompressionMiddleware(RequestDelegate next, IResponseCompressionProvider provider);
         public Task Invoke(HttpContext context);
     }
     public class ResponseCompressionOptions {
         public ResponseCompressionOptions();
         public bool EnableForHttps { get; set; }
         public IEnumerable<string> ExcludedMimeTypes { get; set; }
         public IEnumerable<string> MimeTypes { get; set; }
         public CompressionProviderCollection Providers { get; }
     }
     public class ResponseCompressionProvider : IResponseCompressionProvider {
         public ResponseCompressionProvider(IServiceProvider services, IOptions<ResponseCompressionOptions> options);
         public bool CheckRequestAcceptsCompression(HttpContext context);
         public virtual ICompressionProvider GetCompressionProvider(HttpContext context);
         public virtual bool ShouldCompressResponse(HttpContext context);
     }
 }
```

