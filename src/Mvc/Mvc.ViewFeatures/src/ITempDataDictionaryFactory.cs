// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// A factory which provides access to an <see cref="ITempDataDictionary"/> instance
/// for a request.
/// </summary>
public interface ITempDataDictionaryFactory
{
    /// <summary>
    /// Gets or creates an <see cref="ITempDataDictionary"/> instance for the request associated with the
    /// given <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>
    /// An <see cref="ITempDataDictionary"/> instance for the request associated with the given
    /// <paramref name="context"/>.
    /// </returns>
    ITempDataDictionary GetTempData(HttpContext context);
}
