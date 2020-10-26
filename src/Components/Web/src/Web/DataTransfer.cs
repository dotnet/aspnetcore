// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// The <see cref="DataTransfer"/> object is used to hold the data that is being dragged during a drag and drop operation.
    /// It may hold one or more <see cref="DataTransferItem"/>, each of one or more data types.
    /// For more information about drag and drop, see HTML Drag and Drop API.
    /// </summary>
    public class DataTransfer
    {
        /// <summary>
        /// Gets the type of drag-and-drop operation currently selected or sets the operation to a new type.
        /// The value must be none, copy, link or move.
        /// </summary>
        public string DropEffect { get; set; }

        /// <summary>
        /// Provides all of the types of operations that are possible.
        /// Must be one of none, copy, copyLink, copyMove, link, linkMove, move, all or uninitialized.
        /// </summary>
        public string EffectAllowed { get; set; }

        /// <summary>
        /// Contains a list of all the local files available on the data transfer.
        /// If the drag operation doesn't involve dragging files, this property is an empty list.
        /// </summary>
        public string[] Files { get; set; }

        /// <summary>
        /// Gives a <see cref="DataTransferItem"/> array which is a list of all of the drag data.
        /// </summary>
        public DataTransferItem[] Items { get; set; }

        /// <summary>
        /// An array of <see cref="string"/> giving the formats that were set in the dragstart event.
        /// </summary>
        public string[] Types { get; set; }
    }
}
