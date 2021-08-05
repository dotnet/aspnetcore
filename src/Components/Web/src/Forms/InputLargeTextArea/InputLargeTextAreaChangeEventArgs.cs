// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Supplies information about an <see cref="Microsoft.AspNetCore.Components.Forms.InputLargeTextArea.OnChange"/> event being raised.
    /// </summary>
    public sealed class InputLargeTextAreaChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs a new <see cref="InputLargeTextAreaChangeEventArgs"/> instance.
        /// </summary>
        /// <param name="sender">The textarea element for which the event was raised.</param>
        /// <param name="length">The length of the textarea value.</param>
        public InputLargeTextAreaChangeEventArgs(InputLargeTextArea sender, int length)
        {
            Sender = sender;
            Length = length;
        }

        /// <summary>
        /// The textarea element for which the event was raised.
        /// </summary>
        public InputLargeTextArea Sender { get; }

        /// <summary>
        /// Gets the length of the textarea value.
        /// </summary>
        public int Length { get; }
    }
}
