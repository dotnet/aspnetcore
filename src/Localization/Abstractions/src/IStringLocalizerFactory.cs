// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Localization;

/// <summary>
/// Represents a factory that creates <see cref="IStringLocalizer"/> instances.
/// </summary>
public interface IStringLocalizerFactory
{
    /// <summary>
    /// Creates an <see cref="IStringLocalizer"/> using the <see cref="System.Reflection.Assembly"/> and
    /// <see cref="Type.FullName"/> of the specified <see cref="Type"/>.
    /// </summary>
    /// <param name="resourceSource">The <see cref="Type"/>.</param>
    /// <returns>The <see cref="IStringLocalizer"/>.</returns>
    IStringLocalizer Create(Type resourceSource);

    /// <summary>
    /// Creates an <see cref="IStringLocalizer"/>.
    /// </summary>
    /// <param name="baseName">The base name of the resource to load strings from.</param>
    /// <param name="location">The location to load resources from.</param>
    /// <returns>The <see cref="IStringLocalizer"/>.</returns>
    IStringLocalizer Create(string baseName, string location);
}
