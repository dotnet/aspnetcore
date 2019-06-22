# System.IO.Pipelines

``` diff
 namespace System.IO.Pipelines {
     public struct FlushResult {
         public FlushResult(bool isCanceled, bool isCompleted);
         public bool IsCanceled { get; }
         public bool IsCompleted { get; }
     }
     public interface IDuplexPipe {
         PipeReader Input { get; }
         PipeWriter Output { get; }
     }
     public sealed class Pipe {
         public Pipe();
         public Pipe(PipeOptions options);
         public PipeReader Reader { get; }
         public PipeWriter Writer { get; }
         public void Reset();
     }
     public class PipeOptions {
-        public PipeOptions(MemoryPool<byte> pool = null, PipeScheduler readerScheduler = null, PipeScheduler writerScheduler = null, long pauseWriterThreshold = (long)32768, long resumeWriterThreshold = (long)16384, int minimumSegmentSize = 2048, bool useSynchronizationContext = true);
+        public PipeOptions(MemoryPool<byte> pool = null, PipeScheduler readerScheduler = null, PipeScheduler writerScheduler = null, long pauseWriterThreshold = (long)65536, long resumeWriterThreshold = (long)32768, int minimumSegmentSize = 4096, bool useSynchronizationContext = true);
         public static PipeOptions Default { get; }
         public int MinimumSegmentSize { get; }
         public long PauseWriterThreshold { get; }
         public MemoryPool<byte> Pool { get; }
         public PipeScheduler ReaderScheduler { get; }
         public long ResumeWriterThreshold { get; }
         public bool UseSynchronizationContext { get; }
         public PipeScheduler WriterScheduler { get; }
     }
     public abstract class PipeReader {
         protected PipeReader();
         public abstract void AdvanceTo(SequencePosition consumed);
         public abstract void AdvanceTo(SequencePosition consumed, SequencePosition examined);
+        public virtual Stream AsStream(bool leaveOpen = false);
         public abstract void CancelPendingRead();
         public abstract void Complete(Exception exception = null);
+        public virtual Task CopyToAsync(PipeWriter destination, CancellationToken cancellationToken = default(CancellationToken));
+        public virtual Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default(CancellationToken));
+        public static PipeReader Create(Stream stream, StreamPipeReaderOptions readerOptions = null);
         public abstract void OnWriterCompleted(Action<Exception, object> callback, object state);
         public abstract ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default(CancellationToken));
         public abstract bool TryRead(out ReadResult result);
     }
     public abstract class PipeScheduler {
         protected PipeScheduler();
         public static PipeScheduler Inline { get; }
         public static PipeScheduler ThreadPool { get; }
         public abstract void Schedule(Action<object> action, object state);
     }
     public abstract class PipeWriter : IBufferWriter<byte> {
         protected PipeWriter();
         public abstract void Advance(int bytes);
+        public virtual Stream AsStream(bool leaveOpen = false);
         public abstract void CancelPendingFlush();
         public abstract void Complete(Exception exception = null);
+        protected internal virtual Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default(CancellationToken));
+        public static PipeWriter Create(Stream stream, StreamPipeWriterOptions writerOptions = null);
         public abstract ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default(CancellationToken));
         public abstract Memory<byte> GetMemory(int sizeHint = 0);
         public abstract Span<byte> GetSpan(int sizeHint = 0);
         public abstract void OnReaderCompleted(Action<Exception, object> callback, object state);
         public virtual ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default(CancellationToken));
     }
-    public struct ReadResult {
+    public readonly struct ReadResult {
         public ReadResult(ReadOnlySequence<byte> buffer, bool isCanceled, bool isCompleted);
         public ReadOnlySequence<byte> Buffer { get; }
         public bool IsCanceled { get; }
         public bool IsCompleted { get; }
     }
+    public static class StreamPipeExtensions {
+        public static Task CopyToAsync(this Stream source, PipeWriter destination, CancellationToken cancellationToken = default(CancellationToken));
+    }
+    public class StreamPipeReaderOptions {
+        public StreamPipeReaderOptions(MemoryPool<byte> pool = null, int bufferSize = 4096, int minimumReadSize = 1024, bool leaveOpen = false);
+        public int BufferSize { get; }
+        public bool LeaveOpen { get; }
+        public int MinimumReadSize { get; }
+        public MemoryPool<byte> Pool { get; }
+    }
+    public class StreamPipeWriterOptions {
+        public StreamPipeWriterOptions(MemoryPool<byte> pool = null, int minimumBufferSize = 4096, bool leaveOpen = false);
+        public bool LeaveOpen { get; }
+        public int MinimumBufferSize { get; }
+        public MemoryPool<byte> Pool { get; }
+    }
 }
```

