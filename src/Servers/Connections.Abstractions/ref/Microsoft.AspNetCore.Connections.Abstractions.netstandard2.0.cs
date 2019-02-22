// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Connections
{
    public partial class AddressInUseException : System.InvalidOperationException
    {
        public AddressInUseException(string message) { }
        public AddressInUseException(string message, System.Exception inner) { }
    }
    public partial class ConnectionAbortedException : System.OperationCanceledException
    {
        public ConnectionAbortedException() { }
        public ConnectionAbortedException(string message) { }
        public ConnectionAbortedException(string message, System.Exception inner) { }
    }
    public partial class ConnectionBuilder : Microsoft.AspNetCore.Connections.IConnectionBuilder
    {
        public ConnectionBuilder(System.IServiceProvider applicationServices) { }
        public System.IServiceProvider ApplicationServices { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Connections.ConnectionDelegate Build() { throw null; }
        public Microsoft.AspNetCore.Connections.IConnectionBuilder Use(System.Func<Microsoft.AspNetCore.Connections.ConnectionDelegate, Microsoft.AspNetCore.Connections.ConnectionDelegate> middleware) { throw null; }
    }
    public static partial class ConnectionBuilderExtensions
    {
        public static Microsoft.AspNetCore.Connections.IConnectionBuilder Run(this Microsoft.AspNetCore.Connections.IConnectionBuilder connectionBuilder, System.Func<Microsoft.AspNetCore.Connections.ConnectionContext, System.Threading.Tasks.Task> middleware) { throw null; }
        public static Microsoft.AspNetCore.Connections.IConnectionBuilder Use(this Microsoft.AspNetCore.Connections.IConnectionBuilder connectionBuilder, System.Func<Microsoft.AspNetCore.Connections.ConnectionContext, System.Func<System.Threading.Tasks.Task>, System.Threading.Tasks.Task> middleware) { throw null; }
        public static Microsoft.AspNetCore.Connections.IConnectionBuilder UseConnectionHandler<TConnectionHandler>(this Microsoft.AspNetCore.Connections.IConnectionBuilder connectionBuilder) where TConnectionHandler : Microsoft.AspNetCore.Connections.ConnectionHandler { throw null; }
    }
    public abstract partial class ConnectionContext
    {
        protected ConnectionContext() { }
        public abstract string ConnectionId { get; set; }
        public abstract Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { get; }
        public abstract System.Collections.Generic.IDictionary<object, object> Items { get; set; }
        public abstract System.IO.Pipelines.IDuplexPipe Transport { get; set; }
        public virtual void Abort() { }
        public virtual void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason) { }
    }
    public delegate System.Threading.Tasks.Task ConnectionDelegate(Microsoft.AspNetCore.Connections.ConnectionContext connection);
    public abstract partial class ConnectionHandler
    {
        protected ConnectionHandler() { }
        public abstract System.Threading.Tasks.Task OnConnectedAsync(Microsoft.AspNetCore.Connections.ConnectionContext connection);
    }
    public partial class ConnectionItems : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object, object>>, System.Collections.Generic.IDictionary<object, object>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object, object>>, System.Collections.IEnumerable
    {
        public ConnectionItems() { }
        public ConnectionItems(System.Collections.Generic.IDictionary<object, object> items) { }
        public System.Collections.Generic.IDictionary<object, object> Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Count { get { throw null; } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.IsReadOnly { get { throw null; } }
        object System.Collections.Generic.IDictionary<System.Object,System.Object>.this[object key] { get { throw null; } set { } }
        System.Collections.Generic.ICollection<object> System.Collections.Generic.IDictionary<System.Object,System.Object>.Keys { get { throw null; } }
        System.Collections.Generic.ICollection<object> System.Collections.Generic.IDictionary<System.Object,System.Object>.Values { get { throw null; } }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Add(System.Collections.Generic.KeyValuePair<object, object> item) { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Clear() { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Contains(System.Collections.Generic.KeyValuePair<object, object> item) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.CopyTo(System.Collections.Generic.KeyValuePair<object, object>[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Remove(System.Collections.Generic.KeyValuePair<object, object> item) { throw null; }
        void System.Collections.Generic.IDictionary<System.Object,System.Object>.Add(object key, object value) { }
        bool System.Collections.Generic.IDictionary<System.Object,System.Object>.ContainsKey(object key) { throw null; }
        bool System.Collections.Generic.IDictionary<System.Object,System.Object>.Remove(object key) { throw null; }
        bool System.Collections.Generic.IDictionary<System.Object,System.Object>.TryGetValue(object key, out object value) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class ConnectionResetException : System.IO.IOException
    {
        public ConnectionResetException(string message) { }
        public ConnectionResetException(string message, System.Exception inner) { }
    }
    public partial class DefaultConnectionContext : Microsoft.AspNetCore.Connections.ConnectionContext, Microsoft.AspNetCore.Connections.Features.IConnectionIdFeature, Microsoft.AspNetCore.Connections.Features.IConnectionItemsFeature, Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeFeature, Microsoft.AspNetCore.Connections.Features.IConnectionTransportFeature, Microsoft.AspNetCore.Connections.Features.IConnectionUserFeature, System.IDisposable
    {
        public DefaultConnectionContext() { }
        public DefaultConnectionContext(string id) { }
        public DefaultConnectionContext(string id, System.IO.Pipelines.IDuplexPipe transport, System.IO.Pipelines.IDuplexPipe application) { }
        public System.IO.Pipelines.IDuplexPipe Application { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Threading.CancellationToken ConnectionClosed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override string ConnectionId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override System.Collections.Generic.IDictionary<object, object> Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override System.IO.Pipelines.IDuplexPipe Transport { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Security.Claims.ClaimsPrincipal User { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason) { }
        public void Dispose() { }
    }
    public partial interface IConnectionBuilder
    {
        System.IServiceProvider ApplicationServices { get; }
        Microsoft.AspNetCore.Connections.ConnectionDelegate Build();
        Microsoft.AspNetCore.Connections.IConnectionBuilder Use(System.Func<Microsoft.AspNetCore.Connections.ConnectionDelegate, Microsoft.AspNetCore.Connections.ConnectionDelegate> middleware);
    }
    [System.FlagsAttribute]
    public enum TransferFormat
    {
        Binary = 1,
        Text = 2,
    }
}
namespace Microsoft.AspNetCore.Connections.Features
{
    public partial interface IConnectionHeartbeatFeature
    {
        void OnHeartbeat(System.Action<object> action, object state);
    }
    public partial interface IConnectionIdFeature
    {
        string ConnectionId { get; set; }
    }
    public partial interface IConnectionInherentKeepAliveFeature
    {
        bool HasInherentKeepAlive { get; }
    }
    public partial interface IConnectionItemsFeature
    {
        System.Collections.Generic.IDictionary<object, object> Items { get; set; }
    }
    public partial interface IConnectionLifetimeFeature
    {
        System.Threading.CancellationToken ConnectionClosed { get; set; }
        void Abort();
    }
    public partial interface IConnectionLifetimeNotificationFeature
    {
        System.Threading.CancellationToken ConnectionClosedRequested { get; set; }
        void RequestClose();
    }
    public partial interface IConnectionTransportFeature
    {
        System.IO.Pipelines.IDuplexPipe Transport { get; set; }
    }
    public partial interface IConnectionUserFeature
    {
        System.Security.Claims.ClaimsPrincipal User { get; set; }
    }
    public partial interface IMemoryPoolFeature
    {
        System.Buffers.MemoryPool<byte> MemoryPool { get; }
    }
    public partial interface ITlsHandshakeFeature
    {
        System.Security.Authentication.CipherAlgorithmType CipherAlgorithm { get; }
        int CipherStrength { get; }
        System.Security.Authentication.HashAlgorithmType HashAlgorithm { get; }
        int HashStrength { get; }
        System.Security.Authentication.ExchangeAlgorithmType KeyExchangeAlgorithm { get; }
        int KeyExchangeStrength { get; }
        System.Security.Authentication.SslProtocols Protocol { get; }
    }
    public partial interface ITransferFormatFeature
    {
        Microsoft.AspNetCore.Connections.TransferFormat ActiveFormat { get; set; }
        Microsoft.AspNetCore.Connections.TransferFormat SupportedFormats { get; }
    }
}
