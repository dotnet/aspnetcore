// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.AI;

internal static partial class BlockMappingPipelineLog
{
    [LoggerMessage(1, LogLevel.Debug, "Processing update: Role={Role}, ContentCount={ContentCount}, ContentTypes=[{ContentTypes}]")]
    internal static partial void ProcessingUpdate(ILogger logger, string? role, int contentCount, string contentTypes);

    [LoggerMessage(2, LogLevel.Debug, "Active handler {HandlerType} returned {ResultKind} for block {BlockId}")]
    internal static partial void ActiveHandlerResult(ILogger logger, string handlerType, string resultKind, string? blockId);

    [LoggerMessage(3, LogLevel.Information, "Emitting new block: {BlockType} Id={BlockId} Role={Role}")]
    internal static partial void EmittingBlock(ILogger logger, string blockType, string? blockId, string? role);

    [LoggerMessage(4, LogLevel.Debug, "Phase 2: Trying inactive handler {HandlerType} to claim remaining content")]
    internal static partial void TryingInactiveHandler(ILogger logger, string handlerType);

    [LoggerMessage(5, LogLevel.Debug, "Finalize: Deactivating {ActiveCount} active blocks")]
    internal static partial void Finalizing(ILogger logger, int activeCount);

    [LoggerMessage(6, LogLevel.Debug, "All content handled after active handlers, skipping Phase 2")]
    internal static partial void AllContentHandledAfterPhase1(ILogger logger);
}
