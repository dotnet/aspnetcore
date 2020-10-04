// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.JSInterop
{
    internal static class JSCallResultTypeHelper
    {
        // We avoid using Assembly.GetExecutingAssembly() because this is shared code.
        private static readonly Assembly _currentAssembly = typeof(JSCallResultType).Assembly;

        public static JSCallResultType FromGeneric<TResult>()
        {
            if (typeof(TResult).Assembly == _currentAssembly
                && (typeof(TResult) == typeof(IJSObjectReference)
                || typeof(TResult) == typeof(IJSInProcessObjectReference)
                || typeof(TResult) == typeof(IJSUnmarshalledObjectReference)))
            {
                return JSCallResultType.JSObjectReference;
            }
            else
            {
                return JSCallResultType.Default;
            }
        }
    }
}
