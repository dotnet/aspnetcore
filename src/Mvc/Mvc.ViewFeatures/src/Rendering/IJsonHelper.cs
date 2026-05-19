// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Base JSON helpers.
/// </summary>
public interface IJsonHelper
{
    /// <summary>
    /// Returns serialized JSON for the <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value to serialize as JSON.</param>
    /// <returns>A new <see cref="IHtmlContent"/> containing the serialized JSON.</returns>
    IHtmlContent Serialize(object value);
}
