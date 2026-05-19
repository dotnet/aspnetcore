// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// A <see cref="ParameterDescriptor"/> for bound properties.
/// </summary>
public interface IPropertyInfoParameterDescriptor
{
    /// <summary>
    /// Gets the <see cref="System.Reflection.PropertyInfo"/>.
    /// </summary>
    PropertyInfo PropertyInfo { get; }
}
