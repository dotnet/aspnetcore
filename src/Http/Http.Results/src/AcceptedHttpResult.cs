// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

internal sealed class AcceptedHttpResult : ObjectAtLocationHttpResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptedHttpResult"/> class with the values
    /// provided.
    /// </summary>
    public AcceptedHttpResult()
        : base(location: null, value: null, StatusCodes.Status202Accepted)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptedHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="location">The location at which the status of requested content can be monitored.</param>
    /// <param name="value">The value to format in the entity body.</param>
    public AcceptedHttpResult(string? location, object? value)
        : base(location, value, StatusCodes.Status202Accepted)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptedHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="locationUri">The location at which the status of requested content can be monitored.</param>
    /// <param name="value">The value to format in the entity body.</param>
    public AcceptedHttpResult(Uri locationUri, object? value)
        : base(locationUri, value, StatusCodes.Status202Accepted)
    {
    }
}
