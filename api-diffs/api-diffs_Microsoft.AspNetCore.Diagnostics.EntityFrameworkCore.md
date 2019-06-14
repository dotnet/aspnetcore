# Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore

``` diff
-namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore {
 {
-    public class DatabaseErrorPageMiddleware : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object>> {
 {
-        public DatabaseErrorPageMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<DatabaseErrorPageOptions> options);

-        public virtual Task Invoke(HttpContext httpContext);

-        void System.IObserver<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.OnCompleted();

-        void System.IObserver<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.OnError(Exception error);

-        void System.IObserver<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.OnNext(KeyValuePair<string, object> keyValuePair);

-        void System.IObserver<System.Diagnostics.DiagnosticListener>.OnCompleted();

-        void System.IObserver<System.Diagnostics.DiagnosticListener>.OnError(Exception error);

-        void System.IObserver<System.Diagnostics.DiagnosticListener>.OnNext(DiagnosticListener diagnosticListener);

-    }
-    public class MigrationsEndPointMiddleware {
 {
-        public MigrationsEndPointMiddleware(RequestDelegate next, ILogger<MigrationsEndPointMiddleware> logger, IOptions<MigrationsEndPointOptions> options);

-        public virtual Task Invoke(HttpContext context);

-    }
-}
```

