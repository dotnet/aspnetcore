# Microsoft.AspNetCore.Connections

``` diff
 namespace Microsoft.AspNetCore.Connections {
     public class AddressInUseException : InvalidOperationException {
         public AddressInUseException(string message);
         public AddressInUseException(string message, Exception inner);
     }
     public class ConnectionAbortedException : OperationCanceledException {
         public ConnectionAbortedException();
         public ConnectionAbortedException(string message);
         public ConnectionAbortedException(string message, Exception inner);
     }
     public class ConnectionBuilder : IConnectionBuilder {
         public ConnectionBuilder(IServiceProvider applicationServices);
         public IServiceProvider ApplicationServices { get; }
         public ConnectionDelegate Build();
         public IConnectionBuilder Use(Func<ConnectionDelegate, ConnectionDelegate> middleware);
     }
     public static class ConnectionBuilderExtensions {
         public static IConnectionBuilder Run(this IConnectionBuilder connectionBuilder, Func<ConnectionContext, Task> middleware);
         public static IConnectionBuilder Use(this IConnectionBuilder connectionBuilder, Func<ConnectionContext, Func<Task>, Task> middleware);
         public static IConnectionBuilder UseConnectionHandler<TConnectionHandler>(this IConnectionBuilder connectionBuilder) where TConnectionHandler : ConnectionHandler;
     }
-    public abstract class ConnectionContext {
+    public abstract class ConnectionContext : IAsyncDisposable {
         protected ConnectionContext();
+        public virtual CancellationToken ConnectionClosed { get; set; }
         public abstract string ConnectionId { get; set; }
         public abstract IFeatureCollection Features { get; }
         public abstract IDictionary<object, object> Items { get; set; }
+        public virtual EndPoint LocalEndPoint { get; set; }
+        public virtual EndPoint RemoteEndPoint { get; set; }
         public abstract IDuplexPipe Transport { get; set; }
         public virtual void Abort();
         public virtual void Abort(ConnectionAbortedException abortReason);
+        public virtual ValueTask DisposeAsync();
     }
     public delegate Task ConnectionDelegate(ConnectionContext connection);
     public abstract class ConnectionHandler {
         protected ConnectionHandler();
         public abstract Task OnConnectedAsync(ConnectionContext connection);
     }
     public class ConnectionItems : ICollection<KeyValuePair<object, object>>, IDictionary<object, object>, IEnumerable, IEnumerable<KeyValuePair<object, object>> {
         public ConnectionItems();
         public ConnectionItems(IDictionary<object, object> items);
         public IDictionary<object, object> Items { get; }
         int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Count { get; }
         bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.IsReadOnly { get; }
         object System.Collections.Generic.IDictionary<System.Object,System.Object>.this[object key] { get; set; }
         ICollection<object> System.Collections.Generic.IDictionary<System.Object,System.Object>.Keys { get; }
         ICollection<object> System.Collections.Generic.IDictionary<System.Object,System.Object>.Values { get; }
         void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Add(KeyValuePair<object, object> item);
         void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Clear();
         bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Contains(KeyValuePair<object, object> item);
         void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.CopyTo(KeyValuePair<object, object>[] array, int arrayIndex);
         bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Remove(KeyValuePair<object, object> item);
         void System.Collections.Generic.IDictionary<System.Object,System.Object>.Add(object key, object value);
         bool System.Collections.Generic.IDictionary<System.Object,System.Object>.ContainsKey(object key);
         bool System.Collections.Generic.IDictionary<System.Object,System.Object>.Remove(object key);
         bool System.Collections.Generic.IDictionary<System.Object,System.Object>.TryGetValue(object key, out object value);
         IEnumerator<KeyValuePair<object, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
     public class ConnectionResetException : IOException {
         public ConnectionResetException(string message);
         public ConnectionResetException(string message, Exception inner);
     }
-    public class DefaultConnectionContext : ConnectionContext, IConnectionIdFeature, IConnectionItemsFeature, IConnectionLifetimeFeature, IConnectionTransportFeature, IConnectionUserFeature, IDisposable {
+    public class DefaultConnectionContext : ConnectionContext, IConnectionEndPointFeature, IConnectionIdFeature, IConnectionItemsFeature, IConnectionLifetimeFeature, IConnectionTransportFeature, IConnectionUserFeature {
         public DefaultConnectionContext();
         public DefaultConnectionContext(string id);
         public DefaultConnectionContext(string id, IDuplexPipe transport, IDuplexPipe application);
         public IDuplexPipe Application { get; set; }
-        public CancellationToken ConnectionClosed { get; set; }
+        public override CancellationToken ConnectionClosed { get; set; }
         public override string ConnectionId { get; set; }
         public override IFeatureCollection Features { get; }
         public override IDictionary<object, object> Items { get; set; }
+        public override EndPoint LocalEndPoint { get; set; }
+        public override EndPoint RemoteEndPoint { get; set; }
         public override IDuplexPipe Transport { get; set; }
         public ClaimsPrincipal User { get; set; }
         public override void Abort(ConnectionAbortedException abortReason);
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
     public interface IConnectionBuilder {
         IServiceProvider ApplicationServices { get; }
         ConnectionDelegate Build();
         IConnectionBuilder Use(Func<ConnectionDelegate, ConnectionDelegate> middleware);
     }
+    public interface IConnectionListener : IAsyncDisposable {
+        EndPoint EndPoint { get; }
+        ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default(CancellationToken));
+        ValueTask UnbindAsync(CancellationToken cancellationToken = default(CancellationToken));
+    }
+    public interface IConnectionListenerFactory {
+        ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default(CancellationToken));
+    }
     public enum TransferFormat {
         Binary = 1,
         Text = 2,
     }
 }
```

