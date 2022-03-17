// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Html;

/// <summary>
/// Defines a contract for <see cref="IHtmlContent"/> instances made up of several components which
/// can be copied into an <see cref="IHtmlContentBuilder"/>.
/// </summary>
public interface IHtmlContentContainer : IHtmlContent
{
    /// <summary>
    /// Copies the contained content of this <see cref="IHtmlContentContainer"/> into <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
    void CopyTo(IHtmlContentBuilder builder);

    /// <summary>
    /// <para>
    /// Moves the contained content of this <see cref="IHtmlContentContainer"/> into <paramref name="builder"/>.
    /// </para>
    /// <para>
    /// After <see cref="MoveTo"/> is called, this <see cref="IHtmlContentContainer"/> instance should be left
    /// in an empty state.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="IHtmlContentBuilder"/>.</param>
    void MoveTo(IHtmlContentBuilder builder);
}
