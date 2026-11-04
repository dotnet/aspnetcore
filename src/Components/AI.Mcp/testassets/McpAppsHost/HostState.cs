// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace McpAppsHost;

public sealed class HostState
{
    public int ModelRequests;
    public bool ReceivedViewMetadata;
    public bool HoldResponse;
    public IReadOnlyList<string> ModelToolNames { get; set; } = [];
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
