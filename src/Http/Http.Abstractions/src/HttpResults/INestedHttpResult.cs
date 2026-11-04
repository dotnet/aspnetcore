// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Defines a contract that represents the result of an HTTP endpoint
/// that contains a nested <see cref="IResult"/> type.
/// </summary>
/// <remarks>For example, <c>Results&lt;TResult1, TResult2&gt;</c> is an <see cref="INestedHttpResult"/> and will contain the returned <see cref="IResult"/>.</remarks>
public interface INestedHttpResult
{
    /// <summary>
    /// Gets the actual <see cref="IResult"/> returned by the <see cref="Endpoint"/> route handler delegate.
    /// </summary>
    IResult Result { get; }
}
