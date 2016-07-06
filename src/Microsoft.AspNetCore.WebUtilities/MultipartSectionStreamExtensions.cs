// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.WebUtilities
{
    /// <summary>
    /// Various extension methods for dealing with the section body stream
    /// </summary>
    public static class MultipartSectionStreamExtensions
    {
        /// <summary>
        /// Reads the body of the section as a string
        /// </summary>
        /// <param name="section">The section to read from</param>
        /// <returns>The body steam as string</returns>
        public static async Task<string> ReadAsStringAsync(this MultipartSection section)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            MediaTypeHeaderValue sectionMediaType;
            MediaTypeHeaderValue.TryParse(section.ContentType, out sectionMediaType);

            Encoding streamEncoding = sectionMediaType?.Encoding;
            if (streamEncoding == null || streamEncoding == Encoding.UTF7)
            {
                streamEncoding = Encoding.UTF8;
            }

            using (var reader = new StreamReader(
                section.Body, 
                streamEncoding,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024, 
                leaveOpen: true))
            {
                return await reader.ReadToEndAsync();
            }
        } 
    }
}
