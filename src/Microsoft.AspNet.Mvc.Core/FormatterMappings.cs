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
    /// Used to specify mapping between the Url Format and corresponding <see cref="MediaTypeHeaderValue"/>.
    /// </summary>
    public class FormatterMappings
    {
        private readonly Dictionary<string, MediaTypeHeaderValue> _map =
            new Dictionary<string, MediaTypeHeaderValue>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// This will set mapping for the format to specified <see cref="MediaTypeHeaderValue"/>. 
        /// If the format already exists, the <see cref="MediaTypeHeaderValue"/> will be overwritten with the new value.
        /// </summary>
        public void SetMediaTypeMappingForFormat([NotNull] string format, [NotNull] MediaTypeHeaderValue contentType)
        {
            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "format");
            }

            if (contentType == null)
            {
                throw new ArgumentException((Resources.ArgumentCannotBeNullOrEmpty), "contentType");
            }

            format = RemovePeriodIfPresent(format);
            _map[format] = contentType;
        }

        /// <summary>
        /// Gets <see cref="MediaTypeHeaderValue"/> for the specified format.
        /// </summary>
        public MediaTypeHeaderValue GetMediaTypeForFormat(string format)
        {
            format = RemovePeriodIfPresent(format);
            MediaTypeHeaderValue value = null;
            _map.TryGetValue(format, out value);
            return value;
        }

        private string RemovePeriodIfPresent(string format)
        {
            if (format.StartsWith("."))
            {
                format = format.Substring(1);
            }

            return format;
        }        
    }
}