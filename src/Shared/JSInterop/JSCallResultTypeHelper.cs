// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop;

internal static class JSCallResultTypeHelper
{
    // We avoid using Assembly.GetExecutingAssembly() because this is shared code.
    private static readonly Assembly _currentAssembly = typeof(JSCallResultType).Assembly;

    public static JSCallResultType FromGeneric<TResult>()
    {
        if (typeof(TResult).Assembly == _currentAssembly)
        {
            if (typeof(TResult) == typeof(IJSObjectReference) ||
                typeof(TResult) == typeof(IJSInProcessObjectReference))
            {
                return JSCallResultType.JSObjectReference;
            }
            else if (typeof(TResult) == typeof(IJSStreamReference))
            {
                return JSCallResultType.JSStreamReference;
            }
            else if (typeof(TResult) == typeof(IJSVoidResult))
            {
                return JSCallResultType.JSVoidResult;
            }
        }

        return JSCallResultType.Default;
    }
}
