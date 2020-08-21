// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Contains helpers for creating <see cref="JSCallResultType"/> instances.
    /// </summary>
    public static class JSCallResultTypeHelper
    {
        /// <summary>
        /// Creates a <see cref="JSCallResultType"/> from the given generic type.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result of the relevant JS interop call.
        /// </typeparam>
        /// <returns></returns>
        public static JSCallResultType FromGeneric<TResult>()
            => typeof(JSObjectReference).IsAssignableFrom(typeof(TResult)) ?
                JSCallResultType.JSObjectReference :
                JSCallResultType.Default;
    }
}
