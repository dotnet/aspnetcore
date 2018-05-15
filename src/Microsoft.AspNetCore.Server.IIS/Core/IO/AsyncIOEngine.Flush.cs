// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO
{
    internal partial class AsyncIOEngine
    {
        internal class AsyncFlushOperation : AsyncIOOperation
        {
            private readonly AsyncIOEngine _engine;

            private IntPtr _requestHandler;

            public AsyncFlushOperation(AsyncIOEngine engine)
            {
                _engine = engine;
            }

            public void Initialize(IntPtr requestHandler)
            {
                _requestHandler = requestHandler;
            }

            protected override bool InvokeOperation(out int hr, out int bytes)
            {
                bytes = 0;
                hr = NativeMethods.HttpFlushResponseBytes(_requestHandler, out var fCompletionExpected);

                return !fCompletionExpected;
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
