// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc;

/// <inheritdoc />
/// <typeparam name="TFilter">The <see cref="Type"/> of filter to find.</typeparam>
[DebuggerDisplay("Type = {ServiceType}, Order = {Order}")]
public class ServiceFilterAttribute<TFilter> : ServiceFilterAttribute where TFilter : IFilterMetadata
{
    /// <summary>
    /// Instantiates a new <see cref="ServiceFilterAttribute"/> instance.
    /// </summary>
    public ServiceFilterAttribute() : base(typeof(TFilter)) { }
}
