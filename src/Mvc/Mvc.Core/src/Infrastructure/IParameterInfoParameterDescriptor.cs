// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// A <see cref="ParameterDescriptor"/> for action parameters.
/// </summary>
public interface IParameterInfoParameterDescriptor
{
    /// <summary>
    /// Gets the <see cref="System.Reflection.ParameterInfo"/>.
    /// </summary>
    ParameterInfo ParameterInfo { get; }
}
