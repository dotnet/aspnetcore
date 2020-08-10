// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO
{
    internal partial class WebSocketsAsyncIOEngine
    {
        internal class AsyncInitializeOperation : AsyncIOOperation
        {
            private readonly WebSocketsAsyncIOEngine _engine;

            private HandlerSafeHandle _requestHandler;

            public AsyncInitializeOperation(WebSocketsAsyncIOEngine engine)
            {
                _engine = engine;
            }

            public void Initialize(HandlerSafeHandle requestHandler)
            {
                _requestHandler = requestHandler;
            }

            protected override bool InvokeOperation(out int hr, out int bytes)
            {
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
