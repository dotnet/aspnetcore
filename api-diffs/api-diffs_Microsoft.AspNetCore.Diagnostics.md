# Microsoft.AspNetCore.Diagnostics

``` diff
 namespace Microsoft.AspNetCore.Diagnostics {
     public class CompilationFailure {
         public CompilationFailure(string sourceFilePath, string sourceFileContent, string compiledContent, IEnumerable<DiagnosticMessage> messages);
         public CompilationFailure(string sourceFilePath, string sourceFileContent, string compiledContent, IEnumerable<DiagnosticMessage> messages, string failureSummary);
         public string CompiledContent { get; }
         public string FailureSummary { get; }
         public IEnumerable<DiagnosticMessage> Messages { get; }
         public string SourceFileContent { get; }
         public string SourceFilePath { get; }
     }
     public class DeveloperExceptionPageMiddleware {
-        public DeveloperExceptionPageMiddleware(RequestDelegate next, IOptions<DeveloperExceptionPageOptions> options, ILoggerFactory loggerFactory, IHostingEnvironment hostingEnvironment, DiagnosticSource diagnosticSource);

+        public DeveloperExceptionPageMiddleware(RequestDelegate next, IOptions<DeveloperExceptionPageOptions> options, ILoggerFactory loggerFactory, IWebHostEnvironment hostingEnvironment, DiagnosticSource diagnosticSource, IEnumerable<IDeveloperPageExceptionFilter> filters);
         public Task Invoke(HttpContext context);
     }
     public class DiagnosticMessage {
         public DiagnosticMessage(string message, string formattedMessage, string filePath, int startLine, int startColumn, int endLine, int endColumn);
         public int EndColumn { get; }
         public int EndLine { get; }
         public string FormattedMessage { get; }
         public string Message { get; }
         public string SourceFilePath { get; }
         public int StartColumn { get; }
         public int StartLine { get; }
     }
+    public class ErrorContext {
+        public ErrorContext(HttpContext httpContext, Exception exception);
+        public Exception Exception { get; }
+        public HttpContext HttpContext { get; }
+    }
     public class ExceptionHandlerFeature : IExceptionHandlerFeature, IExceptionHandlerPathFeature {
         public ExceptionHandlerFeature();
         public Exception Error { get; set; }
         public string Path { get; set; }
     }
     public class ExceptionHandlerMiddleware {
+        public ExceptionHandlerMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<ExceptionHandlerOptions> options, DiagnosticListener diagnosticListener);
-        public ExceptionHandlerMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<ExceptionHandlerOptions> options, DiagnosticSource diagnosticSource);

         public Task Invoke(HttpContext context);
     }
     public interface ICompilationException {
         IEnumerable<CompilationFailure> CompilationFailures { get; }
     }
+    public interface IDeveloperPageExceptionFilter {
+        Task HandleExceptionAsync(ErrorContext errorContext, Func<ErrorContext, Task> next);
+    }
     public interface IExceptionHandlerFeature {
         Exception Error { get; }
     }
     public interface IExceptionHandlerPathFeature : IExceptionHandlerFeature {
         string Path { get; }
     }
     public interface IStatusCodePagesFeature {
         bool Enabled { get; set; }
     }
     public interface IStatusCodeReExecuteFeature {
         string OriginalPath { get; set; }
         string OriginalPathBase { get; set; }
         string OriginalQueryString { get; set; }
     }
     public class StatusCodeContext {
         public StatusCodeContext(HttpContext context, StatusCodePagesOptions options, RequestDelegate next);
         public HttpContext HttpContext { get; private set; }
         public RequestDelegate Next { get; private set; }
         public StatusCodePagesOptions Options { get; private set; }
     }
     public class StatusCodePagesFeature : IStatusCodePagesFeature {
         public StatusCodePagesFeature();
         public bool Enabled { get; set; }
     }
     public class StatusCodePagesMiddleware {
         public StatusCodePagesMiddleware(RequestDelegate next, IOptions<StatusCodePagesOptions> options);
         public Task Invoke(HttpContext context);
     }
     public class StatusCodeReExecuteFeature : IStatusCodeReExecuteFeature {
         public StatusCodeReExecuteFeature();
         public string OriginalPath { get; set; }
         public string OriginalPathBase { get; set; }
         public string OriginalQueryString { get; set; }
     }
     public class WelcomePageMiddleware {
         public WelcomePageMiddleware(RequestDelegate next, IOptions<WelcomePageOptions> options);
         public Task Invoke(HttpContext context);
     }
 }
```

