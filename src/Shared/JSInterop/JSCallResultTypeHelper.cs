// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.JSInterop
{
    internal static class JSCallResultTypeHelper
    {
        // We avoid using Assembly.GetExecutingAssembly() because this is shared code.
        private static readonly Assembly _currentAssembly = typeof(JSCallResultType).Assembly;

        public static JSCallResultType FromGeneric<TResult>()
        {
            var resultType = typeof(TResult);

            if (resultType.Assembly == _currentAssembly)
            {
                if (resultType == typeof(IJSObjectReference)
                    || resultType == typeof(IJSInProcessObjectReference)
                    || resultType == typeof(IJSUnmarshalledObjectReference))
                {
                    return JSCallResultType.JSObjectReference;
                }
                else
                {
                    throw new ArgumentException(
                        $"JS interop cannot supply an instance of type '{resultType}'. Consider using " +
                        $"'{typeof(IJSObjectReference)}' instead.");
                }
            }
            else
            {
                return JSCallResultType.Default;
            }
        }
    }
}
