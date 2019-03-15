// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal
{
    public enum FileHandleType
    {
        Auto = 0,
        Pipe = 2,
        Tcp = 1,
    }
    public partial interface IApplicationTransportFeature
    {
        System.IO.Pipelines.IDuplexPipe Application { get; set; }
    }
    public partial interface IConnectionDispatcher
    {
        System.Threading.Tasks.Task OnConnection(Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.TransportConnection connection);
    }
    public partial interface IEndPointInformation
    {
        ulong FileHandle { get; }
        Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.FileHandleType HandleType { get; set; }
        System.Net.IPEndPoint IPEndPoint { get; set; }
        bool NoDelay { get; }
        string SocketPath { get; }
        Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.ListenType Type { get; }
    }
    public partial interface ITransport
    {
        System.Threading.Tasks.Task BindAsync();
        System.Threading.Tasks.Task StopAsync();
        System.Threading.Tasks.Task UnbindAsync();
    }
    public partial interface ITransportFactory
    {
        Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.ITransport Create(Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IEndPointInformation endPointInformation, Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IConnectionDispatcher dispatcher);
    }
    public partial interface ITransportSchedulerFeature
    {
        System.IO.Pipelines.PipeScheduler InputWriterScheduler { get; }
        System.IO.Pipelines.PipeScheduler OutputReaderScheduler { get; }
    }
    public static partial class KestrelMemoryPool
    {
        public static readonly int MinimumSegmentSize;
        public static System.Buffers.MemoryPool<byte> Create() { throw null; }
        public static System.Buffers.MemoryPool<byte> CreateSlabMemoryPool() { throw null; }
    }
    public enum ListenType
    {
        FileHandle = 2,
        IPEndPoint = 0,
        SocketPath = 1,
    }
    public enum SchedulingMode
    {
        Default = 0,
        Inline = 2,
        ThreadPool = 1,
    }
    public abstract partial class TransportConnection : Microsoft.AspNetCore.Connections.ConnectionContext, Microsoft.AspNetCore.Connections.Features.IConnectionHeartbeatFeature, Microsoft.AspNetCore.Connections.Features.IConnectionIdFeature, Microsoft.AspNetCore.Connections.Features.IConnectionItemsFeature, Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeFeature, Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeNotificationFeature, Microsoft.AspNetCore.Connections.Features.IConnectionTransportFeature, Microsoft.AspNetCore.Connections.Features.IMemoryPoolFeature, Microsoft.AspNetCore.Http.Features.IFeatureCollection, Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature, Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IApplicationTransportFeature, Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.ITransportSchedulerFeature, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Type, object>>, System.Collections.IEnumerable
    {
        protected readonly System.Threading.CancellationTokenSource _connectionClosingCts;
        public TransportConnection() { }
        public System.IO.Pipelines.IDuplexPipe Application { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Threading.CancellationToken ConnectionClosed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Threading.CancellationToken ConnectionClosedRequested { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override string ConnectionId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { get { throw null; } }
        public System.IO.Pipelines.PipeWriter Input { get { throw null; } }
        public virtual System.IO.Pipelines.PipeScheduler InputWriterScheduler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override System.Collections.Generic.IDictionary<object, object> Items { get { throw null; } set { } }
        public System.Net.IPAddress LocalAddress { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int LocalPort { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual System.Buffers.MemoryPool<byte> MemoryPool { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        System.Collections.Generic.IDictionary<object, object> Microsoft.AspNetCore.Connections.Features.IConnectionItemsFeature.Items { get { throw null; } set { } }
        System.Threading.CancellationToken Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeFeature.ConnectionClosed { get { throw null; } set { } }
        System.Threading.CancellationToken Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeNotificationFeature.ConnectionClosedRequested { get { throw null; } set { } }
        System.IO.Pipelines.IDuplexPipe Microsoft.AspNetCore.Connections.Features.IConnectionTransportFeature.Transport { get { throw null; } set { } }
        System.Buffers.MemoryPool<byte> Microsoft.AspNetCore.Connections.Features.IMemoryPoolFeature.MemoryPool { get { throw null; } }
        bool Microsoft.AspNetCore.Http.Features.IFeatureCollection.IsReadOnly { get { throw null; } }
        object Microsoft.AspNetCore.Http.Features.IFeatureCollection.this[System.Type key] { get { throw null; } set { } }
        int Microsoft.AspNetCore.Http.Features.IFeatureCollection.Revision { get { throw null; } }
        string Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.ConnectionId { get { throw null; } set { } }
        System.Net.IPAddress Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.LocalIpAddress { get { throw null; } set { } }
        int Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.LocalPort { get { throw null; } set { } }
        System.Net.IPAddress Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.RemoteIpAddress { get { throw null; } set { } }
        int Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.RemotePort { get { throw null; } set { } }
        System.IO.Pipelines.IDuplexPipe Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IApplicationTransportFeature.Application { get { throw null; } set { } }
        System.IO.Pipelines.PipeScheduler Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.ITransportSchedulerFeature.InputWriterScheduler { get { throw null; } }
        System.IO.Pipelines.PipeScheduler Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.ITransportSchedulerFeature.OutputReaderScheduler { get { throw null; } }
        public System.IO.Pipelines.PipeReader Output { get { throw null; } }
        public virtual System.IO.Pipelines.PipeScheduler OutputReaderScheduler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Net.IPAddress RemoteAddress { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int RemotePort { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override System.IO.Pipelines.IDuplexPipe Transport { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason) { }
        void Microsoft.AspNetCore.Connections.Features.IConnectionHeartbeatFeature.OnHeartbeat(System.Action<object> action, object state) { }
        void Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeFeature.Abort() { }
        void Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeNotificationFeature.RequestClose() { }
        TFeature Microsoft.AspNetCore.Http.Features.IFeatureCollection.Get<TFeature>() { throw null; }
        void Microsoft.AspNetCore.Http.Features.IFeatureCollection.Set<TFeature>(TFeature feature) { }
        public void OnHeartbeat(System.Action<object> action, object state) { }
        public void RequestClose() { }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<System.Type, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Type,System.Object>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public void TickHeartbeat() { }
    }
}
