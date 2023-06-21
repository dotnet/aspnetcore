// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO;

internal partial class WebSocketsAsyncIOEngine
{
    internal sealed class WebSocketReadOperation : AsyncIOOperation, IDisposable
    {
        [UnmanagedCallersOnly]
        public static NativeMethods.REQUEST_NOTIFICATION_STATUS ReadCallback(IntPtr httpContext, IntPtr completionInfo, IntPtr completionContext)
        {
            var context = (WebSocketReadOperation)GCHandle.FromIntPtr(completionContext).Target!;

            NativeMethods.HttpGetCompletionInfo(completionInfo, out var cbBytes, out var hr);

            var continuation = context.Complete(hr, cbBytes);

            continuation.Invoke();

            return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_PENDING;
        }

        private readonly WebSocketsAsyncIOEngine _engine;
        private readonly GCHandle _thisHandle;
        private MemoryHandle _inputHandle;
        private NativeSafeHandle? _requestHandler;
        private Memory<byte> _memory;

        public WebSocketReadOperation(WebSocketsAsyncIOEngine engine)
        {
            _thisHandle = GCHandle.Alloc(this);
            _engine = engine;
        }

        protected override unsafe bool InvokeOperation(out int hr, out int bytes)
        {
            Debug.Assert(_requestHandler != null, "Must initialize first.");

            _inputHandle = _memory.Pin();

            hr = NativeMethods.HttpWebsocketsReadBytes(
                _requestHandler,
                (byte*)_inputHandle.Pointer,
                _memory.Length,
                &ReadCallback,
                (IntPtr)_thisHandle,
                out bytes,
                out var completionExpected);

            return !completionExpected;
        }

        public void Initialize(NativeSafeHandle requestHandler, Memory<byte> memory)
        {
            _requestHandler = requestHandler;
            _memory = memory;
        }

        public override void FreeOperationResources(int hr, int bytes)
        {
            _inputHandle.Dispose();
        }

        protected override void ResetOperation()
        {
            base.ResetOperation();

            _memory = default;
            _inputHandle.Dispose();
            _inputHandle = default;
            _requestHandler = default;
        }

        protected override bool IsSuccessfulResult(int hr) => hr == NativeMethods.ERROR_HANDLE_EOF;

        public void Dispose()
        {
            _thisHandle.Free();
        }
    }
}
