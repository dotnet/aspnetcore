// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using ModelContextProtocol;

namespace Microsoft.AspNetCore.Components.AI.Mcp;

internal sealed class McpAppFunction(McpAppHost host, McpAppTool binding) : DelegatingAIFunction(binding.Tool)
{
    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var call = FunctionInvokingChatClient.CurrentContext?.CallContent
            ?? throw new InvalidOperationException("MCP app calls require FunctionInvokingChatClient's native function-call identity.");
        if (string.IsNullOrEmpty(call.CallId) || !string.Equals(call.Name, Name, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The native function-call identity does not match the bound MCP tool.");
        }

        var invocation = host.StartInvocation(binding, call);
        using var lifetime = host.CreateOperationToken(cancellationToken);
        try
        {
            var raw = await binding.Tool.CallAsync(arguments,
                options: new RequestOptions { JsonSerializerOptions = binding.Tool.JsonSerializerOptions },
                cancellationToken: lifetime.Token);
            host.CheckResultSize(raw);
            invocation.Complete(raw);
            return McpAppPolicy.ProjectForModel(raw);
        }
        catch (OperationCanceledException)
        {
            invocation.Cancel();
            throw;
        }
        catch (Exception error)
        {
            invocation.Fail(error);
            throw;
        }
    }
}
