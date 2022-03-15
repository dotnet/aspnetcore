// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Defines a contract that represents the result of an HTTP endpoint
/// that contains an object <see cref="Value"/> and a given statu code.
/// </summary>
public interface IObjectHttpResult : IResult, IStatusCodeHttpResult
{
    /// <summary>
    /// Gets or sets the object result.
    /// </summary>
    object? Value { get; }
}
