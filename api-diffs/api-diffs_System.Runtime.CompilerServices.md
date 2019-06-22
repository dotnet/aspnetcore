# System.Runtime.CompilerServices

``` diff
-namespace System.Runtime.CompilerServices {
 {
-    public static class Unsafe {
 {
-        public unsafe static void* Add<T>(void* source, int elementOffset);

-        public static ref T Add<T>(ref T source, int elementOffset);

-        public static ref T Add<T>(ref T source, IntPtr elementOffset);

-        public static ref T AddByteOffset<T>(ref T source, IntPtr byteOffset);

-        public static bool AreSame<T>(ref T left, ref T right);

-        public static T As<T>(object o) where T : class;

-        public static ref TTo As<TFrom, TTo>(ref TFrom source);

-        public unsafe static void* AsPointer<T>(ref T value);

-        public unsafe static ref T AsRef<T>(void* source);

-        public static ref T AsRef<T>(in T source);

-        public static IntPtr ByteOffset<T>(ref T origin, ref T target);

-        public unsafe static void Copy<T>(void* destination, ref T source);

-        public unsafe static void Copy<T>(ref T destination, void* source);

-        public static void CopyBlock(ref byte destination, ref byte source, uint byteCount);

-        public unsafe static void CopyBlock(void* destination, void* source, uint byteCount);

-        public static void CopyBlockUnaligned(ref byte destination, ref byte source, uint byteCount);

-        public unsafe static void CopyBlockUnaligned(void* destination, void* source, uint byteCount);

-        public static void InitBlock(ref byte startAddress, byte value, uint byteCount);

-        public unsafe static void InitBlock(void* startAddress, byte value, uint byteCount);

-        public static void InitBlockUnaligned(ref byte startAddress, byte value, uint byteCount);

-        public unsafe static void InitBlockUnaligned(void* startAddress, byte value, uint byteCount);

-        public static bool IsAddressGreaterThan<T>(ref T left, ref T right);

-        public static bool IsAddressLessThan<T>(ref T left, ref T right);

-        public unsafe static T Read<T>(void* source);

-        public static T ReadUnaligned<T>(ref byte source);

-        public unsafe static T ReadUnaligned<T>(void* source);

-        public static int SizeOf<T>();

-        public unsafe static void* Subtract<T>(void* source, int elementOffset);

-        public static ref T Subtract<T>(ref T source, int elementOffset);

-        public static ref T Subtract<T>(ref T source, IntPtr elementOffset);

-        public static ref T SubtractByteOffset<T>(ref T source, IntPtr byteOffset);

-        public unsafe static void Write<T>(void* destination, T value);

-        public static void WriteUnaligned<T>(ref byte destination, T value);

-        public unsafe static void WriteUnaligned<T>(void* destination, T value);

-    }
-}
```

