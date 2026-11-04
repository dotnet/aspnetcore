// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.TagHelpers.Cache;

/// <summary>
/// Implements <see cref="IDistributedCacheTagHelperFormatter"/> by serializing the content
/// in UTF8.
/// </summary>
public class DistributedCacheTagHelperFormatter : IDistributedCacheTagHelperFormatter
{
    /// <inheritdoc />
    public Task<byte[]> SerializeAsync(DistributedCacheTagHelperFormattingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

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
        ArgumentNullException.ThrowIfNull(value);

        var content = Encoding.UTF8.GetString(value);
        return Task.FromResult(new HtmlString(content));
    }
}
