// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO;

internal partial class WebSocketsAsyncIOEngine
{
    internal sealed class WebSocketWriteOperation : AsyncWriteOperationBase, IDisposable
    {
        [UnmanagedCallersOnly]
        private static NativeMethods.REQUEST_NOTIFICATION_STATUS WriteCallback(IntPtr httpContext, IntPtr completionInfo, IntPtr completionContext)
        {
            var context = (WebSocketWriteOperation)GCHandle.FromIntPtr(completionContext).Target!;

            NativeMethods.HttpGetCompletionInfo(completionInfo, out var cbBytes, out var hr);

            var continuation = context.Complete(hr, cbBytes);
            continuation.Invoke();

            return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_PENDING;
        }

        private readonly WebSocketsAsyncIOEngine _engine;
        private readonly GCHandle _thisHandle;

        public WebSocketWriteOperation(WebSocketsAsyncIOEngine engine)
        {
            _thisHandle = GCHandle.Alloc(this);
            _engine = engine;
        }

        protected override unsafe int WriteChunks(NativeSafeHandle requestHandler, int chunkCount, HTTP_DATA_CHUNK* dataChunks, out bool completionExpected)
        {
            return NativeMethods.HttpWebsocketsWriteBytes(requestHandler, dataChunks, chunkCount, &WriteCallback, (IntPtr)_thisHandle, out completionExpected);
        }

        protected override void ResetOperation()
        {
            base.ResetOperation();
        }

        public void Dispose()
        {
            _thisHandle.Free();
        }
    }
}
