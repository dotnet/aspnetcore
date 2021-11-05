// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Hosting;

/// <summary>
/// Provides an identifier for a Razor file.
/// </summary>
/// <remarks>
/// This is primarily used to support reloadability during metadata updates and is
/// only available on .cshtml files.
/// </remarks>
public sealed class RazorFileIdentifierAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="RazorFileIdentifierAttribute"/>.
    /// </summary>
    /// <param name="identifier">The file identifier.</param>
    public RazorFileIdentifierAttribute(string identifier)
    {
        Identifier = identifier;
    }

    /// <summary>
    /// Gets the identifier for the file.
    /// </summary>
    public string Identifier { get; }
}
