// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApplicationParts;

/// <summary>
/// A part of an MVC application.
/// </summary>
public abstract class ApplicationPart
{
    /// <summary>
    /// Gets the <see cref="ApplicationPart"/> name.
    /// </summary>
    public abstract string Name { get; }
}
