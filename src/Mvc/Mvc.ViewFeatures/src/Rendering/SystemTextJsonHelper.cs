
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    internal class SystemTextJsonHelper : IJsonHelper
    {
        private readonly JsonOptions _options;

        public SystemTextJsonHelper(IOptions<JsonOptions> options)
        {
            _options = options.Value;
        }

        /// <inheritdoc />
        public IHtmlContent Serialize(object value)
        {
            // JsonSerializer always encodes non-ASCII chars, so we do not need
            // to do anything special with the SerializerOptions
            var json = JsonSerializer.Serialize(value, _options.JsonSerializerOptions);
            return new HtmlString(json);
        }
    }
}
