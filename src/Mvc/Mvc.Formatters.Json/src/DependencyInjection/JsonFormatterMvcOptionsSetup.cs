// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class JsonFormatterMvcOptionsSetup : IConfigureOptions<MvcOptions>
    {
        private readonly JsonFormatterOptions _options;

        public JsonFormatterMvcOptionsSetup(IOptions<JsonFormatterOptions> options)
        {
            _options = options.Value;
        }


        public void Configure(MvcOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.InputFormatters.Add(new JsonInputFormatter(_options));
            options.OutputFormatters.Add(new JsonOutputFormatter(_options));
        }
    }
}