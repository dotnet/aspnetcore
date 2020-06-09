// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.JSInterop
{
    public static partial class DotNetObjectReference
    {
        public static Microsoft.JSInterop.DotNetObjectReference<TValue> Create<TValue>(TValue value) where TValue : class { throw null; }
    }
    public sealed partial class DotNetObjectReference<TValue> : System.IDisposable where TValue : class
    {
        internal DotNetObjectReference() { }
        public TValue Value { get { throw null; } }
        public void Dispose() { }
    }
    public partial interface IJSInProcessRuntime : Microsoft.JSInterop.IJSRuntime
    {
        T Invoke<T>(string identifier, params object[] args);
    }
    public partial interface IJSRuntime
    {
        System.Threading.Tasks.ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args);
        System.Threading.Tasks.ValueTask<TValue> InvokeAsync<TValue>(string identifier, System.Threading.CancellationToken cancellationToken, object[] args);
    }
    public partial class JSException : System.Exception
    {
        public JSException(string message) { }
        public JSException(string message, System.Exception innerException) { }
    }
    public abstract partial class JSInProcessRuntime : Microsoft.JSInterop.JSRuntime, Microsoft.JSInterop.IJSInProcessRuntime, Microsoft.JSInterop.IJSRuntime
    {
        protected JSInProcessRuntime() { }
        protected abstract string InvokeJS(string identifier, string argsJson);
        public TValue Invoke<TValue>(string identifier, params object[] args) { throw null; }
    }
    public static partial class JSInProcessRuntimeExtensions
    {
        public static void InvokeVoid(this Microsoft.JSInterop.IJSInProcessRuntime jsRuntime, string identifier, params object[] args) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=true)]
    public sealed partial class JSInvokableAttribute : System.Attribute
    {
        public JSInvokableAttribute() { }
        public JSInvokableAttribute(string identifier) { }
        public string Identifier { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class JSRuntime : Microsoft.JSInterop.IJSRuntime
    {
        protected JSRuntime() { }
        protected System.TimeSpan? DefaultAsyncTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        protected internal System.Text.Json.JsonSerializerOptions JsonSerializerOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected abstract void BeginInvokeJS(long taskId, string identifier, string argsJson);
        protected internal abstract void EndInvokeDotNet(Microsoft.JSInterop.Infrastructure.DotNetInvocationInfo invocationInfo, in Microsoft.JSInterop.Infrastructure.DotNetInvocationResult invocationResult);
        public System.Threading.Tasks.ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) { throw null; }
        public System.Threading.Tasks.ValueTask<TValue> InvokeAsync<TValue>(string identifier, System.Threading.CancellationToken cancellationToken, object[] args) { throw null; }
    }
    public static partial class JSRuntimeExtensions
    {
        public static System.Threading.Tasks.ValueTask<TValue> InvokeAsync<TValue>(this Microsoft.JSInterop.IJSRuntime jsRuntime, string identifier, params object[] args) { throw null; }
        public static System.Threading.Tasks.ValueTask<TValue> InvokeAsync<TValue>(this Microsoft.JSInterop.IJSRuntime jsRuntime, string identifier, System.Threading.CancellationToken cancellationToken, params object[] args) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.ValueTask<TValue> InvokeAsync<TValue>(this Microsoft.JSInterop.IJSRuntime jsRuntime, string identifier, System.TimeSpan timeout, params object[] args) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.ValueTask InvokeVoidAsync(this Microsoft.JSInterop.IJSRuntime jsRuntime, string identifier, params object[] args) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.ValueTask InvokeVoidAsync(this Microsoft.JSInterop.IJSRuntime jsRuntime, string identifier, System.Threading.CancellationToken cancellationToken, params object[] args) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.ValueTask InvokeVoidAsync(this Microsoft.JSInterop.IJSRuntime jsRuntime, string identifier, System.TimeSpan timeout, params object[] args) { throw null; }
    }
}
namespace Microsoft.JSInterop.Infrastructure
{
    public static partial class DotNetDispatcher
    {
        public static void BeginInvokeDotNet(Microsoft.JSInterop.JSRuntime jsRuntime, Microsoft.JSInterop.Infrastructure.DotNetInvocationInfo invocationInfo, string argsJson) { }
        public static void EndInvokeJS(Microsoft.JSInterop.JSRuntime jsRuntime, string arguments) { }
        public static string Invoke(Microsoft.JSInterop.JSRuntime jsRuntime, in Microsoft.JSInterop.Infrastructure.DotNetInvocationInfo invocationInfo, string argsJson) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct DotNetInvocationInfo
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public DotNetInvocationInfo(string assemblyName, string methodIdentifier, long dotNetObjectId, string callId) { throw null; }
        public string AssemblyName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string CallId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public long DotNetObjectId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string MethodIdentifier { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct DotNetInvocationResult
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public DotNetInvocationResult(System.Exception exception, string errorKind) { throw null; }
        public DotNetInvocationResult(object result) { throw null; }
        public string ErrorKind { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Exception Exception { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public object Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Success { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
