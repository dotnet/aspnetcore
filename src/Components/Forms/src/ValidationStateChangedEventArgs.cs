// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Provides information about the <see cref="EditContext.OnValidationStateChanged"/> event.
    /// </summary>
    public sealed class ValidationStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets a shared empty instance of <see cref="ValidationStateChangedEventArgs"/>.
        /// </summary>
        public new static readonly ValidationStateChangedEventArgs Empty = new ValidationStateChangedEventArgs();

        /// <summary>
        /// Creates a new instance of <see cref="ValidationStateChangedEventArgs" />
        /// </summary>
        public ValidationStateChangedEventArgs()
        {
        }
    }
}
