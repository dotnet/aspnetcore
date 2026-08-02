// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc;

/// <inheritdoc />
/// <typeparam name="TFilter">The <see cref="Type"/> of filter to create.</typeparam>
public class TypeFilterAttribute<TFilter> : TypeFilterAttribute where TFilter : IFilterMetadata
{
    /// <summary>
    /// Instantiates a new <see cref="TypeFilterAttribute"/> instance.
    /// </summary>
    public TypeFilterAttribute() : base(typeof(TFilter)) { }
}
