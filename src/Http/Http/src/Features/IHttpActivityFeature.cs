// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Feature to access the <see cref="Activity"/> associated with a request.
/// </summary>
public interface IHttpActivityFeature
{
    /// <summary>
    /// Returns the <see cref="Activity"/> associated with the current request.
    /// </summary>
    Activity Activity { get; set; }
}
