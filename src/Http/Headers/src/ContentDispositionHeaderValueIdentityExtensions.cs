// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers
{
    /// <summary>
    /// Various extension methods for <see cref="ContentDispositionHeaderValue"/> for identifying the type of the disposition header
    /// </summary>
    public static class ContentDispositionHeaderValueIdentityExtensions
    {
        /// <summary>
        /// Checks if the content disposition header is a file disposition
        /// </summary>
        /// <param name="header">The header to check</param>
        /// <returns>True if the header is file disposition, false otherwise</returns>
        public static bool IsFileDisposition(this ContentDispositionHeaderValue header)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            return header.DispositionType.Equals("form-data")
                && (!StringSegment.IsNullOrEmpty(header.FileName) || !StringSegment.IsNullOrEmpty(header.FileNameStar));
        }

        /// <summary>
        /// Checks if the content disposition header is a form disposition
        /// </summary>
        /// <param name="header">The header to check</param>
        /// <returns>True if the header is form disposition, false otherwise</returns>
        public static bool IsFormDisposition(this ContentDispositionHeaderValue header)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            return header.DispositionType.Equals("form-data")
               && StringSegment.IsNullOrEmpty(header.FileName) && StringSegment.IsNullOrEmpty(header.FileNameStar);
        }
    }
}
