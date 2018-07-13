// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.JSInterop.Internal
{
    // This type takes care of a special case in handling the result of an async call from
    // .NET to JS. The information about what type the result should be exists only on the
    // corresponding TaskCompletionSource<T>. We don't have that information at the time
    // that we deserialize the incoming argsJson before calling DotNetDispatcher.EndInvoke.
    // Declaring the EndInvoke parameter type as JSAsyncCallResult defers the deserialization
    // until later when we have access to the TaskCompletionSource<T>.
    //
    // There's no reason why developers would need anything similar to this in user code,
    // because this is the mechanism by which we resolve the incoming argsJson to the correct
    // user types before completing calls.
    //
    // It's marked as 'public' only because it has to be for use as an argument on a
    // [JSInvokable] method.

    /// <summary>
    /// Intended for framework use only.
    /// </summary>
    public class JSAsyncCallResult
    {
        internal object ResultOrException { get; }

        /// <summary>
        /// Constructs an instance of <see cref="JSAsyncCallResult"/>.
        /// </summary>
        /// <param name="resultOrException">The result of the call.</param>
        internal JSAsyncCallResult(object resultOrException)
        {
            ResultOrException = resultOrException;
        }
    }
}
