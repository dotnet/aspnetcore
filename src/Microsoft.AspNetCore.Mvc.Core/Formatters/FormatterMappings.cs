// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// Used to specify mapping between the URL Format and corresponding media type.
    /// </summary>
    public class FormatterMappings
    {
        private readonly Dictionary<string, string> _map =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Sets mapping for the format to specified media type.
        /// If the format already exists, the media type will be overwritten with the new value.
        /// </summary>
        /// <param name="format">The format value.</param>
        /// <param name="contentType">The media type for the format value.</param>
        public void SetMediaTypeMappingForFormat(string format, string contentType)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            SetMediaTypeMappingForFormat(format, MediaTypeHeaderValue.Parse(contentType));
        }

        /// <summary>
        /// Sets mapping for the format to specified media type.
        /// If the format already exists, the media type will be overwritten with the new value.
        /// </summary>
        /// <param name="format">The format value.</param>
        /// <param name="contentType">The media type for the format value.</param>
        public void SetMediaTypeMappingForFormat(string format, MediaTypeHeaderValue contentType)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            ValidateContentType(contentType);
            format = RemovePeriodIfPresent(format);
            _map[format] = contentType.ToString();
        }

        /// <summary>
        /// Gets the media type for the specified format.
        /// </summary>
        /// <param name="format">The format value.</param>
        /// <returns>The media type for input format.</returns>
        public string GetMediaTypeMappingForFormat(string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                var message = Resources.FormatFormatFormatterMappings_GetMediaTypeMappingForFormat_InvalidFormat(
                    nameof(format));

                throw new ArgumentException(message, nameof(format));
            }

            format = RemovePeriodIfPresent(format);

            _map.TryGetValue(format, out var value);

            return value;
        }

        /// <summary>
        /// Clears the media type mapping for the format.
        /// </summary>
        /// <param name="format">The format value.</param>
        /// <returns><c>true</c> if the format is successfully found and cleared; otherwise, <c>false</c>.</returns>
        public bool ClearMediaTypeMappingForFormat(string format)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            format = RemovePeriodIfPresent(format);
            return _map.Remove(format);
        }

        private void ValidateContentType(MediaTypeHeaderValue contentType)
        {
            if (contentType.Type == "*" || contentType.SubType == "*")
            {
                throw new ArgumentException(
                    string.Format(Resources.FormatterMappings_NotValidMediaType, contentType),
                    nameof(contentType));
            }
        }

        private string RemovePeriodIfPresent(string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(format));
            }

            if (format.StartsWith(".", StringComparison.Ordinal))
            {
                if (format == ".")
                {
                    throw new ArgumentException(string.Format(Resources.Format_NotValid, format), nameof(format));
                }

                format = format.Substring(1);
            }

            return format;
        }
    }
}