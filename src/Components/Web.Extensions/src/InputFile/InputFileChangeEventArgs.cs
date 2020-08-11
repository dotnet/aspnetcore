// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// Supplies information about an <see cref="InputFile.OnChange"/> event being raised.
    /// </summary>
    public class InputFileChangeEventArgs : EventArgs
    {
        /// <summary>
        /// The updated file entries list.
        /// </summary>
        public IReadOnlyList<IBrowserFile> Files { get; }

        /// <summary>
        /// Constructs a new <see cref="InputFileChangeEventArgs"/> instance.
        /// </summary>
        /// <param name="files">The updated file entries list.</param>
        public InputFileChangeEventArgs(IReadOnlyList<IBrowserFile> files)
        {
            Files = files;
        }
    }
}
