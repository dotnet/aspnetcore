// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Contains helper methods for <see cref="IBrowserFile"/>.
    /// </summary>
    public static class BrowserFileExtensions
    {
        /// <summary>
        /// Converts the current image file to a new one of the specified file type and maximum file dimensions.
        /// </summary>
        /// <remarks>
        /// The image will be scaled to fit the specified dimensions while preserving the original aspect ratio.
        /// </remarks>
        /// <param name="browserFile">The <see cref="IBrowserFile"/> to convert to a new image file.</param>
        /// <param name="format">The new image format.</param>
        /// <param name="maxWith">The maximum image width.</param>
        /// <param name="maxHeight">The maximum image height</param>
        /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
        public static ValueTask<IBrowserFile> RequestImageFileAsync(this IBrowserFile browserFile, string format, int maxWith, int maxHeight)
        {
            if (browserFile is BrowserFile browserFileInternal)
            {
                return browserFileInternal.Owner.ConvertToImageFileAsync(browserFileInternal, format, maxWith, maxHeight);
            }

            throw new InvalidOperationException($"Cannot perform this operation on custom {typeof(IBrowserFile)} implementations.");
        }
    }
}
