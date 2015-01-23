// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Net.Http.Headers;
using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Used to specify mapping between the URL Format and corresponding <see cref="MediaTypeHeaderValue"/>.
    /// </summary>
    public class FormatterMappings
    {
        private readonly Dictionary<string, MediaTypeHeaderValue> _map =
            new Dictionary<string, MediaTypeHeaderValue>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Sets mapping for the format to specified <see cref="MediaTypeHeaderValue"/>. 
        /// If the format already exists, the <see cref="MediaTypeHeaderValue"/> will be overwritten with the new value.
        /// </summary>
        /// <param name="format">The format value.</param>
        /// <param name="contentType">The <see cref="MediaTypeHeaderValue"/> for the format value.</param>
        public void SetMediaTypeMappingForFormat([NotNull] string format, [NotNull] MediaTypeHeaderValue contentType)
        {
            ValidateContentType(contentType);
            format = RemovePeriodIfPresent(format);
            _map[format] = contentType;
        }

        /// <summary>
        /// Gets <see cref="MediaTypeHeaderValue"/> for the specified format.
        /// </summary>
        /// <param name="format">The format value.</param>
        /// <returns>The <see cref="MediaTypeHeaderValue"/> for input format.</returns>
        public MediaTypeHeaderValue GetMediaTypeMappingForFormat([NotNull] string format)
        {
            format = RemovePeriodIfPresent(format);

            MediaTypeHeaderValue value = null;
            _map.TryGetValue(format, out value);
            
            return value;
        }

        /// <summary>
        /// Clears the <see cref="MediaTypeHeaderValue"/> mapping for the format.
        /// </summary>
        /// <param name="format">The format value.</param>
        /// <returns><c>true</c> if the format is successfully found and cleared; otherwise, <c>false</c>.</returns>
        public bool ClearMediaTypeMappingForFormat([NotNull] string format)
        {
            format = RemovePeriodIfPresent(format);
            return _map.Remove(format);
        }

        private void ValidateContentType(MediaTypeHeaderValue contentType)
        {
            if (contentType.Type == "*" || contentType.SubType == "*")
            {
                throw new ArgumentException(string.Format(Resources.FormatterMappings_NotValidMediaType, contentType));
            }
        }

        private string RemovePeriodIfPresent(string format)
        {
            if (format == "")
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "format");
            }

            if (format.StartsWith("."))
            {
                if (format == ".")
                {
                    throw new ArgumentException(string.Format(Resources.Format_NotValid, format));
                }

                format = format.Substring(1);                
            }            

            return format;
        }        
    }
}