// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Provides version hash for a specified file.
/// </summary>
public interface IFileVersionProvider
{
    /// <summary>
    /// Adds version query parameter to the specified file path.
    /// </summary>
    /// <param name="requestPathBase">The base path for the current HTTP request.</param>
    /// <param name="path">The path of the file to which version should be added.</param>
    /// <returns>Path containing the version query string.</returns>
    string AddFileVersionToPath(PathString requestPathBase, string path);
}
