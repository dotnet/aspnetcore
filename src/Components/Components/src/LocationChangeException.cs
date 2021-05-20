// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// An exception thrown when <see cref="NavigationManager.LocationChanged"/> throws an exception.
    /// </summary>
    public sealed class LocationChangeException : Exception
    {
        /// <summary>
        /// Creates a new instance of <see cref="LocationChangeException"/>.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public LocationChangeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
