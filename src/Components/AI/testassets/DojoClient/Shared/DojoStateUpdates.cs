// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using AGUI.Abstractions;
using DojoAgent;
using Microsoft.AspNetCore.Components.AI;
using Microsoft.Extensions.AI;

namespace DojoClient;

internal sealed class DojoStateUpdates(bool isDirect)
{
    private readonly Dictionary<string, string> _toolNames = new(StringComparer.Ordinal);

    internal IEnumerable<(JsonElement Value, bool IsDelta)> Read(
        StateMapperContext context,
        string? snapshotTool = null,
        string? deltaTool = null)
    {
        if (context.Update.RawRepresentation is StateSnapshotEvent snapshot)
        {
            yield return (snapshot.Snapshot, false);
        }
        else if (context.Update.RawRepresentation is StateDeltaEvent delta)
        {
            yield return (delta.Delta, true);
        }

        if (!isDirect)
        {
            yield break;
        }

        foreach (var content in context.Update.Contents)
        {
            if (content is FunctionCallContent call)
            {
                _toolNames[call.CallId] = call.Name;
            }
            else if (content is FunctionResultContent result &&
                _toolNames.Remove(result.CallId, out var name) &&
                (name == snapshotTool || name == deltaTool))
            {
                yield return (
                    JsonSerializer.SerializeToElement(result.Result, AIJsonUtilities.DefaultOptions),
                    name == deltaTool);
            }
            else if (content is DataContent data &&
                data.MediaType == ChatClientAgentFactory.PredictiveStateMediaType)
            {
                context.MarkHandled(content);
                yield return (JsonSerializer.Deserialize<JsonElement>(data.Data.Span), false);
            }
        }
    }
}
