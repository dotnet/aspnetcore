// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Media;

/* This is equivalent to a .razor file containing:
 *
 * <img data-blazor-image
 *      data-state=@(IsLoading ? "loading" : _hasError ? "error" : null)
 *      @ref="Element"
 *      @attributes="AdditionalAttributes" />
 *
 */
/// <summary>
/// A component that efficiently renders images from non-HTTP sources like byte arrays.
/// </summary>
public sealed class Image : MediaComponentBase
{
    /// <inheritdoc/>
    protected override string TagName => "img";

    /// <inheritdoc/>
    protected override string TargetAttributeName => "src";

    /// <inheritdoc/>
    protected override string MarkerAttributeName => "data-blazor-image";
}
