# System.Threading.Channels

``` diff
-namespace System.Threading.Channels {
 {
-    public enum BoundedChannelFullMode {
 {
-        DropNewest = 1,

-        DropOldest = 2,

-        DropWrite = 3,

-        Wait = 0,

-    }
-    public sealed class BoundedChannelOptions : ChannelOptions {
 {
-        public BoundedChannelOptions(int capacity);

-        public int Capacity { get; set; }

-        public BoundedChannelFullMode FullMode { get; set; }

-    }
-    public static class Channel {
 {
-        public static Channel<T> CreateBounded<T>(int capacity);

-        public static Channel<T> CreateBounded<T>(BoundedChannelOptions options);

-        public static Channel<T> CreateUnbounded<T>();

-        public static Channel<T> CreateUnbounded<T>(UnboundedChannelOptions options);

-    }
-    public abstract class Channel<T> : Channel<T, T> {
 {
-        protected Channel();

-    }
-    public abstract class Channel<TWrite, TRead> {
 {
-        protected Channel();

-        public ChannelReader<TRead> Reader { get; protected set; }

-        public ChannelWriter<TWrite> Writer { get; protected set; }

-        public static implicit operator ChannelReader<TRead> (Channel<TWrite, TRead> channel);

-        public static implicit operator ChannelWriter<TWrite> (Channel<TWrite, TRead> channel);

-    }
-    public class ChannelClosedException : InvalidOperationException {
 {
-        public ChannelClosedException();

-        public ChannelClosedException(Exception innerException);

-        public ChannelClosedException(string message);

-        public ChannelClosedException(string message, Exception innerException);

-    }
-    public abstract class ChannelOptions {
 {
-        protected ChannelOptions();

-        public bool AllowSynchronousContinuations { get; set; }

-        public bool SingleReader { get; set; }

-        public bool SingleWriter { get; set; }

-    }
-    public abstract class ChannelReader<T> {
 {
-        protected ChannelReader();

-        public virtual Task Completion { get; }

-        public virtual ValueTask<T> ReadAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public abstract bool TryRead(out T item);

-        public abstract ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public abstract class ChannelWriter<T> {
 {
-        protected ChannelWriter();

-        public void Complete(Exception error = null);

-        public virtual bool TryComplete(Exception error = null);

-        public abstract bool TryWrite(T item);

-        public abstract ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual ValueTask WriteAsync(T item, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public sealed class UnboundedChannelOptions : ChannelOptions {
 {
-        public UnboundedChannelOptions();

-    }
-}
```

