// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// A filter that produces the desired content type for the request.
/// </summary>
internal interface IFormatFilter : IFilterMetadata
{
    /// <summary>
    /// Gets the format value for the request associated with the provided <see cref="ActionContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="ActionContext"/> associated with the current request.</param>
    /// <returns>A format value, or <c>null</c> if a format cannot be determined for the request.</returns>
    string? GetFormat(ActionContext context);
}
