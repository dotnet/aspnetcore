// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Rendering
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
        public JsonHelper([NotNull] JsonOutputFormatter jsonOutputFormatter)
        {
            _jsonOutputFormatter = jsonOutputFormatter;
        }

        /// <inheritdoc />
        public HtmlString Serialize(object value)
        {
            return SerializeInternal(_jsonOutputFormatter, value);
        }

        /// <inheritdoc />
        public HtmlString Serialize(object value, [NotNull] JsonSerializerSettings serializerSettings)
        {
            var jsonOutputFormatter = new JsonOutputFormatter(serializerSettings);

            return SerializeInternal(jsonOutputFormatter, value);
        }

        private HtmlString SerializeInternal(JsonOutputFormatter jsonOutputFormatter, object value)
        {
            var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            jsonOutputFormatter.WriteObject(stringWriter, value);

            return new HtmlString(stringWriter.ToString());
        }
    }
}