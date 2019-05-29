// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.JSInterop
{
    public static partial class DotNetDispatcher
    {
        public static void BeginInvoke(string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson) { }
        [Microsoft.JSInterop.JSInvokableAttribute("DotNetDispatcher.EndInvoke")]
        public static void EndInvoke(long asyncHandle, bool succeeded, Microsoft.JSInterop.Internal.JSAsyncCallResult result) { }
        public static string Invoke(string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson) { throw null; }
        [Microsoft.JSInterop.JSInvokableAttribute("DotNetDispatcher.ReleaseDotNetObject")]
        public static void ReleaseDotNetObject(long dotNetObjectId) { }
    }
    public static partial class DotNetObjectRef
    {
        public static Microsoft.JSInterop.DotNetObjectRef<TValue> Create<TValue>(TValue value) where TValue : class { throw null; }
    }
    public sealed partial class DotNetObjectRef<TValue> : System.IDisposable where TValue : class
    {
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public DotNetObjectRef() { }
        [System.Text.Json.Serialization.JsonIgnoreAttribute]
        public TValue Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public long __dotNetObject { get { throw null; } set { } }
        public void Dispose() { }
    }
    public partial interface IJSInProcessRuntime : Microsoft.JSInterop.IJSRuntime
    {
        T Invoke<T>(string identifier, params object[] args);
    }
    public partial interface IJSRuntime
    {
        System.Threading.Tasks.Task<TValue> InvokeAsync<TValue>(string identifier, params object[] args);
    }
    public partial class JSException : System.Exception
    {
        public JSException(string message) { }
    }
    public abstract partial class JSInProcessRuntimeBase : Microsoft.JSInterop.JSRuntimeBase, Microsoft.JSInterop.IJSInProcessRuntime, Microsoft.JSInterop.IJSRuntime
    {
        protected JSInProcessRuntimeBase() { }
        protected abstract string InvokeJS(string identifier, string argsJson);
        public TValue Invoke<TValue>(string identifier, params object[] args) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=true)]
    public partial class JSInvokableAttribute : System.Attribute
    {
        public JSInvokableAttribute() { }
        public JSInvokableAttribute(string identifier) { }
        public string Identifier { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public static partial class JSRuntime
    {
        public static void SetCurrentJSRuntime(Microsoft.JSInterop.IJSRuntime instance) { }
    }
    public abstract partial class JSRuntimeBase : Microsoft.JSInterop.IJSRuntime
    {
        protected JSRuntimeBase() { }
        protected abstract void BeginInvokeJS(long asyncHandle, string identifier, string argsJson);
        public System.Threading.Tasks.Task<T> InvokeAsync<T>(string identifier, params object[] args) { throw null; }
    }
}
namespace Microsoft.JSInterop.Internal
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class JSAsyncCallResult
    {
        internal JSAsyncCallResult() { }
    }
}
