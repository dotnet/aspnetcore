// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO
{
    internal partial class AsyncIOEngine
    {
        internal class AsyncReadOperation : AsyncIOOperation
        {
            private readonly AsyncIOEngine _engine;

            private MemoryHandle _inputHandle;

            private IntPtr _requestHandler;

            private Memory<byte> _memory;

            public AsyncReadOperation(AsyncIOEngine engine)
            {
                _engine = engine;
            }

            public void Initialize(IntPtr requestHandler, Memory<byte> memory)
            {
                _requestHandler = requestHandler;
                _memory = memory;
            }

            protected override unsafe bool InvokeOperation(out int hr, out int bytes)
            {
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
        }
    }
}
