// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO
{
    internal partial class AsyncIOEngine
    {
        private class AsyncWriteOperation : AsyncWriteOperationBase
        {
            private readonly AsyncIOEngine _engine;

            public AsyncWriteOperation(AsyncIOEngine engine)
            {
                _engine = engine;
            }

            protected override unsafe int WriteChunks(IntPtr requestHandler, int chunkCount, HttpApiTypes.HTTP_DATA_CHUNK* dataChunks,
                out bool completionExpected)
            {
                return NativeMethods.HttpWriteResponseBytes(requestHandler, dataChunks, chunkCount, out completionExpected);
            }

            protected override void ResetOperation()
            {
                base.ResetOperation();

                _engine.ReturnOperation(this);
            }
        }
    }
}
