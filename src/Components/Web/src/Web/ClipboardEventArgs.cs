// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Supplies information about an clipboard event that is being raised.
    /// </summary>
    public class ClipboardEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the type of the event.
        /// </summary>
        public string Type { get; set; }
    }
}
