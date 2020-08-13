// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Internal
{
    internal static partial class AspNetCoreTempDirectory
    {
        public static string TempDirectory { get { throw null; } }
        public static System.Func<string> TempDirectoryFactory { get { throw null; } }
    }
}

namespace Microsoft.AspNetCore.WebUtilities
{
    public sealed partial class FileBufferingWriteStream : System.IO.Stream
    {
        internal bool Disposed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal System.IO.FileStream FileStream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal Microsoft.AspNetCore.WebUtilities.PagedByteBuffer PagedByteBuffer
        {
            [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; }
        }
    }

    public partial class FormPipeReader
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void ParseFormValues(ref System.Buffers.ReadOnlySequence<byte> buffer, ref Microsoft.AspNetCore.WebUtilities.KeyValueAccumulator accumulator, bool isFinalBlock) { }
    }

    public partial class HttpResponseStreamWriter : System.IO.TextWriter
    {
        internal const int DefaultBufferSize = 16384;
    }

    internal sealed partial class PagedByteBuffer : System.IDisposable
    {
        internal const int PageSize = 1024;
        public PagedByteBuffer(System.Buffers.ArrayPool<byte> arrayPool) { }
        internal bool Disposed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int Length { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal System.Collections.Generic.List<byte[]> Pages { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void Add(byte[] buffer, int offset, int count) { }
        public void Dispose() { }
        public void MoveTo(System.IO.Stream stream) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task MoveToAsync(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
