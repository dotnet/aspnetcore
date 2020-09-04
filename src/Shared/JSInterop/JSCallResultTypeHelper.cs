// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.JSInterop
{
    internal static class JSCallResultTypeHelper
    {
        public static JSCallResultType FromGeneric<TResult>()
            => typeof(TResult) == typeof(IJSObjectReference)
            || typeof(TResult) == typeof(IJSInProcessObjectReference)
            || typeof(TResult) == typeof(IJSUnmarshalledObjectReference) ?
                JSCallResultType.JSObjectReference :
                JSCallResultType.Default;
    }
}
