// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.TagHelpers.Cache
{
    /// <summary>
    /// Implements <see cref="IDistributedCacheTagHelperFormatter"/> by serializing the content
    /// in UTF8.
    /// </summary>
    public class DistributedCacheTagHelperFormatter : IDistributedCacheTagHelperFormatter
    {
        /// <inheritdoc />
        public Task<byte[]> SerializeAsync(DistributedCacheTagHelperFormattingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Html == null)
            {
                throw new ArgumentException(
                    Resources.FormatPropertyOfTypeCannotBeNull(
                        nameof(DistributedCacheTagHelperFormattingContext.Html),
                        typeof(DistributedCacheTagHelperFormattingContext).FullName));
            }

            var serialized = Encoding.UTF8.GetBytes(context.Html.ToString());
            return Task.FromResult(serialized);
        }

        /// <inheritdoc />
        public Task<HtmlString> DeserializeAsync(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var content = Encoding.UTF8.GetString(value);
            return Task.FromResult(new HtmlString(content));
        }
    }
}
