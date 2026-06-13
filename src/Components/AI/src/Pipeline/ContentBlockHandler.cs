// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

public abstract class ContentBlockHandler<TState> where TState : new()
{
    public abstract BlockMappingResult<TState> Handle(BlockMappingContext context, TState state);
}
