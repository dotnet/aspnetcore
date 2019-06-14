# Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack

``` diff
-namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack {
 {
-    public class DynamicTable {
 {
-        public DynamicTable(int maxSize);

-        public int Count { get; }

-        public int MaxSize { get; }

-        public int Size { get; }

-        public HeaderField this[int index] { get; }

-        public void Insert(Span<byte> name, Span<byte> value);

-        public void Resize(int maxSize);

-    }
-    public struct HeaderField {
 {
-        public const int RfcOverhead = 32;

-        public HeaderField(Span<byte> name, Span<byte> value);

-        public int Length { get; }

-        public byte[] Name { get; }

-        public byte[] Value { get; }

-        public static int GetLength(int nameLength, int valueLength);

-    }
-    public class HPackDecoder {
 {
-        public HPackDecoder(int maxDynamicTableSize, int maxRequestHeaderFieldSize);

-        public void Decode(ReadOnlySequence<byte> data, bool endHeaders, IHttpHeadersHandler handler);

-    }
-    public class HPackDecodingException : Exception {
 {
-        public HPackDecodingException(string message);

-        public HPackDecodingException(string message, Exception innerException);

-    }
-    public class HPackEncoder {
 {
-        public HPackEncoder();

-        public bool BeginEncode(IEnumerable<KeyValuePair<string, string>> headers, Span<byte> buffer, out int length);

-        public bool BeginEncode(int statusCode, IEnumerable<KeyValuePair<string, string>> headers, Span<byte> buffer, out int length);

-        public bool Encode(Span<byte> buffer, out int length);

-    }
-    public class HPackEncodingException : Exception {
 {
-        public HPackEncodingException(string message);

-        public HPackEncodingException(string message, Exception innerException);

-    }
-    public class Huffman {
 {
-        public Huffman();

-        public static int Decode(ReadOnlySpan<byte> src, Span<byte> dst);

-        public static ValueTuple<uint, int> Encode(int data);

-    }
-    public class HuffmanDecodingException : Exception {
 {
-        public HuffmanDecodingException(string message);

-    }
-    public class IntegerDecoder {
 {
-        public IntegerDecoder();

-        public bool BeginTryDecode(byte b, int prefixLength, out int result);

-        public static void ThrowIntegerTooBigException();

-        public bool TryDecode(byte b, out int result);

-    }
-    public static class IntegerEncoder {
 {
-        public static bool Encode(int i, int n, Span<byte> buffer, out int length);

-    }
-    public class StaticTable {
 {
-        public int Count { get; }

-        public static StaticTable Instance { get; }

-        public IReadOnlyDictionary<int, int> StatusIndex { get; }

-        public HeaderField this[int index] { get; }

-    }
-    public static class StatusCodes {
 {
-        public static byte[] ToStatusBytes(int statusCode);

-    }
-}
```

