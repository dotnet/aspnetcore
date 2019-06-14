# Microsoft.AspNetCore.NodeServices

``` diff
-namespace Microsoft.AspNetCore.NodeServices {
 {
-    public static class EmbeddedResourceReader {
 {
-        public static string Read(Type assemblyContainingType, string path);

-    }
-    public interface INodeServices : IDisposable {
 {
-        Task<T> InvokeAsync<T>(string moduleName, params object[] args);

-        Task<T> InvokeAsync<T>(CancellationToken cancellationToken, string moduleName, params object[] args);

-        Task<T> InvokeExportAsync<T>(string moduleName, string exportedFunctionName, params object[] args);

-        Task<T> InvokeExportAsync<T>(CancellationToken cancellationToken, string moduleName, string exportedFunctionName, params object[] args);

-    }
-    public static class NodeServicesFactory {
 {
-        public static INodeServices CreateNodeServices(NodeServicesOptions options);

-    }
-    public class NodeServicesOptions {
 {
-        public NodeServicesOptions(IServiceProvider serviceProvider);

-        public CancellationToken ApplicationStoppingToken { get; set; }

-        public int DebuggingPort { get; set; }

-        public IDictionary<string, string> EnvironmentVariables { get; set; }

-        public int InvocationTimeoutMilliseconds { get; set; }

-        public bool LaunchWithDebugging { get; set; }

-        public Func<INodeInstance> NodeInstanceFactory { get; set; }

-        public ILogger NodeInstanceOutputLogger { get; set; }

-        public string ProjectPath { get; set; }

-        public string[] WatchFileExtensions { get; set; }

-    }
-    public sealed class StringAsTempFile : IDisposable {
 {
-        public StringAsTempFile(string content, CancellationToken applicationStoppingToken);

-        public string FileName { get; }

-        public void Dispose();

-        ~StringAsTempFile();

-    }
-}
```

