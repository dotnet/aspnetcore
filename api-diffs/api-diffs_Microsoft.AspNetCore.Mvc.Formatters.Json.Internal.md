# Microsoft.AspNetCore.Mvc.Formatters.Json.Internal

``` diff
-namespace Microsoft.AspNetCore.Mvc.Formatters.Json.Internal {
 {
-    public class JsonArrayPool<T> : IArrayPool<T> {
 {
-        public JsonArrayPool(ArrayPool<T> inner);

-        public T[] Rent(int minimumLength);

-        public void Return(T[] array);

-    }
-    public class JsonResultExecutor {
 {
-        public JsonResultExecutor(IHttpResponseStreamWriterFactory writerFactory, ILogger<JsonResultExecutor> logger, IOptions<MvcJsonOptions> options, ArrayPool<char> charPool);

-        protected ILogger Logger { get; }

-        protected MvcJsonOptions Options { get; }

-        protected IHttpResponseStreamWriterFactory WriterFactory { get; }

-        public virtual Task ExecuteAsync(ActionContext context, JsonResult result);

-    }
-    public class JsonSerializerObjectPolicy : IPooledObjectPolicy<JsonSerializer> {
 {
-        public JsonSerializerObjectPolicy(JsonSerializerSettings serializerSettings);

-        public JsonSerializer Create();

-        public bool Return(JsonSerializer serializer);

-    }
-    public class MvcJsonMvcOptionsSetup : IConfigureOptions<MvcOptions> {
 {
-        public MvcJsonMvcOptionsSetup(ILoggerFactory loggerFactory, IOptions<MvcJsonOptions> jsonOptions, ArrayPool<char> charPool, ObjectPoolProvider objectPoolProvider);

-        public void Configure(MvcOptions options);

-    }
-}
```

