// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Represents errors that occur during an interop call from .NET to JavaScript when the JavaScript runtime becomes disconnected.
    /// </summary>
    public sealed class JSDisconnectedException : Exception
    {
        /// <summary>
        /// Constructs an instance of <see cref="JSDisconnectedException"/>.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public JSDisconnectedException(string message) : base(message)
        {
        }
    }
}
