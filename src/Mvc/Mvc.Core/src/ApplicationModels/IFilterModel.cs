// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// Model that has a list of <see cref="IFilterMetadata"/>.
/// </summary>
public interface IFilterModel
{
    /// <summary>
    /// List of <see cref="IFilterMetadata"/>.
    /// </summary>
    IList<IFilterMetadata> Filters { get; }
}
