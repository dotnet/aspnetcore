// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.WebUtilities
{
    /// <summary>
    /// Various extensions for converting multipart sections
    /// </summary>
    public static class MultipartSectionConverterExtensions
    {
        /// <summary>
        /// Converts the section to a file section
        /// </summary>
        /// <param name="section">The section to convert</param>
        /// <returns>A file section</returns>
        public static FileMultipartSection AsFileSection(this MultipartSection section)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            try
            {
                return new FileMultipartSection(section);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts the section to a form section
        /// </summary>
        /// <param name="section">The section to convert</param>
        /// <returns>A form section</returns>
        public static FormMultipartSection AsFormDataSection(this MultipartSection section)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            try
            {
                return new FormMultipartSection(section);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieves and parses the content disposition header from a section
        /// </summary>
        /// <param name="section">The section from which to retrieve</param>
        /// <returns>A <see cref="ContentDispositionHeaderValue"/> if the header was found, null otherwise</returns>
        public static ContentDispositionHeaderValue GetContentDispositionHeader(this MultipartSection section)
        {
            ContentDispositionHeaderValue header;
            if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out header))
            {
                return null;
            }

            return header;
        }
    }
}
