// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Describes the type of result expected from a JS interop call.
    /// </summary>
    public enum JSCallResultType : int
    {
        /// <summary>
        /// Indicates that the returned value is not treated in a special way.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Indicates that the returned value is to be treated as a JS object reference.
        /// </summary>
        JSObjectReference = 1,
    }

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
