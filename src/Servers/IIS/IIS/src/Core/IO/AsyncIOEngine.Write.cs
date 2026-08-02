// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO;

internal partial class AsyncIOEngine
{
    private sealed class AsyncWriteOperation : AsyncWriteOperationBase
    {
        private readonly AsyncIOEngine _engine;

        public AsyncWriteOperation(AsyncIOEngine engine)
        {
            _engine = engine;
        }

        protected override unsafe int WriteChunks(NativeSafeHandle requestHandler, int chunkCount, HTTP_DATA_CHUNK* dataChunks,
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
