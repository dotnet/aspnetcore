// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Represents data used to build an <c>ApiDescription</c>, stored as part of the
/// <see cref="Abstractions.ActionDescriptor.Properties"/>.
/// </summary>
public class ApiDescriptionActionData
{
    /// <summary>
    /// The <c>ApiDescription.GroupName</c> of <c>ApiDescription</c> objects for the associated
    /// action.
    /// </summary>
    public string? GroupName { get; set; }
}
