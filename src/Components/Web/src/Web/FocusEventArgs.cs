// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Supplies information about a focus event that is being raised.
    /// </summary>
    public class FocusEventArgs : EventArgs
    {
        // Not including support for 'relatedTarget' since we don't have a good way to represent it.
        // see: https://developer.mozilla.org/en-US/docs/Web/API/FocusEvent

        /// <summary>
        /// Gets or sets the type of the event.
        /// </summary>
        public string Type { get; set; }
    }
}
