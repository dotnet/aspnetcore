// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Testing
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false)]
    [Xunit.Sdk.XunitTestCaseDiscovererAttribute("Microsoft.AspNetCore.Testing.ConditionalFactDiscoverer", "Microsoft.AspNetCore.Testing")]
    public partial class ConditionalFactAttribute : Xunit.FactAttribute
    {
        public ConditionalFactAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false)]
    [Xunit.Sdk.XunitTestCaseDiscovererAttribute("Microsoft.AspNetCore.Testing.ConditionalTheoryDiscoverer", "Microsoft.AspNetCore.Testing")]
    public partial class ConditionalTheoryAttribute : Xunit.TheoryAttribute
    {
        public ConditionalTheoryAttribute() { }
    }
    public partial class CultureReplacer : System.IDisposable
    {
        public CultureReplacer(System.Globalization.CultureInfo culture, System.Globalization.CultureInfo uiCulture) { }
        public CultureReplacer(string culture = "en-GB", string uiCulture = "en-US") { }
        public static System.Globalization.CultureInfo DefaultCulture { get { throw null; } }
        public static string DefaultCultureName { get { throw null; } }
        public static string DefaultUICultureName { get { throw null; } }
        public void Dispose() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, Inherited=true, AllowMultiple=false)]
    public sealed partial class DockerOnlyAttribute : System.Attribute, Microsoft.AspNetCore.Testing.ITestCondition
    {
        public DockerOnlyAttribute() { }
        public bool IsMet { get { throw null; } }
        public string SkipReason { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=true)]
    public partial class EnvironmentVariableSkipConditionAttribute : System.Attribute, Microsoft.AspNetCore.Testing.ITestCondition
    {
        public EnvironmentVariableSkipConditionAttribute(string variableName, params string[] values) { }
        public bool IsMet { get { throw null; } }
        public bool RunOnMatch { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SkipReason { get { throw null; } }
    }
    public static partial class ExceptionAssert
    {
        public static System.ArgumentException ThrowsArgument(System.Action testCode, string paramName, string exceptionMessage) { throw null; }
        public static System.Threading.Tasks.Task<System.ArgumentException> ThrowsArgumentAsync(System.Func<System.Threading.Tasks.Task> testCode, string paramName, string exceptionMessage) { throw null; }
        public static System.ArgumentNullException ThrowsArgumentNull(System.Action testCode, string paramName) { throw null; }
        public static System.ArgumentException ThrowsArgumentNullOrEmpty(System.Action testCode, string paramName) { throw null; }
        public static System.Threading.Tasks.Task<System.ArgumentException> ThrowsArgumentNullOrEmptyAsync(System.Func<System.Threading.Tasks.Task> testCode, string paramName) { throw null; }
        public static System.ArgumentException ThrowsArgumentNullOrEmptyString(System.Action testCode, string paramName) { throw null; }
        public static System.Threading.Tasks.Task<System.ArgumentException> ThrowsArgumentNullOrEmptyStringAsync(System.Func<System.Threading.Tasks.Task> testCode, string paramName) { throw null; }
        public static System.ArgumentOutOfRangeException ThrowsArgumentOutOfRange(System.Action testCode, string paramName, string exceptionMessage, object actualValue = null) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<TException> ThrowsAsync<TException>(System.Func<System.Threading.Tasks.Task> testCode, string exceptionMessage) where TException : System.Exception { throw null; }
        public static TException Throws<TException>(System.Action testCode) where TException : System.Exception { throw null; }
        public static TException Throws<TException>(System.Action testCode, string exceptionMessage) where TException : System.Exception { throw null; }
        public static TException Throws<TException>(System.Func<object> testCode, string exceptionMessage) where TException : System.Exception { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Method)]
    [Xunit.Sdk.TraitDiscovererAttribute("Microsoft.AspNetCore.Testing.FlakyTestDiscoverer", "Microsoft.AspNetCore.Testing")]
    public sealed partial class FlakyAttribute : System.Attribute, Xunit.Sdk.ITraitAttribute
    {
        public FlakyAttribute(string gitHubIssueUrl, string firstFilter, params string[] additionalFilters) { }
        public System.Collections.Generic.IReadOnlyList<string> Filters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string GitHubIssueUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public static partial class FlakyOn
    {
        public const string All = "All";
        public static partial class AzP
        {
            public const string All = "AzP:All";
            public const string Linux = "AzP:OS:Linux";
            public const string macOS = "AzP:OS:Darwin";
            public const string Windows = "AzP:OS:Windows_NT";
        }
        public static partial class Helix
        {
            public const string All = "Helix:Queue:All";
            public const string Centos7Amd64 = "Helix:Queue:Centos.7.Amd64.Open";
            public const string Debian8Amd64 = "Helix:Queue:Debian.8.Amd64.Open";
            public const string Debian9Amd64 = "Helix:Queue:Debian.9.Amd64.Open";
            public const string Fedora27Amd64 = "Helix:Queue:Fedora.27.Amd64.Open";
            public const string Fedora28Amd64 = "Helix:Queue:Fedora.28.Amd64.Open";
            public const string macOS1012Amd64 = "Helix:Queue:OSX.1012.Amd64.Open";
            public const string Redhat7Amd64 = "Helix:Queue:Redhat.7.Amd64.Open";
            public const string Ubuntu1604Amd64 = "Helix:Queue:Ubuntu.1604.Amd64.Open";
            public const string Ubuntu1810Amd64 = "Helix:Queue:Ubuntu.1810.Amd64.Open";
            public const string Windows10Amd64 = "Helix:Queue:Windows.10.Amd64.ClientRS4.VS2017.Open";
        }
    }
    public partial class FlakyTestDiscoverer : Xunit.Sdk.ITraitDiscoverer
    {
        public FlakyTestDiscoverer() { }
        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> GetTraits(Xunit.Abstractions.IAttributeInfo traitAttribute) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false)]
    public partial class FrameworkSkipConditionAttribute : System.Attribute, Microsoft.AspNetCore.Testing.ITestCondition
    {
        public FrameworkSkipConditionAttribute(Microsoft.AspNetCore.Testing.RuntimeFrameworks excludedFrameworks) { }
        public bool IsMet { get { throw null; } }
        public string SkipReason { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public static partial class HelixQueues
    {
        public const string Centos7Amd64 = "Centos.7.Amd64.Open";
        public const string Debian8Amd64 = "Debian.8.Amd64.Open";
        public const string Debian9Amd64 = "Debian.9.Amd64.Open";
        public const string Fedora27Amd64 = "Fedora.27.Amd64.Open";
        public const string Fedora28Amd64 = "Fedora.28.Amd64.Open";
        public const string macOS1012Amd64 = "OSX.1012.Amd64.Open";
        public const string Redhat7Amd64 = "Redhat.7.Amd64.Open";
        public const string Ubuntu1604Amd64 = "Ubuntu.1604.Amd64.Open";
        public const string Ubuntu1810Amd64 = "Ubuntu.1810.Amd64.Open";
        public const string Windows10Amd64 = "Windows.10.Amd64.ClientRS4.VS2017.Open";
    }
    public static partial class HttpClientSlim
    {
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<System.Net.Sockets.Socket> GetSocket(System.Uri requestUri) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<string> GetStringAsync(string requestUri, bool validateCertificate = true) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<string> GetStringAsync(System.Uri requestUri, bool validateCertificate = true) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<string> PostAsync(string requestUri, System.Net.Http.HttpContent content, bool validateCertificate = true) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<string> PostAsync(System.Uri requestUri, System.Net.Http.HttpContent content, bool validateCertificate = true) { throw null; }
    }
    public partial interface ITestCondition
    {
        bool IsMet { get; }
        string SkipReason { get; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=true)]
    public partial class MinimumOSVersionAttribute : System.Attribute, Microsoft.AspNetCore.Testing.ITestCondition
    {
        public MinimumOSVersionAttribute(Microsoft.AspNetCore.Testing.OperatingSystems operatingSystem, string minVersion) { }
        public bool IsMet { get { throw null; } }
        public string SkipReason { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    [System.FlagsAttribute]
    public enum OperatingSystems
    {
        Linux = 1,
        MacOSX = 2,
        Windows = 4,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=true)]
    public partial class OSSkipConditionAttribute : System.Attribute, Microsoft.AspNetCore.Testing.ITestCondition
    {
        public OSSkipConditionAttribute(Microsoft.AspNetCore.Testing.OperatingSystems operatingSystem, params string[] versions) { }
        public bool IsMet { get { throw null; } }
        public string SkipReason { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method)]
    public partial class ReplaceCultureAttribute : Xunit.Sdk.BeforeAfterTestAttribute
    {
        public ReplaceCultureAttribute() { }
        public ReplaceCultureAttribute(string currentCulture, string currentUICulture) { }
        public System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Globalization.CultureInfo UICulture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override void After(System.Reflection.MethodInfo methodUnderTest) { }
        public override void Before(System.Reflection.MethodInfo methodUnderTest) { }
    }
    [System.FlagsAttribute]
    public enum RuntimeFrameworks
    {
        None = 0,
        Mono = 1,
        CLR = 2,
        CoreCLR = 4,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false)]
    public partial class SkipOnCIAttribute : System.Attribute, Microsoft.AspNetCore.Testing.ITestCondition
    {
        public SkipOnCIAttribute(string issueUrl = "") { }
        public bool IsMet { get { throw null; } }
        public string IssueUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string SkipReason { get { throw null; } }
        public static string GetIfOnAzdo() { throw null; }
        public static string GetTargetHelixQueue() { throw null; }
        public static bool OnAzdo() { throw null; }
        public static bool OnCI() { throw null; }
        public static bool OnHelix() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple=false)]
    public partial class SkipOnHelixAttribute : System.Attribute, Microsoft.AspNetCore.Testing.ITestCondition
    {
        public SkipOnHelixAttribute(string issueUrl) { }
        public bool IsMet { get { throw null; } }
        public string IssueUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Queues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SkipReason { get { throw null; } }
        public static string GetTargetHelixQueue() { throw null; }
        public static bool OnHelix() { throw null; }
    }
    public partial class SkippedTestCase : Xunit.Sdk.XunitTestCase
    {
        [System.ObsoleteAttribute("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public SkippedTestCase() { }
        public SkippedTestCase(string skipReason, Xunit.Abstractions.IMessageSink diagnosticMessageSink, Xunit.Sdk.TestMethodDisplay defaultMethodDisplay, Xunit.Sdk.TestMethodDisplayOptions defaultMethodDisplayOptions, Xunit.Abstractions.ITestMethod testMethod, object[] testMethodArguments = null) { }
        public override void Deserialize(Xunit.Abstractions.IXunitSerializationInfo data) { }
        protected override string GetSkipReason(Xunit.Abstractions.IAttributeInfo factAttribute) { throw null; }
        public override void Serialize(Xunit.Abstractions.IXunitSerializationInfo data) { }
    }
    public static partial class TaskExtensions
    {
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task TimeoutAfter(this System.Threading.Tasks.Task task, System.TimeSpan timeout, [System.Runtime.CompilerServices.CallerFilePathAttribute]string filePath = null, [System.Runtime.CompilerServices.CallerLineNumberAttribute]int lineNumber = 0) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task<T> TimeoutAfter<T>(this System.Threading.Tasks.Task<T> task, System.TimeSpan timeout, [System.Runtime.CompilerServices.CallerFilePathAttribute]string filePath = null, [System.Runtime.CompilerServices.CallerLineNumberAttribute]int lineNumber = 0) { throw null; }
    }
    public static partial class TestMethodExtensions
    {
        public static string EvaluateSkipConditions(this Xunit.Abstractions.ITestMethod testMethod) { throw null; }
    }
    [System.ObsoleteAttribute("This API is obsolete and the pattern its usage encouraged should not be used anymore. See https://github.com/aspnet/Extensions/issues/1697 for details.")]
    public partial class TestPathUtilities
    {
        public TestPathUtilities() { }
        public static string GetRepoRootDirectory() { throw null; }
        public static string GetSolutionRootDirectory(string solution) { throw null; }
    }
    public static partial class TestPlatformHelper
    {
        public static bool IsLinux { get { throw null; } }
        public static bool IsMac { get { throw null; } }
        public static bool IsMono { get { throw null; } }
        public static bool IsWindows { get { throw null; } }
    }
    public static partial class WindowsVersions
    {
        public const string Win10 = "10.0";
        public const string Win2008R2 = "6.1";
        public const string Win7 = "6.1";
        public const string Win8 = "6.2";
        public const string Win81 = "6.3";
    }
}
namespace Microsoft.AspNetCore.Testing.Tracing
{
    public partial class CollectingEventListener : System.Diagnostics.Tracing.EventListener
    {
        public CollectingEventListener() { }
        public void CollectFrom(System.Diagnostics.Tracing.EventSource eventSource) { }
        public void CollectFrom(string eventSourceName) { }
        public System.Collections.Generic.IReadOnlyList<System.Diagnostics.Tracing.EventWrittenEventArgs> GetEventsWritten() { throw null; }
        protected override void OnEventSourceCreated(System.Diagnostics.Tracing.EventSource eventSource) { }
        protected override void OnEventWritten(System.Diagnostics.Tracing.EventWrittenEventArgs eventData) { }
    }
    public partial class EventAssert
    {
        public EventAssert(int expectedId, string expectedName, System.Diagnostics.Tracing.EventLevel expectedLevel) { }
        public static void Collection(System.Collections.Generic.IEnumerable<System.Diagnostics.Tracing.EventWrittenEventArgs> events, params Microsoft.AspNetCore.Testing.Tracing.EventAssert[] asserts) { }
        public static Microsoft.AspNetCore.Testing.Tracing.EventAssert Event(int id, string name, System.Diagnostics.Tracing.EventLevel level) { throw null; }
        public Microsoft.AspNetCore.Testing.Tracing.EventAssert Payload(string name, System.Action<object> asserter) { throw null; }
        public Microsoft.AspNetCore.Testing.Tracing.EventAssert Payload(string name, object expectedValue) { throw null; }
    }
    [Xunit.CollectionAttribute("Microsoft.AspNetCore.Testing.Tracing.EventSourceTestCollection")]
    public abstract partial class EventSourceTestBase : System.IDisposable
    {
        public const string CollectionName = "Microsoft.AspNetCore.Testing.Tracing.EventSourceTestCollection";
        public EventSourceTestBase() { }
        protected void CollectFrom(System.Diagnostics.Tracing.EventSource eventSource) { }
        protected void CollectFrom(string eventSourceName) { }
        public void Dispose() { }
        protected System.Collections.Generic.IReadOnlyList<System.Diagnostics.Tracing.EventWrittenEventArgs> GetEvents() { throw null; }
    }
}
