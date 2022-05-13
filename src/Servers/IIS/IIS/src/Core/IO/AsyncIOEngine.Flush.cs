// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.IIS.Core.IO;

internal partial class AsyncIOEngine
{
    internal sealed class AsyncFlushOperation : AsyncIOOperation
    {
        private readonly AsyncIOEngine _engine;

        private NativeSafeHandle? _requestHandler;
        private bool _moreData;

        public AsyncFlushOperation(AsyncIOEngine engine)
        {
            _engine = engine;
        }

        public void Initialize(NativeSafeHandle requestHandler, bool moreData)
        {
            _requestHandler = requestHandler;
            _moreData = moreData;
        }

        protected override bool InvokeOperation(out int hr, out int bytes)
        {
            Debug.Assert(_requestHandler != null, "Must initialize first.");

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
