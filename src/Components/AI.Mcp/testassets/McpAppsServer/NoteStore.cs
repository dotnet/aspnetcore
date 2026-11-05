// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace McpAppsServer;

public sealed class NoteStore(IConfiguration configuration)
{
    public Uri Origin { get; } = new(configuration["urls"]
        ?? throw new InvalidOperationException("The server URL is unavailable."));
    public string SocketOrigin => new UriBuilder(Origin)
    {
        Scheme = Origin.Scheme == Uri.UriSchemeHttps ? "wss" : "ws",
    }.Uri.GetLeftPart(UriPartial.Authority);

    public int InitialCalls;
    public int ActiveInitialCalls;
    public int CancelledInitialCalls;
    public int SavedCalls;
    public int ActiveCalls;
    public int CancelledCalls;
    public int ModelOnlyCalls;
    public int AssetRequests;
    public int SocketRequests;
    public int ResourceReads;
    public string Payload { get; } = configuration.GetValue<bool>("LARGE_PAYLOADS")
        ? new string('x', 2 * 1024 * 1024 + 1) : "";
    public bool HoldResource { get; } = configuration.GetValue<bool>("HOLD_RESOURCE");
    public TaskCompletionSource ResourceRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public string Message = "Hello from a standard app.";
    public TaskCompletionSource Saved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
