// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

/// <inheritdoc />
/// <typeparam name="T">A type which configures a middleware pipeline.</typeparam>
public class MiddlewareFilterAttribute<T> : MiddlewareFilterAttribute
{
    /// <summary>
    /// Instantiates a new instance of <see cref="MiddlewareFilterAttribute"/>.
    /// </summary>
    public MiddlewareFilterAttribute() : base(typeof(T)) { }
}
