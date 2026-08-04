// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Blocks;

public class FunctionInvocationContentBlockTests
{
    [Fact]
    public void ToolName_DelegatesToCallName()
    {
        var block = new FunctionInvocationContentBlock
        {
            Call = new FunctionCallContent("call-1", "GetWeather",
                new Dictionary<string, object?> { ["city"] = "Seattle" })
        };

        Assert.Equal("GetWeather", block.ToolName);
    }

    [Fact]
    public void Arguments_DelegatesToCallArguments()
    {
        var args = new Dictionary<string, object?> { ["city"] = "Seattle" };
        var block = new FunctionInvocationContentBlock
        {
            Call = new FunctionCallContent("call-1", "GetWeather", args)
        };

        Assert.Same(args, block.Arguments);
    }

    [Fact]
    public void HasResult_FalseByDefault()
    {
        var block = new FunctionInvocationContentBlock();
        Assert.False(block.HasResult);
    }

    [Fact]
    public void HasResult_TrueAfterResultSet()
    {
        var block = new FunctionInvocationContentBlock
        {
            Result = new FunctionResultContent("call-1", "sunny")
        };
        Assert.True(block.HasResult);
    }

    [Fact]
    public void ToolName_NullWhenCallNotSet()
    {
        var block = new FunctionInvocationContentBlock();
        Assert.Null(block.ToolName);
    }

    [Fact]
    public void Arguments_NullWhenCallNotSet()
    {
        var block = new FunctionInvocationContentBlock();
        Assert.Null(block.Arguments);
    }
}
