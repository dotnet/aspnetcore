
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    internal class SystemTextJsonHelper : IJsonHelper
    {
        private readonly MvcOptions _mvcOptions;

        public SystemTextJsonHelper(IOptions<MvcOptions> mvcOptions)
        {
            _mvcOptions = mvcOptions.Value;
        }

        /// <inheritdoc />
        public IHtmlContent Serialize(object value)
        {
            // JsonSerializer always encodes
            var json = JsonSerializer.ToString(value, value.GetType(), _mvcOptions.SerializerOptions);
            return new HtmlString(json);
        }
    }
}
