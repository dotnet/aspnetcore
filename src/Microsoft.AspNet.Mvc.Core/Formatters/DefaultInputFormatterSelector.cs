// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultInputFormatterSelector : IInputFormatterSelector
    {
        public IInputFormatter SelectFormatter(InputFormatterContext context)
        {
            // TODO: https://github.com/aspnet/Mvc/issues/1014
            var formatters = context.ActionContext.InputFormatters;
            foreach (var formatter in formatters)
            {
                if (formatter.CanRead(context))
                {
                    return formatter;
                }
            }

            var request = context.ActionContext.HttpContext.Request;

            // TODO: https://github.com/aspnet/Mvc/issues/458
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                                                              "415: Unsupported content type {0}",
                                                              request.ContentType));
        }
    }
}
