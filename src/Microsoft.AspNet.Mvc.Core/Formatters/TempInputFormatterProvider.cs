// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    public class TempInputFormatterProvider : IInputFormatterProvider
    {
        private IInputFormattersProvider _defaultFormattersProvider;

        public TempInputFormatterProvider([NotNull] IInputFormattersProvider formattersProvider)
        {
            _defaultFormattersProvider = formattersProvider;
        }

        public IInputFormatter GetInputFormatter(InputFormatterProviderContext context)
        {
            var request = context.HttpContext.Request;
            MediaTypeHeaderValue contentType;
            if (!MediaTypeHeaderValue.TryParse(request.ContentType, out contentType))
            {
                // TODO: https://github.com/aspnet/Mvc/issues/458
                throw new InvalidOperationException("400: Bad Request");
            }

            var formatters = _defaultFormattersProvider.InputFormatters;
            foreach (var formatter in formatters)
            {
                var formatterMatched = formatter.SupportedMediaTypes
                                                .Any(supportedMediaType =>
                                                        supportedMediaType.IsSubsetOf(contentType));
                if (formatterMatched)
                {
                    return formatter;
                }
            }

            // TODO: https://github.com/aspnet/Mvc/issues/458
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                                                              "415: Unsupported content type {0}",
                                                              contentType.RawValue));
        }
    }
}
