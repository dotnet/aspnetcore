// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Browser.Rendering
{
    /// <summary>
    /// Represents an exception related to remote rendering.
    /// </summary>
    public class RemoteRendererException : Exception
    {
        /// <summary>
        /// Constructs an instance of <see cref="RemoteRendererException"/>.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public RemoteRendererException(string message) : base(message)
        {
        }
    }
}
