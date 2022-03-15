// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Defines a contract that represents the result of an HTTP result endpoint
/// that constains an <see cref="Location"/>.
/// </summary>
public interface IAtLocationHttpResult : IResult
{
    /// <summary>
    /// Gets the location at which the status of the requested content can be monitored.
    /// </summary>
    string? Location { get; }
}
