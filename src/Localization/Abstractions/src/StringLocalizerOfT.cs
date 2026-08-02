// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.Localization;

/// <summary>
/// Provides strings for <typeparamref name="TResourceSource"/>.
/// </summary>
/// <typeparam name="TResourceSource">The <see cref="Type"/> to provide strings for.</typeparam>
public class StringLocalizer<TResourceSource> : IStringLocalizer<TResourceSource>
{
    private readonly IStringLocalizer _localizer;

    /// <summary>
    /// Creates a new <see cref="StringLocalizer{TResourceSource}"/>.
    /// </summary>
    /// <param name="factory">The <see cref="IStringLocalizerFactory"/> to use.</param>
    public StringLocalizer(IStringLocalizerFactory factory)
    {
        ArgumentNullThrowHelper.ThrowIfNull(factory);

        _localizer = factory.Create(typeof(TResourceSource));
    }

    /// <inheritdoc />
    public virtual LocalizedString this[string name]
    {
        get
        {
            ArgumentNullThrowHelper.ThrowIfNull(name);

            return _localizer[name];
        }
    }

    /// <inheritdoc />
    public virtual LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            ArgumentNullThrowHelper.ThrowIfNull(name);

            return _localizer[name, arguments];
        }
    }

    /// <inheritdoc />
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        _localizer.GetAllStrings(includeParentCultures);
}
