// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Maps model updates into content blocks. A handler is invoked for every update until it
/// completes the block it emitted.
/// </summary>
/// <typeparam name="TState">The state the handler keeps across updates for a single block.</typeparam>
/// <example>
/// <code>
/// internal sealed class EchoHandler : ContentBlockHandler&lt;RichContentBlock&gt;
/// {
///     public override BlockMappingResult&lt;RichContentBlock&gt; Handle(
///         BlockMappingContext context, RichContentBlock state)
///         => BlockMappingResult&lt;RichContentBlock&gt;.Pass();
/// }
/// </code>
/// </example>
public abstract class ContentBlockHandler<TState> where TState : new()
{
    /// <summary>
    /// Handles an update.
    /// </summary>
    /// <param name="context">The update being mapped.</param>
    /// <param name="state">The state for the block this handler owns.</param>
    /// <returns>The outcome of the invocation.</returns>
    public abstract BlockMappingResult<TState> Handle(BlockMappingContext context, TState state);
}
