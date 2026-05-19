// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: This file is copied from src/Middleware/StaticFiles/src/FileExtensionContentTypeProvider.cs
// and made internal with a namespace change.
// It can't be referenced directly from the StaticFiles package because that would cause this package to require
// Microsoft.AspNetCore.App, thus preventing it from being used anywhere ASP.NET Core isn't supported (such as
// various platforms that .NET MAUI runs on, such as Android and iOS).

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.WebView;

/// <summary>
/// Used to look up MIME types given a file path
/// </summary>
internal interface IContentTypeProvider
{
    /// <summary>
    /// Given a file path, determine the MIME type
    /// </summary>
    /// <param name="subpath">A file path</param>
    /// <param name="contentType">The resulting MIME type</param>
    /// <returns>True if MIME type could be determined</returns>
    bool TryGetContentType(string subpath, [MaybeNullWhen(false)] out string contentType);
}
