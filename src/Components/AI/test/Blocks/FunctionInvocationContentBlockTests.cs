// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Blocks;

public class FunctionInvocationContentBlockTests
{
    [Fact]
    public void Call_PopulatesInvocationMetadata()
    {
        var arguments = new Dictionary<string, object?> { ["location"] = "Seattle" };
        var call = new FunctionCallContent("call-1", "get_weather", arguments);

        var block = new FunctionInvocationContentBlock { Call = call };

        Assert.Same(call, block.Call);
        Assert.Equal("call-1", block.Id);
        Assert.Equal("get_weather", block.ToolName);
        Assert.Same(arguments, block.Arguments);
        Assert.False(block.HasResult);
    }

    [Fact]
    public void Result_MarksInvocationAsCompleted()
    {
        var result = new FunctionResultContent("call-1", "sunny");
        var block = new FunctionInvocationContentBlock { Result = result };

        Assert.Same(result, block.Result);
        Assert.True(block.HasResult);
    }

    [Fact]
    public void InvocationMetadata_IsNullWithoutCall()
    {
        var block = new FunctionInvocationContentBlock();

        Assert.Null(block.ToolName);
        Assert.Null(block.Arguments);
        Assert.False(block.HasResult);
    }
}
