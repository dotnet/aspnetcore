// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

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
            var request = context.ActionContext.HttpContext.Request;
            var formatterContext = new InputFormatterContext(context.ActionContext,
                                                             context.Metadata.ModelType);

            // TODO: https://github.com/aspnet/Mvc/issues/1014
            var formatters = _defaultFormattersProvider.InputFormatters;
            foreach (var formatter in formatters)
            {
                var formatterMatched = formatter.CanRead(formatterContext);
                if (formatterMatched)
                {
                    return formatter;
                }
            }

            // TODO: https://github.com/aspnet/Mvc/issues/458
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                                                              "415: Unsupported content type {0}",
                                                              request.ContentType));
        }
    }
}
