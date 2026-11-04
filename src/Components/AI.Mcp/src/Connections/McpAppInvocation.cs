// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Components.AI.Mcp;

internal sealed class McpAppConnection(McpClient client)
{
    public McpClient Client { get; } = client;
    public Dictionary<string, McpAppTool> Tools { get; } = new(StringComparer.Ordinal);
}

internal sealed class McpAppTool(McpAppConnection connection, McpClientTool tool)
{
    public McpAppConnection Connection { get; } = connection;
    public McpClientTool Tool { get; } = tool;
    public string? ResourceUri { get; } = McpAppPolicy.ResourceUri(tool.ProtocolTool);
}

internal sealed class McpAppInvocation(McpAppTool binding, FunctionCallContent call)
{
    private readonly TaskCompletionSource<CallToolResult> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public McpAppTool Binding { get; } = binding;
    public FunctionCallContent Call { get; } = call;
    public Task<CallToolResult> Result => _result.Task;

    public void Complete(CallToolResult result) => _result.TrySetResult(result);
    public void Fail(Exception error) => _result.TrySetException(error);
    public void Cancel() => _result.TrySetCanceled();
}
