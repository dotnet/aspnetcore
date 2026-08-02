// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO;

internal partial class WebSocketsAsyncIOEngine
{
    internal sealed class AsyncInitializeOperation : AsyncIOOperation
    {
        private readonly WebSocketsAsyncIOEngine _engine;

        private NativeSafeHandle? _requestHandler;

        public AsyncInitializeOperation(WebSocketsAsyncIOEngine engine)
        {
            _engine = engine;
        }

        public void Initialize(NativeSafeHandle requestHandler)
        {
            _requestHandler = requestHandler;
        }

        protected override bool InvokeOperation(out int hr, out int bytes)
        {
            Debug.Assert(_requestHandler != null, "Must initialize first.");
            hr = NativeMethods.HttpFlushResponseBytes(_requestHandler, fMoreData: true, out var completionExpected);
            bytes = 0;
            return !completionExpected;
        }

        protected override void ResetOperation()
        {
            base.ResetOperation();

            _requestHandler = default;
        }
    }
}
