// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Supplies information about an drag event that is being raised.
    /// </summary>
    public class UIDragEventArgs : UIMouseEventArgs
    {
        /// <summary>
        /// The data that underlies a drag-and-drop operation, known as the drag data store.
        /// See <see cref="DataTransfer"/>.
        /// </summary>
        public DataTransfer DataTransfer { get; set; }
    }
}
