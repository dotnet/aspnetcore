# Microsoft.AspNetCore.WebUtilities

``` diff
 namespace Microsoft.AspNetCore.WebUtilities {
     public static class Base64UrlTextEncoder {
         public static byte[] Decode(string text);
         public static string Encode(byte[] data);
     }
     public class BufferedReadStream : Stream {
         public BufferedReadStream(Stream inner, int bufferSize);
         public BufferedReadStream(Stream inner, int bufferSize, ArrayPool<byte> bytePool);
         public ArraySegment<byte> BufferedData { get; }
         public override bool CanRead { get; }
         public override bool CanSeek { get; }
         public override bool CanTimeout { get; }
         public override bool CanWrite { get; }
         public override long Length { get; }
         public override long Position { get; set; }
         protected override void Dispose(bool disposing);
         public bool EnsureBuffered();
         public bool EnsureBuffered(int minCount);
         public Task<bool> EnsureBufferedAsync(int minCount, CancellationToken cancellationToken);
         public Task<bool> EnsureBufferedAsync(CancellationToken cancellationToken);
         public override void Flush();
         public override Task FlushAsync(CancellationToken cancellationToken);
         public override int Read(byte[] buffer, int offset, int count);
         public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
         public string ReadLine(int lengthLimit);
         public Task<string> ReadLineAsync(int lengthLimit, CancellationToken cancellationToken);
         public override long Seek(long offset, SeekOrigin origin);
         public override void SetLength(long value);
         public override void Write(byte[] buffer, int offset, int count);
         public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
     }
     public class FileBufferingReadStream : Stream {
+        public FileBufferingReadStream(Stream inner, int memoryThreshold);
         public FileBufferingReadStream(Stream inner, int memoryThreshold, Nullable<long> bufferLimit, Func<string> tempFileDirectoryAccessor);
         public FileBufferingReadStream(Stream inner, int memoryThreshold, Nullable<long> bufferLimit, Func<string> tempFileDirectoryAccessor, ArrayPool<byte> bytePool);
         public FileBufferingReadStream(Stream inner, int memoryThreshold, Nullable<long> bufferLimit, string tempFileDirectory);
         public FileBufferingReadStream(Stream inner, int memoryThreshold, Nullable<long> bufferLimit, string tempFileDirectory, ArrayPool<byte> bytePool);
         public override bool CanRead { get; }
         public override bool CanSeek { get; }
         public override bool CanWrite { get; }
         public bool InMemory { get; }
         public override long Length { get; }
         public override long Position { get; set; }
         public string TempFileName { get; }
         protected override void Dispose(bool disposing);
+        public override ValueTask DisposeAsync();
         public override void Flush();
         public override int Read(byte[] buffer, int offset, int count);
         public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
         public override long Seek(long offset, SeekOrigin origin);
         public override void SetLength(long value);
         public override void Write(byte[] buffer, int offset, int count);
         public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
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
     public class FileMultipartSection {
         public FileMultipartSection(MultipartSection section);
         public FileMultipartSection(MultipartSection section, ContentDispositionHeaderValue header);
         public string FileName { get; }
         public Stream FileStream { get; }
         public string Name { get; }
         public MultipartSection Section { get; }
     }
     public class FormMultipartSection {
         public FormMultipartSection(MultipartSection section);
         public FormMultipartSection(MultipartSection section, ContentDispositionHeaderValue header);
         public string Name { get; }
         public MultipartSection Section { get; }
         public Task<string> GetValueAsync();
     }
+    public class FormPipeReader {
+        public FormPipeReader(PipeReader pipeReader);
+        public FormPipeReader(PipeReader pipeReader, Encoding encoding);
+        public int KeyLengthLimit { get; set; }
+        public int ValueCountLimit { get; set; }
+        public int ValueLengthLimit { get; set; }
+        public Task<Dictionary<string, StringValues>> ReadFormAsync(CancellationToken cancellationToken = default(CancellationToken));
+    }
     public class FormReader : IDisposable {
         public const int DefaultKeyLengthLimit = 2048;
         public const int DefaultValueCountLimit = 1024;
         public const int DefaultValueLengthLimit = 4194304;
         public FormReader(Stream stream);
         public FormReader(Stream stream, Encoding encoding);
         public FormReader(Stream stream, Encoding encoding, ArrayPool<char> charPool);
         public FormReader(string data);
         public FormReader(string data, ArrayPool<char> charPool);
         public int KeyLengthLimit { get; set; }
         public int ValueCountLimit { get; set; }
         public int ValueLengthLimit { get; set; }
         public void Dispose();
         public Dictionary<string, StringValues> ReadForm();
         public Task<Dictionary<string, StringValues>> ReadFormAsync(CancellationToken cancellationToken = default(CancellationToken));
         public Nullable<KeyValuePair<string, string>> ReadNextPair();
         public Task<Nullable<KeyValuePair<string, string>>> ReadNextPairAsync(CancellationToken cancellationToken = default(CancellationToken));
     }
     public class HttpRequestStreamReader : TextReader {
         public HttpRequestStreamReader(Stream stream, Encoding encoding);
         public HttpRequestStreamReader(Stream stream, Encoding encoding, int bufferSize);
         public HttpRequestStreamReader(Stream stream, Encoding encoding, int bufferSize, ArrayPool<byte> bytePool, ArrayPool<char> charPool);
         protected override void Dispose(bool disposing);
         public override int Peek();
         public override int Read();
         public override int Read(char[] buffer, int index, int count);
         public override Task<int> ReadAsync(char[] buffer, int index, int count);
     }
     public class HttpResponseStreamWriter : TextWriter {
         public HttpResponseStreamWriter(Stream stream, Encoding encoding);
         public HttpResponseStreamWriter(Stream stream, Encoding encoding, int bufferSize);
         public HttpResponseStreamWriter(Stream stream, Encoding encoding, int bufferSize, ArrayPool<byte> bytePool, ArrayPool<char> charPool);
         public override Encoding Encoding { get; }
         protected override void Dispose(bool disposing);
+        public override ValueTask DisposeAsync();
         public override void Flush();
         public override Task FlushAsync();
         public override void Write(char value);
         public override void Write(char[] values, int index, int count);
         public override void Write(string value);
         public override Task WriteAsync(char value);
         public override Task WriteAsync(char[] values, int index, int count);
         public override Task WriteAsync(string value);
     }
     public struct KeyValueAccumulator {
         public bool HasValues { get; }
         public int KeyCount { get; }
         public int ValueCount { get; private set; }
         public void Append(string key, string value);
         public Dictionary<string, StringValues> GetResults();
     }
     public class MultipartReader {
         public const int DefaultHeadersCountLimit = 16;
         public const int DefaultHeadersLengthLimit = 16384;
         public MultipartReader(string boundary, Stream stream);
         public MultipartReader(string boundary, Stream stream, int bufferSize);
         public Nullable<long> BodyLengthLimit { get; set; }
         public int HeadersCountLimit { get; set; }
         public int HeadersLengthLimit { get; set; }
         public Task<MultipartSection> ReadNextSectionAsync(CancellationToken cancellationToken = default(CancellationToken));
     }
     public class MultipartSection {
         public MultipartSection();
         public Nullable<long> BaseStreamOffset { get; set; }
         public Stream Body { get; set; }
         public string ContentDisposition { get; }
         public string ContentType { get; }
         public Dictionary<string, StringValues> Headers { get; set; }
     }
     public static class MultipartSectionConverterExtensions {
         public static FileMultipartSection AsFileSection(this MultipartSection section);
         public static FormMultipartSection AsFormDataSection(this MultipartSection section);
         public static ContentDispositionHeaderValue GetContentDispositionHeader(this MultipartSection section);
     }
     public static class MultipartSectionStreamExtensions {
         public static Task<string> ReadAsStringAsync(this MultipartSection section);
     }
     public static class QueryHelpers {
         public static string AddQueryString(string uri, IDictionary<string, string> queryString);
         public static string AddQueryString(string uri, string name, string value);
         public static Dictionary<string, StringValues> ParseNullableQuery(string queryString);
         public static Dictionary<string, StringValues> ParseQuery(string queryString);
     }
     public static class ReasonPhrases {
         public static string GetReasonPhrase(int statusCode);
     }
     public static class StreamHelperExtensions {
         public static Task DrainAsync(this Stream stream, ArrayPool<byte> bytePool, Nullable<long> limit, CancellationToken cancellationToken);
         public static Task DrainAsync(this Stream stream, Nullable<long> limit, CancellationToken cancellationToken);
         public static Task DrainAsync(this Stream stream, CancellationToken cancellationToken);
     }
     public static class WebEncoders {
         public static byte[] Base64UrlDecode(string input);
         public static byte[] Base64UrlDecode(string input, int offset, char[] buffer, int bufferOffset, int count);
         public static byte[] Base64UrlDecode(string input, int offset, int count);
         public static string Base64UrlEncode(byte[] input);
         public static int Base64UrlEncode(byte[] input, int offset, char[] output, int outputOffset, int count);
         public static string Base64UrlEncode(byte[] input, int offset, int count);
+        public static string Base64UrlEncode(ReadOnlySpan<byte> input);
         public static int GetArraySizeRequiredToDecode(int count);
         public static int GetArraySizeRequiredToEncode(int count);
     }
 }
```

