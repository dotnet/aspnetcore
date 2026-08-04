// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.AI.Tests.TestFramework;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI.Tests.Components;

public class ApprovalBlockSsrTests
{
    [Fact]
    public void ApprovalButtons_HaveFormSubmitAttributes()
    {
        var innerBlock = new FunctionInvocationContentBlock();
        innerBlock.Call = new FunctionCallContent("call-xyz", "delete_file");
        var request = new ToolApprovalRequestContent("req-1", innerBlock.Call);
        var block = new FunctionApprovalBlock(innerBlock, request);
        block.Id = "call-xyz";

        var listContext = new MessageListContext();
        var fragment = listContext.RenderBlock(block);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<FragmentHost>(p =>
        {
            p["Fragment"] = fragment;
        });

        var html = cut.GetHtml();
        Assert.Contains("type=\"submit\"", html);
        Assert.Contains("name=\"BlockAction\"", html);
        Assert.Contains("value=\"approve:call-xyz\"", html);
        Assert.Contains("value=\"reject:call-xyz\"", html);
    }

    [Fact]
    public void ApprovedBlock_DoesNotRenderFormButtons()
    {
        var innerBlock = new FunctionInvocationContentBlock();
        innerBlock.Call = new FunctionCallContent("call-abc", "send_email");
        var request = new ToolApprovalRequestContent("req-2", innerBlock.Call);
        var block = new FunctionApprovalBlock(innerBlock, request);
        block.Id = "call-abc";
        block.Approve();

        var listContext = new MessageListContext();
        var fragment = listContext.RenderBlock(block);

        var renderer = new TestRenderer();
        var cut = renderer.RenderComponent<FragmentHost>(p =>
        {
            p["Fragment"] = fragment;
        });

        var html = cut.GetHtml();
        Assert.DoesNotContain("name=\"BlockAction\"", html);
        Assert.Contains("Approved", html);
    }

    internal class FragmentHost : IComponent
    {
        private RenderHandle _renderHandle;

        [Parameter]
        public RenderFragment? Fragment { get; set; }

        void IComponent.Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        Task IComponent.SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            _renderHandle.Render(b => b.AddContent(0, Fragment));
            return Task.CompletedTask;
        }
    }
}
