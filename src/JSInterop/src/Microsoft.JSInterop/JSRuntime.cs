// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Provides mechanisms for accessing the current <see cref="IJSRuntime"/>.
    /// </summary>
    public static class JSRuntime
    {
        private static AsyncLocal<IJSRuntime> _currentJSRuntime
            = new AsyncLocal<IJSRuntime>();

        /// <summary>
        /// Gets the current <see cref="IJSRuntime"/>, if any.
        /// </summary>
        public static IJSRuntime Current => _currentJSRuntime.Value;

        /// <summary>
        /// Sets the current JS runtime to the supplied instance.
        ///
        /// This is intended for framework use. Developers should not normally need to call this method.
        /// </summary>
        /// <param name="instance">The new current <see cref="IJSRuntime"/>.</param>
        public static void SetCurrentJSRuntime(IJSRuntime instance)
        {
            _currentJSRuntime.Value = instance
                ?? throw new ArgumentNullException(nameof(instance));
        }
    }
}
