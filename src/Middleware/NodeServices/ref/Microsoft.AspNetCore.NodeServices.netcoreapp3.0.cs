// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.NodeServices
{
    public static partial class EmbeddedResourceReader
    {
        public static string Read(System.Type assemblyContainingType, string path) { throw null; }
    }
    public partial interface INodeServices : System.IDisposable
    {
        System.Threading.Tasks.Task<T> InvokeAsync<T>(string moduleName, params object[] args);
        System.Threading.Tasks.Task<T> InvokeAsync<T>(System.Threading.CancellationToken cancellationToken, string moduleName, params object[] args);
        System.Threading.Tasks.Task<T> InvokeExportAsync<T>(string moduleName, string exportedFunctionName, params object[] args);
        System.Threading.Tasks.Task<T> InvokeExportAsync<T>(System.Threading.CancellationToken cancellationToken, string moduleName, string exportedFunctionName, params object[] args);
    }
    public static partial class NodeServicesFactory
    {
        public static Microsoft.AspNetCore.NodeServices.INodeServices CreateNodeServices(Microsoft.AspNetCore.NodeServices.NodeServicesOptions options) { throw null; }
    }
    public partial class NodeServicesOptions
    {
        public NodeServicesOptions(System.IServiceProvider serviceProvider) { }
        public System.Threading.CancellationToken ApplicationStoppingToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int DebuggingPort { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IDictionary<string, string> EnvironmentVariables { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int InvocationTimeoutMilliseconds { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool LaunchWithDebugging { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.NodeServices.HostingModels.INodeInstance> NodeInstanceFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.Logging.ILogger NodeInstanceOutputLogger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ProjectPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string[] WatchFileExtensions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public sealed partial class StringAsTempFile : System.IDisposable
    {
        public StringAsTempFile(string content, System.Threading.CancellationToken applicationStoppingToken) { }
        public string FileName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void Dispose() { }
        ~StringAsTempFile() { }
    }
}
namespace Microsoft.AspNetCore.NodeServices.HostingModels
{
    public partial interface INodeInstance : System.IDisposable
    {
        System.Threading.Tasks.Task<T> InvokeExportAsync<T>(System.Threading.CancellationToken cancellationToken, string moduleName, string exportNameOrNull, params object[] args);
    }
    public partial class NodeInvocationException : System.Exception
    {
        public NodeInvocationException(string message, string details) { }
        public NodeInvocationException(string message, string details, bool nodeInstanceUnavailable, bool allowConnectionDraining) { }
        public bool AllowConnectionDraining { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool NodeInstanceUnavailable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class NodeInvocationInfo
    {
        public NodeInvocationInfo() { }
        public object[] Args { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ExportedFunctionName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ModuleName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public static partial class NodeServicesOptionsExtensions
    {
        public static void UseHttpHosting(this Microsoft.AspNetCore.NodeServices.NodeServicesOptions options) { }
    }
    public abstract partial class OutOfProcessNodeInstance : Microsoft.AspNetCore.NodeServices.HostingModels.INodeInstance, System.IDisposable
    {
        protected readonly Microsoft.Extensions.Logging.ILogger OutputLogger;
        public OutOfProcessNodeInstance(string entryPointScript, string projectPath, string[] watchFileExtensions, string commandLineArguments, System.Threading.CancellationToken applicationStoppingToken, Microsoft.Extensions.Logging.ILogger nodeOutputLogger, System.Collections.Generic.IDictionary<string, string> environmentVars, int invocationTimeoutMilliseconds, bool launchWithDebugging, int debuggingPort) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~OutOfProcessNodeInstance() { }
        protected abstract System.Threading.Tasks.Task<T> InvokeExportAsync<T>(Microsoft.AspNetCore.NodeServices.HostingModels.NodeInvocationInfo invocationInfo, System.Threading.CancellationToken cancellationToken);
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<T> InvokeExportAsync<T>(System.Threading.CancellationToken cancellationToken, string moduleName, string exportNameOrNull, params object[] args) { throw null; }
        protected virtual void OnErrorDataReceived(string errorData) { }
        protected virtual void OnOutputDataReceived(string outputData) { }
        protected virtual System.Diagnostics.ProcessStartInfo PrepareNodeProcessStartInfo(string entryPointFilename, string projectPath, string commandLineArguments, System.Collections.Generic.IDictionary<string, string> environmentVars, bool launchWithDebugging, int debuggingPort) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class NodeServicesServiceCollectionExtensions
    {
        public static void AddNodeServices(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection) { }
        public static void AddNodeServices(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection, System.Action<Microsoft.AspNetCore.NodeServices.NodeServicesOptions> setupAction) { }
    }
}
