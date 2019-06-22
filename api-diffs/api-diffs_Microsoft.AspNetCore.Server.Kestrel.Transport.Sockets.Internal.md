# Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal

``` diff
-namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal {
 {
-    public static class BufferExtensions {
 {
-        public static ArraySegment<byte> GetArray(this Memory<byte> memory);

-        public static ArraySegment<byte> GetArray(this ReadOnlyMemory<byte> memory);

-    }
-    public class IOQueue : PipeScheduler {
 {
-        public IOQueue();

-        public override void Schedule(Action<object> action, object state);

-    }
-    public interface ISocketsTrace : ILogger {
 {
-        void ConnectionError(string connectionId, Exception ex);

-        void ConnectionPause(string connectionId);

-        void ConnectionReadFin(string connectionId);

-        void ConnectionReset(string connectionId);

-        void ConnectionResume(string connectionId);

-        void ConnectionWriteFin(string connectionId, string reason);

-    }
-    public class SocketAwaitableEventArgs : SocketAsyncEventArgs, ICriticalNotifyCompletion, INotifyCompletion {
 {
-        public SocketAwaitableEventArgs(PipeScheduler ioScheduler);

-        public bool IsCompleted { get; }

-        public void Complete();

-        public SocketAwaitableEventArgs GetAwaiter();

-        public int GetResult();

-        public void OnCompleted(Action continuation);

-        protected override void OnCompleted(SocketAsyncEventArgs _);

-        public void UnsafeOnCompleted(Action continuation);

-    }
-    public sealed class SocketReceiver : SocketSenderReceiverBase {
 {
-        public SocketReceiver(Socket socket, PipeScheduler scheduler);

-        public SocketAwaitableEventArgs ReceiveAsync(Memory<byte> buffer);

-        public SocketAwaitableEventArgs WaitForDataAsync();

-    }
-    public sealed class SocketSender : SocketSenderReceiverBase {
 {
-        public SocketSender(Socket socket, PipeScheduler scheduler);

-        public SocketAwaitableEventArgs SendAsync(ReadOnlySequence<byte> buffers);

-    }
-    public abstract class SocketSenderReceiverBase : IDisposable {
 {
-        protected readonly SocketAwaitableEventArgs _awaitableEventArgs;

-        protected readonly Socket _socket;

-        protected SocketSenderReceiverBase(Socket socket, PipeScheduler scheduler);

-        public void Dispose();

-    }
-    public class SocketsTrace : ILogger, ISocketsTrace {
 {
-        public SocketsTrace(ILogger logger);

-        public IDisposable BeginScope<TState>(TState state);

-        public void ConnectionError(string connectionId, Exception ex);

-        public void ConnectionPause(string connectionId);

-        public void ConnectionRead(string connectionId, int count);

-        public void ConnectionReadFin(string connectionId);

-        public void ConnectionReset(string connectionId);

-        public void ConnectionResume(string connectionId);

-        public void ConnectionWrite(string connectionId, int count);

-        public void ConnectionWriteCallback(string connectionId, int status);

-        public void ConnectionWriteFin(string connectionId, string reason);

-        public bool IsEnabled(LogLevel logLevel);

-        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);

-    }
-}
```

