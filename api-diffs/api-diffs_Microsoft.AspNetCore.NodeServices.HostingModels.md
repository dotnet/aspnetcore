# Microsoft.AspNetCore.NodeServices.HostingModels

``` diff
-namespace Microsoft.AspNetCore.NodeServices.HostingModels {
 {
-    public interface INodeInstance : IDisposable {
 {
-        Task<T> InvokeExportAsync<T>(CancellationToken cancellationToken, string moduleName, string exportNameOrNull, params object[] args);

-    }
-    public class NodeInvocationException : Exception {
 {
-        public NodeInvocationException(string message, string details);

-        public NodeInvocationException(string message, string details, bool nodeInstanceUnavailable, bool allowConnectionDraining);

-        public bool AllowConnectionDraining { get; private set; }

-        public bool NodeInstanceUnavailable { get; private set; }

-    }
-    public class NodeInvocationInfo {
 {
-        public NodeInvocationInfo();

-        public object[] Args { get; set; }

-        public string ExportedFunctionName { get; set; }

-        public string ModuleName { get; set; }

-    }
-    public static class NodeServicesOptionsExtensions {
 {
-        public static void UseHttpHosting(this NodeServicesOptions options);

-    }
-    public abstract class OutOfProcessNodeInstance : IDisposable, INodeInstance {
 {
-        protected readonly ILogger OutputLogger;

-        public OutOfProcessNodeInstance(string entryPointScript, string projectPath, string[] watchFileExtensions, string commandLineArguments, CancellationToken applicationStoppingToken, ILogger nodeOutputLogger, IDictionary<string, string> environmentVars, int invocationTimeoutMilliseconds, bool launchWithDebugging, int debuggingPort);

-        public void Dispose();

-        protected virtual void Dispose(bool disposing);

-        ~OutOfProcessNodeInstance();

-        protected abstract Task<T> InvokeExportAsync<T>(NodeInvocationInfo invocationInfo, CancellationToken cancellationToken);

-        public Task<T> InvokeExportAsync<T>(CancellationToken cancellationToken, string moduleName, string exportNameOrNull, params object[] args);

-        protected virtual void OnErrorDataReceived(string errorData);

-        protected virtual void OnOutputDataReceived(string outputData);

-        protected virtual ProcessStartInfo PrepareNodeProcessStartInfo(string entryPointFilename, string projectPath, string commandLineArguments, IDictionary<string, string> environmentVars, bool launchWithDebugging, int debuggingPort);

-    }
-}
```

