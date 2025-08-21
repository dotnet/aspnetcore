// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Media;

/// <summary>
/// A component that renders video content from non-HTTP sources (byte arrays or streams)
/// by materializing an in-memory blob and assigning its object URL to the underlying
/// <c>&lt;video&gt;</c> element.
/// </summary>
/// <remarks>
/// This component uses the same client-side pipeline as <see cref="Image"/> and therefore
/// reads the full content into memory to create a blob URL. It is not suitable for large or
/// truly streaming video scenarios. Use browser-native streaming approaches if you require
/// progressive playback.
///
/// To configure common video attributes like <c>controls</c>, <c>autoplay</c>, <c>muted</c>,
/// or <c>loop</c>, pass them through <see cref="MediaComponentBase.AdditionalAttributes"/>.
/// </remarks>
public sealed class Video : MediaComponentBase
{
    /// <inheritdoc/>
    protected override string TagName => "video";

    /// <inheritdoc/>
    protected override string TargetAttributeName => "src";

    /// <inheritdoc/>
    protected override string MarkerAttributeName => "data-blazor-video";
}
