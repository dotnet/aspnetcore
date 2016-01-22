// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using Microsoft.AspNet.Html;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.Rendering;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    /// <summary>
    /// Default implementation of <see cref="IJsonHelper"/>.
    /// </summary>
    public class JsonHelper : IJsonHelper
    {
        private readonly JsonOutputFormatter _jsonOutputFormatter;

        /// <summary>
        /// Initializes a new instance of <see cref="JsonHelper"/> that is backed by <paramref name="jsonOutputFormatter"/>.
        /// </summary>
        /// <param name="jsonOutputFormatter">The <see cref="JsonOutputFormatter"/> used to serialize JSON.</param>
        public JsonHelper(JsonOutputFormatter jsonOutputFormatter)
        {
            if (jsonOutputFormatter == null)
            {
                throw new ArgumentNullException(nameof(jsonOutputFormatter));
            }

            _jsonOutputFormatter = jsonOutputFormatter;
        }

        /// <inheritdoc />
        public IHtmlContent Serialize(object value)
        {
            return SerializeInternal(_jsonOutputFormatter, value);
        }

        /// <inheritdoc />
        public IHtmlContent Serialize(object value, JsonSerializerSettings serializerSettings)
        {
            if (serializerSettings == null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            var jsonOutputFormatter = new JsonOutputFormatter(serializerSettings);

            return SerializeInternal(jsonOutputFormatter, value);
        }

        private IHtmlContent SerializeInternal(JsonOutputFormatter jsonOutputFormatter, object value)
        {
            var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            jsonOutputFormatter.WriteObject(stringWriter, value);

            return new HtmlString(stringWriter.ToString());
        }
    }
}