// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Media;

/* This is equivalent to a .razor file containing:
 *
 * <video data-blazor-video
 *        src="@(_currentObjectUrl)"
 *        data-state=@(IsLoading ? "loading" : _hasError ? "error" : null)
 *        @attributes="AdditionalAttributes"
 *        @ref="Element"></video>
 *
 */
/// <summary>
/// A component that efficiently renders video content from non-HTTP sources like byte arrays.
/// </summary>
public sealed class Video : MediaComponentBase
{
    /// <inheritdoc/>
    protected override string TagName => "video";

    /// <inheritdoc/>
    protected override string TargetAttributeName => "src";

    /// <inheritdoc/>
    protected override string MarkerAttributeName => "data-blazor-video";
}
