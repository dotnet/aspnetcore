// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// A <see cref="TextOutputFormatter"/> for simple text content.
/// </summary>
public class StringOutputFormatter : TextOutputFormatter
{
    /// <summary>
    /// Creates a new <see cref="StringOutputFormatter"/> that only supports plain text encoded as <see cref="P:System.Text.Encoding.UTF8" />
    /// or <see cref="P:System.Text.Encoding.Unicode" />.
    /// </summary>
    public StringOutputFormatter()
    {
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
        SupportedMediaTypes.Add("text/plain");
    }

    /// <summary>
    /// Verifies that the object to be formatted is a <see langword="string" /> and proceeds with the standard checks of
    /// <see cref="M:Microsoft.AspNetCore.Mvc.Formatters.OutputFormatter.CanWriteResult(OutputFormatterCanWriteContext)" />.
    /// </summary>
    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ObjectType == typeof(string) || context.Object is string)
        {
            // Call into base to check if the current request's content type is a supported media type.
            return base.CanWriteResult(context);
        }

        return false;
    }

    /// <inheritdoc/>
    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(encoding);

        var valueAsString = (string?)context.Object;
        if (string.IsNullOrEmpty(valueAsString))
        {
            return Task.CompletedTask;
        }

        var response = context.HttpContext.Response;
        return response.WriteAsync(valueAsString, encoding);
    }
}
