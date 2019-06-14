# Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal

``` diff
-namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal {
 {
-    public enum FileHandleType {
 {
-        Auto = 0,

-        Pipe = 2,

-        Tcp = 1,

-    }
-    public interface IApplicationTransportFeature {
 {
-        IDuplexPipe Application { get; set; }

-    }
-    public interface IConnectionDispatcher {
 {
-        Task OnConnection(TransportConnection connection);

-    }
-    public interface IEndPointInformation {
 {
-        ulong FileHandle { get; }

-        FileHandleType HandleType { get; set; }

-        IPEndPoint IPEndPoint { get; set; }

-        bool NoDelay { get; }

-        string SocketPath { get; }

-        ListenType Type { get; }

-    }
-    public interface ITransport {
 {
-        Task BindAsync();

-        Task StopAsync();

-        Task UnbindAsync();

-    }
-    public interface ITransportFactory {
 {
-        ITransport Create(IEndPointInformation endPointInformation, IConnectionDispatcher dispatcher);

-    }
-    public interface ITransportSchedulerFeature {
 {
-        PipeScheduler InputWriterScheduler { get; }

-        PipeScheduler OutputReaderScheduler { get; }

-    }
-    public static class KestrelMemoryPool {
 {
-        public static readonly int MinimumSegmentSize;

-        public static MemoryPool<byte> Create();

-        public static MemoryPool<byte> CreateSlabMemoryPool();

-    }
-    public enum ListenType {
 {
-        FileHandle = 2,

-        IPEndPoint = 0,

-        SocketPath = 1,

-    }
-    public enum SchedulingMode {
 {
-        Default = 0,

-        Inline = 2,

-        ThreadPool = 1,

-    }
-    public abstract class TransportConnection : ConnectionContext, IApplicationTransportFeature, IConnectionHeartbeatFeature, IConnectionIdFeature, IConnectionItemsFeature, IConnectionLifetimeFeature, IConnectionLifetimeNotificationFeature, IConnectionTransportFeature, IEnumerable, IEnumerable<KeyValuePair<Type, object>>, IFeatureCollection, IHttpConnectionFeature, IMemoryPoolFeature, ITransportSchedulerFeature {
 {
-        protected readonly CancellationTokenSource _connectionClosingCts;

-        public TransportConnection();

-        public IDuplexPipe Application { get; set; }

-        public CancellationToken ConnectionClosed { get; set; }

-        public CancellationToken ConnectionClosedRequested { get; set; }

-        public override string ConnectionId { get; set; }

-        public override IFeatureCollection Features { get; }

-        public PipeWriter Input { get; }

-        public virtual PipeScheduler InputWriterScheduler { get; }

-        public override IDictionary<object, object> Items { get; set; }

-        public IPAddress LocalAddress { get; set; }

-        public int LocalPort { get; set; }

-        public virtual MemoryPool<byte> MemoryPool { get; }

-        IDictionary<object, object> Microsoft.AspNetCore.Connections.Features.IConnectionItemsFeature.Items { get; set; }

-        CancellationToken Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeFeature.ConnectionClosed { get; set; }

-        CancellationToken Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeNotificationFeature.ConnectionClosedRequested { get; set; }

-        IDuplexPipe Microsoft.AspNetCore.Connections.Features.IConnectionTransportFeature.Transport { get; set; }

-        MemoryPool<byte> Microsoft.AspNetCore.Connections.Features.IMemoryPoolFeature.MemoryPool { get; }

-        bool Microsoft.AspNetCore.Http.Features.IFeatureCollection.IsReadOnly { get; }

-        object Microsoft.AspNetCore.Http.Features.IFeatureCollection.this[Type key] { get; set; }

-        int Microsoft.AspNetCore.Http.Features.IFeatureCollection.Revision { get; }

-        string Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.ConnectionId { get; set; }

-        IPAddress Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.LocalIpAddress { get; set; }

-        int Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.LocalPort { get; set; }

-        IPAddress Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.RemoteIpAddress { get; set; }

-        int Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.RemotePort { get; set; }

-        IDuplexPipe Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.IApplicationTransportFeature.Application { get; set; }

-        PipeScheduler Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.ITransportSchedulerFeature.InputWriterScheduler { get; }

-        PipeScheduler Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal.ITransportSchedulerFeature.OutputReaderScheduler { get; }

-        public PipeReader Output { get; }

-        public virtual PipeScheduler OutputReaderScheduler { get; }

-        public IPAddress RemoteAddress { get; set; }

-        public int RemotePort { get; set; }

-        public override IDuplexPipe Transport { get; set; }

-        public override void Abort(ConnectionAbortedException abortReason);

-        void Microsoft.AspNetCore.Connections.Features.IConnectionHeartbeatFeature.OnHeartbeat(Action<object> action, object state);

-        void Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeFeature.Abort();

-        void Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeNotificationFeature.RequestClose();

-        TFeature Microsoft.AspNetCore.Http.Features.IFeatureCollection.Get<TFeature>();

-        void Microsoft.AspNetCore.Http.Features.IFeatureCollection.Set<TFeature>(TFeature feature);

-        public void OnHeartbeat(Action<object> action, object state);

-        public void RequestClose();

-        IEnumerator<KeyValuePair<Type, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Type,System.Object>>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public void TickHeartbeat();

-    }
-}
```

