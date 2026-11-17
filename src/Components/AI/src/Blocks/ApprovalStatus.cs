// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Describes the response to a function approval request.
/// </summary>
public enum ApprovalStatus
{
    /// <summary>
    /// The function is waiting for a response.
    /// </summary>
    Pending,

    /// <summary>
    /// The function was approved.
    /// </summary>
    Approved,

    /// <summary>
    /// The function was rejected.
    /// </summary>
    Rejected,
}
