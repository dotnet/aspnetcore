// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.Extensions.HotReload;
using Microsoft.JSInterop;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.AspNetCore.Components.WebAssembly.HotReload;

/// <summary>
/// Contains methods called by interop. Intended for framework use only, not supported for use in application
/// code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static partial class WebAssemblyHotReload
{
    /// <summary>
    /// For framework use only.
    /// </summary>
    public readonly struct LogEntry
    {
        public string Message { get; init; }
        public int Severity { get; init; }
    }

    /// <summary>
    /// For framework use only.
    /// </summary>
    public readonly struct Delta
    {
        public string ModuleId { get; init; }
        public byte[] MetadataDelta { get; init; }
        public byte[] ILDelta { get; init; }
        public byte[] PdbDelta { get; init; }
        public int[] UpdatedTypes { get; init; }
    }

    private const string BlazorHotReloadModuleName = "blazor-hotreload";

    private static HotReloadAgent? _hotReloadAgent;

    internal static async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("__ASPNETCORE_BROWSER_TOOLS") == "true" &&
            OperatingSystem.IsBrowser())
        {
            var existingAgent = Interlocked.CompareExchange(ref _hotReloadAgent, new HotReloadAgent(), null);
            if (existingAgent != null)
            {
                throw new InvalidOperationException("Already initialized");
            }

            // Attempt to read previously applied hot reload deltas if the ASP.NET Core browser tools are available (indicated by the presence of the Environment variable).
            // The agent is injected in to the hosted app and can serve this script that can provide results from local-storage.
            // See https://github.com/dotnet/aspnetcore/issues/37357#issuecomment-941237000
            await JSHost.ImportAsync(BlazorHotReloadModuleName, "/_framework/blazor-hotreload.js");
            await ReceiveHotReloadAsync();
        }
    }

    private static HotReloadAgent GetAgent()
        => _hotReloadAgent ?? throw new InvalidOperationException("Not initialized");

    /// <summary>
    /// For framework use only.
    /// </summary>
    [Obsolete("Use ApplyHotReloadDeltas instead")]
    [JSInvokable(nameof(ApplyHotReloadDelta))]
    public static void ApplyHotReloadDelta(string moduleIdString, byte[] metadataDelta, byte[] ilDelta, byte[] pdbBytes, int[]? updatedTypes)
    {
        _ = GetAgent().ApplyDeltas(
            [new UpdateDelta(Guid.Parse(moduleIdString, CultureInfo.InvariantCulture), metadataDelta, ilDelta, pdbBytes, updatedTypes ?? [])],
            ResponseLoggingLevel.WarningsAndErrors);
    }

    /// <summary>
    /// For framework use only.
    /// </summary>
    [JSInvokable(nameof(ApplyHotReloadDeltas))]
    public static LogEntry[] ApplyHotReloadDeltas(Delta[] deltas, int loggingLevel)
    {
        return GetAgent().ApplyDeltas(
            deltas.Select(d => new UpdateDelta(Guid.Parse(d.ModuleId, CultureInfo.InvariantCulture), d.MetadataDelta, d.ILDelta, d.PdbDelta, d.UpdatedTypes)), (ResponseLoggingLevel)loggingLevel)
            .Select(log => new LogEntry() { Message = log.message, Severity = (int)log.severity }).ToArray();
    }

    /// <summary>
    /// For framework use only.
    /// </summary>
    [JSInvokable(nameof(GetApplyUpdateCapabilities))]
    public static string GetApplyUpdateCapabilities()
        => GetAgent().Capabilities;

    [JSImport("receiveHotReloadAsync", BlazorHotReloadModuleName)]
    private static partial Task ReceiveHotReloadAsync();
}
