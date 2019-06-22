# Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal

``` diff
-namespace Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal {
 {
-    public class AdaptedPipeline : IDuplexPipe {
 {
-        public AdaptedPipeline(IDuplexPipe transport, Pipe inputPipe, Pipe outputPipe, IKestrelTrace log);

-        public Pipe Input { get; }

-        public IKestrelTrace Log { get; }

-        public Pipe Output { get; }

-        PipeReader System.IO.Pipelines.IDuplexPipe.Input { get; }

-        PipeWriter System.IO.Pipelines.IDuplexPipe.Output { get; }

-        public Task RunAsync(Stream stream);

-    }
-    public class ConnectionAdapterContext {
 {
-        public Stream ConnectionStream { get; }

-        public IFeatureCollection Features { get; }

-    }
-    public interface IAdaptedConnection : IDisposable {
 {
-        Stream ConnectionStream { get; }

-    }
-    public interface IConnectionAdapter {
 {
-        bool IsHttps { get; }

-        Task<IAdaptedConnection> OnConnectionAsync(ConnectionAdapterContext context);

-    }
-    public class LoggingConnectionAdapter : IConnectionAdapter {
 {
-        public LoggingConnectionAdapter(ILogger logger);

-        public bool IsHttps { get; }

-        public Task<IAdaptedConnection> OnConnectionAsync(ConnectionAdapterContext context);

-    }
-    public class RawStream : Stream {
 {
-        public RawStream(PipeReader input, PipeWriter output);

-        public override bool CanRead { get; }

-        public override bool CanSeek { get; }

-        public override bool CanWrite { get; }

-        public override long Length { get; }

-        public override long Position { get; set; }

-        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state);

-        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state);

-        public override int EndRead(IAsyncResult asyncResult);

-        public override void EndWrite(IAsyncResult asyncResult);

-        public override void Flush();

-        public override Task FlushAsync(CancellationToken cancellationToken);

-        public override int Read(byte[] buffer, int offset, int count);

-        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

-        public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default(CancellationToken));

-        public override long Seek(long offset, SeekOrigin origin);

-        public override void SetLength(long value);

-        public override void Write(byte[] buffer, int offset, int count);

-        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

-        public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default(CancellationToken));

-    }
-}
```

