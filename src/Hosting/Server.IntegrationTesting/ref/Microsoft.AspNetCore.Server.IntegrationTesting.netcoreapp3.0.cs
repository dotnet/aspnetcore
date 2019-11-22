// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Hosting
{
    public static partial class IWebHostExtensions
    {
        public static string GetAddress(this Microsoft.AspNetCore.Hosting.IWebHost host) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public abstract partial class ApplicationDeployer : System.IDisposable
    {
        public static readonly string DotnetCommandName;
        public ApplicationDeployer(Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentParameters deploymentParameters, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        protected Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentParameters DeploymentParameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected Microsoft.Extensions.Logging.ILogger Logger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected Microsoft.Extensions.Logging.ILoggerFactory LoggerFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected void AddEnvironmentVariablesToProcess(System.Diagnostics.ProcessStartInfo startInfo, System.Collections.Generic.IDictionary<string, string> environmentVariables) { }
        protected void CleanPublishedOutput() { }
        public abstract System.Threading.Tasks.Task<Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentResult> DeployAsync();
        public abstract void Dispose();
        protected void DotnetPublish(string publishRoot = null) { }
        protected string GetDotNetExeForArchitecture() { throw null; }
        protected void InvokeUserApplicationCleanup() { }
        protected void ShutDownIfAnyHostProcess(System.Diagnostics.Process hostProcess) { }
        protected void StartTimer() { }
        protected void StopTimer() { }
        protected void TriggerHostShutdown(System.Threading.CancellationTokenSource hostShutdownSource) { }
    }
    public partial class ApplicationDeployerFactory
    {
        public ApplicationDeployerFactory() { }
        public static Microsoft.AspNetCore.Server.IntegrationTesting.ApplicationDeployer Create(Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentParameters deploymentParameters, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { throw null; }
    }
    public partial class ApplicationPublisher
    {
        public static readonly string DotnetCommandName;
        public ApplicationPublisher(string applicationPath) { }
        public string ApplicationPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected static System.IO.DirectoryInfo CreateTempDirectory() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Server.IntegrationTesting.PublishedApplication> Publish(Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentParameters deploymentParameters, Microsoft.Extensions.Logging.ILogger logger) { throw null; }
    }
    public enum ApplicationType
    {
        Portable = 0,
        Standalone = 1,
    }
    public partial class CachingApplicationPublisher : Microsoft.AspNetCore.Server.IntegrationTesting.ApplicationPublisher, System.IDisposable
    {
        public CachingApplicationPublisher(string applicationPath) : base (default(string)) { }
        public static void CopyFiles(System.IO.DirectoryInfo source, System.IO.DirectoryInfo target, Microsoft.Extensions.Logging.ILogger logger) { }
        public void Dispose() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Server.IntegrationTesting.PublishedApplication> Publish(Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentParameters deploymentParameters, Microsoft.Extensions.Logging.ILogger logger) { throw null; }
    }
    public partial class DeploymentParameters
    {
        public DeploymentParameters() { }
        public DeploymentParameters(Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentParameters parameters) { }
        public DeploymentParameters(Microsoft.AspNetCore.Server.IntegrationTesting.TestVariant variant) { }
        public DeploymentParameters(string applicationPath, Microsoft.AspNetCore.Server.IntegrationTesting.ServerType serverType, Microsoft.AspNetCore.Server.IntegrationTesting.RuntimeFlavor runtimeFlavor, Microsoft.AspNetCore.Server.IntegrationTesting.RuntimeArchitecture runtimeArchitecture) { }
        public string AdditionalPublishParameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ApplicationBaseUriHint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ApplicationName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ApplicationPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.IntegrationTesting.ApplicationPublisher ApplicationPublisher { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.IntegrationTesting.ApplicationType ApplicationType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Configuration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string EnvironmentName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IDictionary<string, string> EnvironmentVariables { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.IntegrationTesting.HostingModel HostingModel { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool PreservePublishedApplicationForDebugging { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool PublishApplicationBeforeDeployment { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string PublishedApplicationRootPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IDictionary<string, string> PublishEnvironmentVariables { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.IntegrationTesting.RuntimeArchitecture RuntimeArchitecture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.IntegrationTesting.RuntimeFlavor RuntimeFlavor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Scheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ServerConfigLocation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ServerConfigTemplateContent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.IntegrationTesting.ServerType ServerType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SiteName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool StatusMessagesEnabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string TargetFramework { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Action<Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentParameters> UserAdditionalCleanup { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override string ToString() { throw null; }
    }
    public partial class DeploymentResult
    {
        public DeploymentResult(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentParameters deploymentParameters, string applicationBaseUri) { }
        public DeploymentResult(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentParameters deploymentParameters, string applicationBaseUri, string contentRoot, System.Threading.CancellationToken hostShutdownToken) { }
        public string ApplicationBaseUri { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string ContentRoot { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentParameters DeploymentParameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Threading.CancellationToken HostShutdownToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Net.Http.HttpClient HttpClient { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Net.Http.HttpClient CreateHttpClient(System.Net.Http.HttpMessageHandler baseHandler) { throw null; }
    }
    public static partial class DotNetCommands
    {
        public static string GetDotNetExecutable(Microsoft.AspNetCore.Server.IntegrationTesting.RuntimeArchitecture arch) { throw null; }
        public static string GetDotNetHome() { throw null; }
        public static string GetDotNetInstallDir(Microsoft.AspNetCore.Server.IntegrationTesting.RuntimeArchitecture arch) { throw null; }
        public static bool IsRunningX86OnX64(Microsoft.AspNetCore.Server.IntegrationTesting.RuntimeArchitecture arch) { throw null; }
    }
    public enum HostingModel
    {
        InProcess = 2,
        None = 0,
        OutOfProcess = 1,
    }
    public partial class IISExpressAncmSchema
    {
        public IISExpressAncmSchema() { }
        public static string SkipReason { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static bool SupportsInProcessHosting { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class NginxDeployer : Microsoft.AspNetCore.Server.IntegrationTesting.SelfHostDeployer
    {
        public NginxDeployer(Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentParameters deploymentParameters, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) : base (default(Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentParameters), default(Microsoft.Extensions.Logging.ILoggerFactory)) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentResult> DeployAsync() { throw null; }
        public override void Dispose() { }
    }
    public partial class PublishedApplication : System.IDisposable
    {
        public PublishedApplication(string path, Microsoft.Extensions.Logging.ILogger logger) { }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void Dispose() { }
    }
    public partial class RemoteWindowsDeployer : Microsoft.AspNetCore.Server.IntegrationTesting.ApplicationDeployer
    {
        public RemoteWindowsDeployer(Microsoft.AspNetCore.Server.IntegrationTesting.RemoteWindowsDeploymentParameters deploymentParameters, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) : base (default(Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentParameters), default(Microsoft.Extensions.Logging.ILoggerFactory)) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentResult> DeployAsync() { throw null; }
        public override void Dispose() { }
    }
    public partial class RemoteWindowsDeploymentParameters : Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentParameters
    {
        public RemoteWindowsDeploymentParameters(string applicationPath, string dotnetRuntimePath, Microsoft.AspNetCore.Server.IntegrationTesting.ServerType serverType, Microsoft.AspNetCore.Server.IntegrationTesting.RuntimeFlavor runtimeFlavor, Microsoft.AspNetCore.Server.IntegrationTesting.RuntimeArchitecture runtimeArchitecture, string remoteServerFileSharePath, string remoteServerName, string remoteServerAccountName, string remoteServerAccountPassword) { }
        public string DotnetRuntimePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string RemoteServerFileSharePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string ServerAccountName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string ServerAccountPassword { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string ServerName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class RetryHelper
    {
        public RetryHelper() { }
        public static void RetryOperation(System.Action retryBlock, System.Action<System.Exception> exceptionBlock, int retryCount = 3, int retryDelayMilliseconds = 0) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> RetryRequest(System.Func<System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>> retryBlock, Microsoft.Extensions.Logging.ILogger logger, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken), int retryCount = 60) { throw null; }
    }
    public enum RuntimeArchitecture
    {
        x64 = 0,
        x86 = 1,
    }
    public enum RuntimeFlavor
    {
        Clr = 2,
        CoreClr = 1,
        None = 0,
    }
    public partial class SelfHostDeployer : Microsoft.AspNetCore.Server.IntegrationTesting.ApplicationDeployer
    {
        public SelfHostDeployer(Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentParameters deploymentParameters, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) : base (default(Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentParameters), default(Microsoft.Extensions.Logging.ILoggerFactory)) { }
        public System.Diagnostics.Process HostProcess { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Server.IntegrationTesting.DeploymentResult> DeployAsync() { throw null; }
        public override void Dispose() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected System.Threading.Tasks.Task<System.ValueTuple<System.Uri, System.Threading.CancellationToken>> StartSelfHostAsync(System.Uri hintUrl) { throw null; }
    }
    public enum ServerType
    {
        HttpSys = 3,
        IIS = 2,
        IISExpress = 1,
        Kestrel = 4,
        Nginx = 5,
        None = 0,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=true)]
    public partial class SkipIfEnvironmentVariableNotEnabledAttribute : System.Attribute, Microsoft.AspNetCore.Testing.ITestCondition
    {
        public SkipIfEnvironmentVariableNotEnabledAttribute(string environmentVariableName) { }
        public string AdditionalInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IsMet { get { throw null; } }
        public string SkipReason { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Method)]
    public sealed partial class SkipIfIISExpressSchemaMissingInProcessAttribute : System.Attribute, Microsoft.AspNetCore.Testing.ITestCondition
    {
        public SkipIfIISExpressSchemaMissingInProcessAttribute() { }
        public bool IsMet { get { throw null; } }
        public string SkipReason { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false)]
    public partial class SkipOn32BitOSAttribute : System.Attribute, Microsoft.AspNetCore.Testing.ITestCondition
    {
        public SkipOn32BitOSAttribute() { }
        public bool IsMet { get { throw null; } }
        public string SkipReason { get { throw null; } }
    }
    public partial class TestMatrix : System.Collections.Generic.IEnumerable<object[]>, System.Collections.IEnumerable
    {
        public TestMatrix() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Server.IntegrationTesting.ApplicationType> ApplicationTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Server.IntegrationTesting.RuntimeArchitecture> Architectures { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Server.IntegrationTesting.HostingModel> HostingModels { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Server.IntegrationTesting.ServerType> Servers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IList<string> Tfms { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public static Microsoft.AspNetCore.Server.IntegrationTesting.TestMatrix ForServers(params Microsoft.AspNetCore.Server.IntegrationTesting.ServerType[] types) { throw null; }
        public System.Collections.Generic.IEnumerator<object[]> GetEnumerator() { throw null; }
        public Microsoft.AspNetCore.Server.IntegrationTesting.TestMatrix Skip(string message, System.Func<Microsoft.AspNetCore.Server.IntegrationTesting.TestVariant, bool> check) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public Microsoft.AspNetCore.Server.IntegrationTesting.TestMatrix WithAllApplicationTypes() { throw null; }
        public Microsoft.AspNetCore.Server.IntegrationTesting.TestMatrix WithAllArchitectures() { throw null; }
        public Microsoft.AspNetCore.Server.IntegrationTesting.TestMatrix WithAllHostingModels() { throw null; }
        public Microsoft.AspNetCore.Server.IntegrationTesting.TestMatrix WithAncmV2InProcess() { throw null; }
        public Microsoft.AspNetCore.Server.IntegrationTesting.TestMatrix WithApplicationTypes(params Microsoft.AspNetCore.Server.IntegrationTesting.ApplicationType[] types) { throw null; }
        public Microsoft.AspNetCore.Server.IntegrationTesting.TestMatrix WithArchitectures(params Microsoft.AspNetCore.Server.IntegrationTesting.RuntimeArchitecture[] archs) { throw null; }
        public Microsoft.AspNetCore.Server.IntegrationTesting.TestMatrix WithHostingModels(params Microsoft.AspNetCore.Server.IntegrationTesting.HostingModel[] models) { throw null; }
        public Microsoft.AspNetCore.Server.IntegrationTesting.TestMatrix WithTfms(params string[] tfms) { throw null; }
    }
    public partial class TestVariant : Xunit.Abstractions.IXunitSerializable
    {
        public TestVariant() { }
        public Microsoft.AspNetCore.Server.IntegrationTesting.ApplicationType ApplicationType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.IntegrationTesting.RuntimeArchitecture Architecture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.IntegrationTesting.HostingModel HostingModel { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.IntegrationTesting.ServerType Server { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Skip { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Tfm { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void Deserialize(Xunit.Abstractions.IXunitSerializationInfo info) { }
        public void Serialize(Xunit.Abstractions.IXunitSerializationInfo info) { }
        public override string ToString() { throw null; }
    }
    public static partial class Tfm
    {
        public const string Net461 = "net461";
        public const string NetCoreApp20 = "netcoreapp2.0";
        public const string NetCoreApp21 = "netcoreapp2.1";
        public const string NetCoreApp22 = "netcoreapp2.2";
        public const string NetCoreApp30 = "netcoreapp3.0";
        public static bool Matches(string tfm1, string tfm2) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.IntegrationTesting.Common
{
    public static partial class TestPortHelper
    {
        public static int GetNextPort() { throw null; }
        public static int GetNextSSLPort() { throw null; }
    }
    public static partial class TestUriHelper
    {
        public static System.Uri BuildTestUri(Microsoft.AspNetCore.Server.IntegrationTesting.ServerType serverType) { throw null; }
        public static System.Uri BuildTestUri(Microsoft.AspNetCore.Server.IntegrationTesting.ServerType serverType, string hint) { throw null; }
    }
    public static partial class TestUrlHelper
    {
        public static string GetTestUrl(Microsoft.AspNetCore.Server.IntegrationTesting.ServerType serverType) { throw null; }
    }
}
namespace System.Diagnostics
{
    public static partial class ProcessLoggingExtensions
    {
        public static void StartAndCaptureOutAndErrToLogger(this System.Diagnostics.Process process, string prefix, Microsoft.Extensions.Logging.ILogger logger) { }
    }
}
