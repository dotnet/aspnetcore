// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace AIApp.E2E.Tests.ServiceOverrides;

internal static class LiveChatClientCapture
{
    private const string CaptureEnabledEnvironmentVariable = "COMPONENTS_AI_CAPTURE_LIVE";
    private const string EndpointEnvironmentVariable = "COMPONENTS_AI_AZURE_OPENAI_ENDPOINT";
    private const string DeploymentEnvironmentVariable = "COMPONENTS_AI_AZURE_OPENAI_DEPLOYMENT";

    public static CapturingChatClient Create(
        Func<Uri, string, IChatClient> createClient)
    {
        ArgumentNullException.ThrowIfNull(createClient);

        if (!string.Equals(
            Environment.GetEnvironmentVariable(CaptureEnabledEnvironmentVariable),
            "true",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Live capture is disabled. Set {CaptureEnabledEnvironmentVariable}=true explicitly.");
        }

        var endpoint = Environment.GetEnvironmentVariable(EndpointEnvironmentVariable);
        var deployment = Environment.GetEnvironmentVariable(DeploymentEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(deployment))
        {
            throw new InvalidOperationException(
                $"Live capture requires {EndpointEnvironmentVariable} and {DeploymentEnvironmentVariable}.");
        }

        return new CapturingChatClient(createClient(new Uri(endpoint), deployment));
    }
}
