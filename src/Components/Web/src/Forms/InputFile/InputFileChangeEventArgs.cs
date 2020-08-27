// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Supplies information about an <see cref="InputFile.OnChange"/> event being raised.
    /// </summary>
    public sealed class InputFileChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs a new <see cref="InputFileChangeEventArgs"/> instance.
        /// </summary>
        /// <param name="files">The list of <see cref="IBrowserFile"/>.</param>
        public InputFileChangeEventArgs(IReadOnlyList<IBrowserFile> files)
        {
            Files = files;
        }

        /// <summary>
        /// Gets the file entries list.
        /// </summary>
        public IReadOnlyList<IBrowserFile> Files { get; init; }
    }
}
