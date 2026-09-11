// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI;
using Microsoft.Extensions.AI;

namespace ComponentsAIClaimApp.Data;

internal sealed class ClaimErrorContentHandler(ILogger logger)
    : ContentBlockHandler<ClaimErrorContentHandler.HandlerState>
{
    public const string DefaultMessage = "We couldn't complete the assessment. Please try again.";

    public override BlockMappingResult<HandlerState> Handle(
        BlockMappingContext context,
        HandlerState state)
    {
        foreach (var content in context.UnhandledContents)
        {
            if (content is ErrorContent error)
            {
                context.MarkHandled(content);
                logger.LogError(
                    "AG-UI claim assessment failed with code {ErrorCode}: {ErrorMessage}",
                    error.ErrorCode,
                    error.Message);
                throw new InvalidOperationException(DefaultMessage);
            }
        }

        return BlockMappingResult<HandlerState>.Pass();
    }

    internal sealed class HandlerState
    {
    }
}
