// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Html;

/// <summary>
/// HTML content which can be written asynchronously to a TextWriter.
/// </summary>
public interface IHtmlAsyncContent : IHtmlContent
{
    /// <summary>
    /// Writes the content to the specified <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The <see cref="TextWriter"/> to which the content is written.</param>
    ValueTask WriteToAsync(TextWriter writer);
}
