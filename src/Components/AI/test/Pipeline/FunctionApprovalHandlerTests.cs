// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Pipeline;

public class FunctionApprovalHandlerTests
{
    [Fact]
    public async Task Process_SeparateUpdatesEmitEveryApproval()
    {
        var pipeline = new BlockMappingPipeline(new UIAgentOptions());

        var first = await ProcessAsync(
            pipeline,
            CreateApprovalUpdate("message-1", "call-1", "delete_file"));
        var second = await ProcessAsync(
            pipeline,
            CreateApprovalUpdate("message-1", "call-2", "send_email"));

        var firstApproval = Assert.IsType<FunctionApprovalBlock>(Assert.Single(first));
        var secondApproval = Assert.IsType<FunctionApprovalBlock>(Assert.Single(second));
        Assert.Equal("call-1", firstApproval.Id);
        Assert.Equal("delete_file", firstApproval.ToolName);
        Assert.Equal(BlockLifecycleState.Inactive, firstApproval.LifecycleState);
        Assert.Equal("call-2", secondApproval.Id);
        Assert.Equal("send_email", secondApproval.ToolName);
    }

    [Fact]
    public async Task Process_CustomFunctionBlockIsNested()
    {
        var options = new UIAgentOptions();
        options.AddBlockHandler(new CustomFunctionHandler());
        var pipeline = new BlockMappingPipeline(options);

        var blocks = await ProcessAsync(
            pipeline,
            CreateApprovalUpdate("message-1", "call-1", "custom_tool"));

        var approval = Assert.IsType<FunctionApprovalBlock>(Assert.Single(blocks));
        var customBlock = Assert.IsType<CustomFunctionBlock>(approval.InnerBlock);
        Assert.IsType<FunctionInvocationContentBlock>(customBlock.InnerBlock);
        Assert.Equal("call-1", approval.Id);
    }

    [Fact]
    public async Task Process_UIActionBlockIsNested()
    {
        var options = new UIAgentOptions();
        options.RegisterUIAction(AIFunctionFactory.Create(
            () => "done",
            "client_tool",
            "Runs in the client."));
        var pipeline = new BlockMappingPipeline(options);

        var blocks = await ProcessAsync(
            pipeline,
            CreateApprovalUpdate("message-1", "call-1", "client_tool"));

        var approval = Assert.IsType<FunctionApprovalBlock>(Assert.Single(blocks));
        Assert.IsType<UIActionBlock>(approval.InnerBlock);
    }

    private static ChatResponseUpdate CreateApprovalUpdate(
        string messageId,
        string callId,
        string toolName)
    {
        var call = new FunctionCallContent(callId, toolName);
        return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = messageId,
            Contents = [new ToolApprovalRequestContent($"request-{callId}", call)],
        };
    }

    private static async Task<List<ContentBlock>> ProcessAsync(
        BlockMappingPipeline pipeline,
        ChatResponseUpdate update)
    {
        var blocks = new List<ContentBlock>();
        await foreach (var block in pipeline.Process(update))
        {
            blocks.Add(block);
        }

        return blocks;
    }

    private sealed class CustomFunctionBlock : FunctionInvocationContentBlock
    {
        public ContentBlock? InnerBlock { get; set; }
    }

    private sealed class CustomFunctionHandler :
        ContentBlockHandler<CustomFunctionBlock>
    {
        public override BlockMappingResult<CustomFunctionBlock> Handle(
            BlockMappingContext context,
            CustomFunctionBlock state)
        {
            foreach (var content in context.UnhandledContents)
            {
                if (content is FunctionCallContent { Name: "custom_tool" } call)
                {
                    context.MarkHandled(call);
                    state.Call = call;
                    state.InnerBlock = context.CreateInnerBlock(
                        new FunctionCallContent("inner-call", "inner-tool"));
                    return BlockMappingResult<CustomFunctionBlock>.Emit(state, state);
                }
            }

            return BlockMappingResult<CustomFunctionBlock>.Pass();
        }
    }
}
