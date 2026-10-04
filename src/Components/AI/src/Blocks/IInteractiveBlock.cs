// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a content block that pauses a conversation until the user supplies a result.
/// </summary>
public interface IInteractiveBlock
{
    /// <summary>
    /// Waits for the user-supplied result.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the wait.</param>
    /// <returns>The content used to continue the conversation.</returns>
    Task<AIContent> GetResultAsync(CancellationToken cancellationToken = default);
}
