# Microsoft.AspNetCore.WebUtilities

``` diff
 namespace Microsoft.AspNetCore.WebUtilities {
     public class FileBufferingReadStream : Stream {
+        public FileBufferingReadStream(Stream inner, int memoryThreshold);
     }
+    public sealed class FileBufferingWriteStream : Stream {
+        public FileBufferingWriteStream(int memoryThreshold = 32768, Nullable<long> bufferLimit = default(Nullable<long>), Func<string> tempFileDirectoryAccessor = null);
+        public override bool CanRead { get; }
+        public override bool CanSeek { get; }
+        public override bool CanWrite { get; }
+        public override long Length { get; }
+        public override long Position { get; set; }
+        protected override void Dispose(bool disposing);
+        public override ValueTask DisposeAsync();
+        public Task DrainBufferAsync(Stream destination, CancellationToken cancellationToken = default(CancellationToken));
+        public override void Flush();
+        public override Task FlushAsync(CancellationToken cancellationToken);
+        public override int Read(byte[] buffer, int offset, int count);
+        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
+        public override long Seek(long offset, SeekOrigin origin);
+        public override void SetLength(long value);
+        public override void Write(byte[] buffer, int offset, int count);
+        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
+    }
+    public class FormPipeReader {
+        public FormPipeReader(PipeReader pipeReader);
+        public FormPipeReader(PipeReader pipeReader, Encoding encoding);
+        public int KeyLengthLimit { get; set; }
+        public int ValueCountLimit { get; set; }
+        public int ValueLengthLimit { get; set; }
+        public Task<Dictionary<string, StringValues>> ReadFormAsync(CancellationToken cancellationToken = default(CancellationToken));
+    }
     public class HttpResponseStreamWriter : TextWriter {
+        public override ValueTask DisposeAsync();
     }
     public static class WebEncoders {
+        public static string Base64UrlEncode(ReadOnlySpan<byte> input);
     }
 }
```

