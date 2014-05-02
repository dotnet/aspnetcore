// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class TempInputFormatterProvider : IInputFormatterProvider
    {
        private readonly IInputFormatter[] _formatters;

        public TempInputFormatterProvider(IEnumerable<IInputFormatter> formatters)
        {
            _formatters = formatters.ToArray();
        }

        public IInputFormatter GetInputFormatter(InputFormatterProviderContext context)
        {
            var request = context.HttpContext.Request;
            var contentType = request.GetContentType();
            if (contentType == null)
            {
                // TODO: http exception?
                throw new InvalidOperationException("400: Bad Request");
            }

            for (var i = 0; i < _formatters.Length; i++)
            {
                var formatter = _formatters[i];
                if (formatter.SupportedMediaTypes.Contains(contentType.ContentType, StringComparer.OrdinalIgnoreCase))
                {
                    return formatter;
                }
            }

            // TODO: Http exception
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "415: Unsupported content type {0}", contentType));
        }
    }
}
