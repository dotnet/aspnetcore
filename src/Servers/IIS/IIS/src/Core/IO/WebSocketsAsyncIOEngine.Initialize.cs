// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO
{
    internal partial class WebSocketsAsyncIOEngine
    {
        internal class AsyncInitializeOperation : AsyncIOOperation
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
                _engine.ReturnOperation(this);
            }
        }
    }
}
