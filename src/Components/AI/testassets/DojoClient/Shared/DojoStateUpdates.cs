// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using AGUI.Abstractions;
using DojoAgent;
using Microsoft.AspNetCore.Components.AI;
using Microsoft.Extensions.AI;

namespace DojoClient;

internal sealed class DojoStateUpdates
{
    private readonly Dictionary<string, string> _toolNames = new(StringComparer.Ordinal);

    internal IEnumerable<(JsonElement Value, bool IsDelta)> Read(
        StateMapperContext context,
        string? snapshotTool = null,
        string? deltaTool = null)
    {
        switch (context.Update.RawRepresentation)
        {
            case StateSnapshotEvent snapshot:
                yield return (snapshot.Snapshot, false);
                yield break;
            case StateDeltaEvent delta:
                yield return (delta.Delta, true);
                yield break;
            case BaseEvent:
                // AG-UI carries state only through the two events mapped above; any other
                // event has no native state fallback, so applying it again here would
                // duplicate a state update already delivered as an event.
                yield break;
        }

        foreach (var content in context.Update.Contents)
        {
            if (content is FunctionCallContent call &&
                (call.Name == snapshotTool || call.Name == deltaTool))
            {
                _toolNames[call.CallId] = call.Name;
            }
            else if (content is FunctionResultContent result &&
                _toolNames.Remove(result.CallId, out var name))
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
