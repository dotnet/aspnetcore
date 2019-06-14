# Microsoft.AspNetCore.Diagnostics

``` diff
 namespace Microsoft.AspNetCore.Diagnostics {
     public class DeveloperExceptionPageMiddleware {
-        public DeveloperExceptionPageMiddleware(RequestDelegate next, IOptions<DeveloperExceptionPageOptions> options, ILoggerFactory loggerFactory, IHostingEnvironment hostingEnvironment, DiagnosticSource diagnosticSource);

+        public DeveloperExceptionPageMiddleware(RequestDelegate next, IOptions<DeveloperExceptionPageOptions> options, ILoggerFactory loggerFactory, IWebHostEnvironment hostingEnvironment, DiagnosticSource diagnosticSource, IEnumerable<IDeveloperPageExceptionFilter> filters);
     }
+    public class ErrorContext {
+        public ErrorContext(HttpContext httpContext, Exception exception);
+        public Exception Exception { get; }
+        public HttpContext HttpContext { get; }
+    }
     public class ExceptionHandlerMiddleware {
+        public ExceptionHandlerMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<ExceptionHandlerOptions> options, DiagnosticListener diagnosticListener);
-        public ExceptionHandlerMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<ExceptionHandlerOptions> options, DiagnosticSource diagnosticSource);

     }
+    public interface IDeveloperPageExceptionFilter {
+        Task HandleExceptionAsync(ErrorContext errorContext, Func<ErrorContext, Task> next);
+    }
 }
```

