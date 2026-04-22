// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Tests.Blocks;

public class ReasoningContentBlockTests
{
    [Fact]
    public void AppendText_AccumulatesReasoningTokens()
    {
        var block = new ReasoningContentBlock();
        block.AppendText("Let me think...");
        block.AppendText(" The answer is ");
        block.AppendText("42.");

        Assert.Equal("Let me think... The answer is 42.", block.Text);
    }

    [Fact]
    public void Text_EmptyByDefault()
    {
        var block = new ReasoningContentBlock();
        Assert.Equal(string.Empty, block.Text);
    }

    [Fact]
    public void IsEncrypted_TrueWhenOnlyProtectedData()
    {
        var block = new ReasoningContentBlock
        {
            ProtectedData = "encrypted-blob"
        };
        Assert.True(block.IsEncrypted);
    }

    [Fact]
    public void IsEncrypted_FalseWhenTextPresent()
    {
        var block = new ReasoningContentBlock
        {
            ProtectedData = "encrypted-blob"
        };
        block.AppendText("visible reasoning");
        Assert.False(block.IsEncrypted);
    }

    [Fact]
    public void IsEncrypted_FalseWhenNoProtectedData()
    {
        var block = new ReasoningContentBlock();
        block.AppendText("reasoning");
        Assert.False(block.IsEncrypted);
    }

    [Fact]
    public void IsEncrypted_FalseByDefault()
    {
        var block = new ReasoningContentBlock();
        Assert.False(block.IsEncrypted);
    }
}
