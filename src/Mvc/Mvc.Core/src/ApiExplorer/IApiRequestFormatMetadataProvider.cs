// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

/// <summary>
/// Provides metadata information about the request format to an <c>IApiDescriptionProvider</c>.
/// </summary>
/// <remarks>
/// An <see cref="Formatters.IInputFormatter"/> should implement this interface to expose metadata information
/// to an <c>IApiDescriptionProvider</c>.
/// </remarks>
public interface IApiRequestFormatMetadataProvider
{
    /// <summary>
    /// Gets a filtered list of content types which are supported by the <see cref="Formatters.IInputFormatter"/>
    /// for the <paramref name="objectType"/> and <paramref name="contentType"/>.
    /// </summary>
    /// <param name="contentType">
    /// The content type for which the supported content types are desired, or <c>null</c> if any content
    /// type can be used.
    /// </param>
    /// <param name="objectType">
    /// The <see cref="Type"/> for which the supported content types are desired.
    /// </param>
    /// <returns>Content types which are supported by the <see cref="Formatters.IInputFormatter"/>.</returns>
    IReadOnlyList<string>? GetSupportedContentTypes(
        string contentType,
        Type objectType);
}
