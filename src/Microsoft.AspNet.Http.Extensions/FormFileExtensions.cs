// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Http
{
    /// <summary>
    /// Extension methods for <see cref="IFormFile"/>.
    /// </summary>
    public static class FormFileExtensions
    {
        // Stream.CopyTo method uses 80KB as the default buffer size.
        private static int DefaultBufferSize = 80 * 1024;

        /// <summary>
        /// Saves the contents of an uploaded file.
        /// </summary>
        /// <param name="formFile">The <see cref="IFormFile"/>.</param>
        /// <param name="filename">The name of the file to create.</param>
        public static void SaveAs(this IFormFile formFile, string filename)
        {
            if (formFile == null)
            {
                throw new ArgumentNullException(nameof(formFile));
            }

            using (var fileStream = new FileStream(filename, FileMode.Create))
            {
                var inputStream = formFile.OpenReadStream();
                inputStream.CopyTo(fileStream);
            }
        }

        /// <summary>
        /// Asynchronously saves the contents of an uploaded file.
        /// </summary>
        /// <param name="formFile">The <see cref="IFormFile"/>.</param>
        /// <param name="filename">The name of the file to create.</param>
        public async static Task SaveAsAsync(
            this IFormFile formFile,
            string filename,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (formFile == null)
            {
                throw new ArgumentNullException(nameof(formFile));
            }

            using (var fileStream = new FileStream(filename, FileMode.Create))
            {
                var inputStream = formFile.OpenReadStream();
                await inputStream.CopyToAsync(fileStream, DefaultBufferSize, cancellationToken);
            }
        }
    }
}