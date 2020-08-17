// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace WebAssembly.JSInterop
{
    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    internal struct JSCallInfo
    {
        [FieldOffset(0)]
        public string FunctionIdentifier;

        [FieldOffset(4)]
        public JSCallResultType ResultType;

        [FieldOffset(8)]
        public string MarshalledCallArgsJson;

        [FieldOffset(12)]
        public long MarshalledCallAsyncHandle;
    }
}
