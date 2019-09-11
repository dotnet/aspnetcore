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
            private bool _moreData;

            public AsyncFlushOperation(AsyncIOEngine engine)
            {
                _engine = engine;
            }

            public void Initialize(IntPtr requestHandler, bool moreData)
            {
                _requestHandler = requestHandler;
                _moreData = moreData;
            }

            protected override bool InvokeOperation(out int hr, out int bytes)
            {
                bytes = 0;
                hr = NativeMethods.HttpFlushResponseBytes(_requestHandler, _moreData, out var fCompletionExpected);

                return !fCompletionExpected;
            }

            protected override void ResetOperation()
            {
                base.ResetOperation();

                _requestHandler = default;
                _moreData = false;
                _engine.ReturnOperation(this);
            }
        }
    }
}
