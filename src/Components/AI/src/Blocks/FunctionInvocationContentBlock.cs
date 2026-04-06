// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

public class FunctionInvocationContentBlock : ContentBlock
{
    public FunctionCallContent? Call { get; set; }

    public FunctionResultContent? Result { get; set; }

    public string? ToolName => Call?.Name;

    public IDictionary<string, object?>? Arguments => Call?.Arguments;

    public bool HasResult => Result is not null;
}
