// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

public class FunctionInvocationContentBlock : ContentBlock
{
    private FunctionCallContent? _call;

    public FunctionCallContent? Call
    {
        get => _call;
        set
        {
            _call = value;
            // A tool block is identified by its call. Deriving the Id here means generated
            // handlers (which live in the consumer's assembly and cannot set the internal Id
            // setter) don't need to assign it themselves.
            if (value is not null && string.IsNullOrEmpty(Id))
            {
                Id = value.CallId;
            }
        }
    }

    public FunctionResultContent? Result { get; set; }

    public string? ToolName => Call?.Name;

    public IDictionary<string, object?>? Arguments => Call?.Arguments;

    public bool HasResult => Result is not null;
}
