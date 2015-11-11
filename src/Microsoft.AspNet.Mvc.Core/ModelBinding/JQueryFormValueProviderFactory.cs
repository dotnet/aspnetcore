// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class JQueryFormValueProviderFactory : IValueProviderFactory
    {
        public Task<IValueProvider> GetValueProviderAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            
            var request = context.HttpContext.Request;
            if (request.HasFormContentType)
            {
                return CreateValueProviderAsync(request);
            }

            return TaskCache<IValueProvider>.DefaultCompletedTask;
        }

        private static async Task<IValueProvider> CreateValueProviderAsync(HttpRequest request)
        {
            return new JQueryFormValueProvider(
                    BindingSource.Form,
                    await GetValueCollectionAsync(request),
                    CultureInfo.CurrentCulture);
        }

        private static async Task<IDictionary<string, StringValues>> GetValueCollectionAsync(HttpRequest request)
        {
            var formCollection = await request.ReadFormAsync();

            var dictionary = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in formCollection)
            {
                var key = NormalizeJQueryToMvc(entry.Key);
                dictionary[key] = entry.Value;
            }

            return dictionary;
        }

        // This is a helper method for Model Binding over a JQuery syntax.
        // Normalize from JQuery to MVC keys. The model binding infrastructure uses MVC keys.
        // x[] --> x
        // [] --> ""
        // x[12] --> x[12]
        // x[field]  --> x.field, where field is not a number
        private static string NormalizeJQueryToMvc(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            var indexOpen = key.IndexOf('[');
            if (indexOpen == -1)
            {

                // Fast path, no normalization needed.
                // This skips string conversion and allocating the string builder.
                return key;
            }

            var builder = new StringBuilder();
            var position = 0;
            while (position < key.Length)
            {
                if (indexOpen == -1)
                {
                    // No more brackets.
                    builder.Append(key, position, key.Length - position);
                    break;
                }

                builder.Append(key, position, indexOpen - position); // everything up to "["

                // Find closing bracket.
                var indexClose = key.IndexOf(']', indexOpen);
                if (indexClose == -1)
                {
                    throw new ArgumentException(
                        message: Resources.FormatJQueryFormValueProviderFactory_MissingClosingBracket(key),
                        paramName: "key");
                }

                if (indexClose == indexOpen + 1)
                {
                    // Empty brackets signify an array. Just remove.
                }
                else if (char.IsDigit(key[indexOpen + 1]))
                {
                    // Array index. Leave unchanged.
                    builder.Append(key, indexOpen, indexClose - indexOpen + 1);
                }
                else
                {
                    // Field name. Convert to dot notation.
                    builder.Append('.');
                    builder.Append(key, indexOpen + 1, indexClose - indexOpen - 1);
                }

                position = indexClose + 1;
                indexOpen = key.IndexOf('[', position);
            }

            return builder.ToString();
        }
    }
}
