// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.JSInterop
{
    public enum JSCallResultType : int
    {
        Default = 0,
        JSObjectReference = 1,
    }

    public static class JSCallResultTypeHelper
    {
        public static JSCallResultType FromGeneric<TResult>()
            => typeof(JSObjectReference).IsAssignableFrom(typeof(TResult)) ?
                JSCallResultType.JSObjectReference :
                JSCallResultType.Default;
    }
}
