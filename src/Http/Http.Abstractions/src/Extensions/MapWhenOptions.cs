// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder.Extensions;

/// <summary>
/// Options for the <see cref="MapWhenMiddleware"/>.
/// </summary>
public class MapWhenOptions
{
    private Func<HttpContext, bool>? _predicate;

    /// <summary>
    /// The user callback that determines if the branch should be taken.
    /// </summary>
    public Func<HttpContext, bool>? Predicate
    {
        get
        {
            return _predicate;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _predicate = value;
        }
    }

    /// <summary>
    /// The branch taken for a positive match.
    /// </summary>
    public RequestDelegate? Branch { get; set; }
}
