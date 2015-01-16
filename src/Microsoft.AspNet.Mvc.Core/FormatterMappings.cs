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
    /// These options are used to specify mapping between the Url Format and corresponding ContentType.
    /// </summary>
    public class FormatterMappings
    {
        private readonly Dictionary<string, MediaTypeHeaderValue> _map =
            new Dictionary<string, MediaTypeHeaderValue>(StringComparer.OrdinalIgnoreCase);

        public void SetFormatMapping([NotNull] string format, [NotNull] MediaTypeHeaderValue contentType)
        {
            if (contentType == null)
            {
                throw new ArgumentException((Resources.ArgumentCannotBeNullOrEmpty), "contentType");
            }

            format = RemovePeriodIfPresent(format);
            _map[format] = contentType;
        }

        public MediaTypeHeaderValue GetContentTypeForFormat(string format)
        {
            format = RemovePeriodIfPresent(format);
            MediaTypeHeaderValue value = null;
            _map.TryGetValue(format, out value);
            return value;
        }

        private string RemovePeriodIfPresent(string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "format");
            }
            if (format.StartsWith("."))
            {
                format = format.Substring(1);
            }

            return format;
        }        
    }
}