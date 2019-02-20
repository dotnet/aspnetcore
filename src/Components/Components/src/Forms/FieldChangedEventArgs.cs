// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Provides information about the <see cref="EditContext.OnFieldChanged"/> event.
    /// </summary>
    public sealed class FieldChangedEventArgs
    {
        /// <summary>
        /// Identifies the field whose value has changed.
        /// </summary>
        public FieldIdentifier FieldIdentifier { get; }

        internal FieldChangedEventArgs(in FieldIdentifier fieldIdentifier)
        {
            FieldIdentifier = fieldIdentifier;
        }
    }
}
