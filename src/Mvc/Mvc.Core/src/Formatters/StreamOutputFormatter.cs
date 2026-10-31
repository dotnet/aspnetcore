// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// Always copies the <see cref="T:System.IO.Stream" /> to the response,
/// regardless of requested content type.
/// </summary>
public class StreamOutputFormatter : IOutputFormatter
{
    /// <inheritdoc />
    public bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Ignore the passed in content type, if the object is a Stream.
        if (context.Object is Stream)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task WriteAsync(OutputFormatterWriteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        using (var valueAsStream = ((Stream)context.Object!))
        {
            var response = context.HttpContext.Response;

            if (context.ContentType != null)
            {
                response.ContentType = context.ContentType.ToString();
            }

            await valueAsStream.CopyToAsync(response.Body);
        }
    }
}
