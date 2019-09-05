// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// The <see cref="DataTransferItem"/> object represents one drag data item.
    /// During a drag operation, each drag event has a dataTransfer property which contains a list of drag data items.
    /// Each item in the list is a <see cref="DataTransferItem"/> object.
    /// </summary>
    public class DataTransferItem
    {
        /// <summary>
        /// The kind of drag data item, string or file
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// The drag data item's type, typically a MIME type
        /// </summary>
        public string Type { get; set; }
    }
}
