// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO;

internal partial class AsyncIOEngine
{
    internal sealed class AsyncReadOperation : AsyncIOOperation
    {
        private readonly AsyncIOEngine _engine;

        private MemoryHandle _inputHandle;

        private NativeSafeHandle? _requestHandler;

        private Memory<byte> _memory;

        public AsyncReadOperation(AsyncIOEngine engine)
        {
            _engine = engine;
        }

        public void Initialize(NativeSafeHandle requestHandler, Memory<byte> memory)
        {
            _requestHandler = requestHandler;
            _memory = memory;
        }

        protected override unsafe bool InvokeOperation(out int hr, out int bytes)
        {
            Debug.Assert(_requestHandler != null, "Must initialize first.");

            _inputHandle = _memory.Pin();
            hr = NativeMethods.HttpReadRequestBytes(
                _requestHandler,
                (byte*)_inputHandle.Pointer,
                _memory.Length,
                out bytes,
                out bool completionExpected);

            return !completionExpected;
        }

        protected override void ResetOperation()
        {
            base.ResetOperation();

            _memory = default;
            _inputHandle.Dispose();
            _inputHandle = default;
            _requestHandler = default;

            _engine.ReturnOperation(this);
        }

        public override void FreeOperationResources(int hr, int bytes)
        {
            _inputHandle.Dispose();
        }

        protected override bool IsSuccessfulResult(int hr) => hr == NativeMethods.ERROR_HANDLE_EOF;
    }
}
