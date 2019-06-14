# Microsoft.AspNetCore.Connections

``` diff
 namespace Microsoft.AspNetCore.Connections {
-    public abstract class ConnectionContext {
+    public abstract class ConnectionContext : IAsyncDisposable {
+        public virtual CancellationToken ConnectionClosed { get; set; }
+        public virtual EndPoint LocalEndPoint { get; set; }
+        public virtual EndPoint RemoteEndPoint { get; set; }
+        public virtual ValueTask DisposeAsync();
     }
-    public class DefaultConnectionContext : ConnectionContext, IConnectionIdFeature, IConnectionItemsFeature, IConnectionLifetimeFeature, IConnectionTransportFeature, IConnectionUserFeature, IDisposable {
+    public class DefaultConnectionContext : ConnectionContext, IConnectionEndPointFeature, IConnectionIdFeature, IConnectionItemsFeature, IConnectionLifetimeFeature, IConnectionTransportFeature, IConnectionUserFeature {
-        public CancellationToken ConnectionClosed { get; set; }
+        public override CancellationToken ConnectionClosed { get; set; }
+        public override EndPoint LocalEndPoint { get; set; }
+        public override EndPoint RemoteEndPoint { get; set; }
-        public void Dispose();

+        public override ValueTask DisposeAsync();
     }
+    public class FileHandleEndPoint : EndPoint {
+        public FileHandleEndPoint(ulong fileHandle, FileHandleType fileHandleType);
+        public ulong FileHandle { get; }
+        public FileHandleType FileHandleType { get; }
+    }
+    public enum FileHandleType {
+        Auto = 0,
+        Pipe = 2,
+        Tcp = 1,
+    }
+    public interface IConnectionListener : IAsyncDisposable {
+        EndPoint EndPoint { get; }
+        ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default(CancellationToken));
+        ValueTask UnbindAsync(CancellationToken cancellationToken = default(CancellationToken));
+    }
+    public interface IConnectionListenerFactory {
+        ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default(CancellationToken));
+    }
+    public abstract class TransportConnection : ConnectionContext, IConnectionIdFeature, IConnectionItemsFeature, IConnectionLifetimeFeature, IConnectionTransportFeature, IEnumerable, IEnumerable<KeyValuePair<Type, object>>, IFeatureCollection, IMemoryPoolFeature {
+        public TransportConnection();
+        public IDuplexPipe Application { get; set; }
+        public override CancellationToken ConnectionClosed { get; set; }
+        public override string ConnectionId { get; set; }
+        public override IFeatureCollection Features { get; }
+        public override IDictionary<object, object> Items { get; set; }
+        public override EndPoint LocalEndPoint { get; set; }
+        public virtual MemoryPool<byte> MemoryPool { get; }
+        IDictionary<object, object> Microsoft.AspNetCore.Connections.Features.IConnectionItemsFeature.Items { get; set; }
+        CancellationToken Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeFeature.ConnectionClosed { get; set; }
+        IDuplexPipe Microsoft.AspNetCore.Connections.Features.IConnectionTransportFeature.Transport { get; set; }
+        MemoryPool<byte> Microsoft.AspNetCore.Connections.Features.IMemoryPoolFeature.MemoryPool { get; }
+        bool Microsoft.AspNetCore.Http.Features.IFeatureCollection.IsReadOnly { get; }
+        object Microsoft.AspNetCore.Http.Features.IFeatureCollection.this[Type key] { get; set; }
+        int Microsoft.AspNetCore.Http.Features.IFeatureCollection.Revision { get; }
+        public override EndPoint RemoteEndPoint { get; set; }
+        public override IDuplexPipe Transport { get; set; }
+        public override void Abort(ConnectionAbortedException abortReason);
+        void Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeFeature.Abort();
+        TFeature Microsoft.AspNetCore.Http.Features.IFeatureCollection.Get<TFeature>();
+        void Microsoft.AspNetCore.Http.Features.IFeatureCollection.Set<TFeature>(TFeature feature);
+        IEnumerator<KeyValuePair<Type, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Type,System.Object>>.GetEnumerator();
+        IEnumerator System.Collections.IEnumerable.GetEnumerator();
+    }
 }
```

