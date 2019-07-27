// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Provides information about the <see cref="EditContext.OnValidationRequested"/> event.
    /// </summary>
    public sealed class ValidationRequestedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets a shared empty instance of <see cref="ValidationRequestedEventArgs"/>.
        /// </summary>
        public static new readonly ValidationRequestedEventArgs Empty = new ValidationRequestedEventArgs();

        /// <summary>
        /// Creates a new instance of <see cref="ValidationRequestedEventArgs"/>.
        /// </summary>
        public ValidationRequestedEventArgs()
        {
        }
    }
}
