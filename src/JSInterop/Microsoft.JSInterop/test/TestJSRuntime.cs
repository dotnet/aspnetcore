// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.JSInterop
{
    internal class TestJSRuntime : JSRuntime
    {
        protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson)
        {
            throw new NotImplementedException();
        }

        protected internal override void EndInvokeDotNet(string callId, bool success, object resultOrError, string assemblyName, string methodIdentifier, long dotNetObjectId)
        {
            throw new NotImplementedException();
        }
    }
}
