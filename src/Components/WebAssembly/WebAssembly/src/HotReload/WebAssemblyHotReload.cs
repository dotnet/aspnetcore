// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.DotNet.HotReload;
using Microsoft.JSInterop;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Microsoft.AspNetCore.Components.WebAssembly.HotReload;

/// <summary>
/// Contains methods called by interop. Intended for framework use only, not supported for use in application
/// code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[UnconditionalSuppressMessage(
    "Trimming",
    "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
    Justification = "Hot Reload does not support trimming")]
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
    internal sealed class Update
    {
        public int Id { get; set; }
        public Delta[] Deltas { get; set; } = default!;
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

    private static readonly AgentReporter s_reporter = new();
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private static bool s_initialized;
    private static HotReloadAgent? s_hotReloadAgent;

    internal static async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("__ASPNETCORE_BROWSER_TOOLS") == "true" &&
            OperatingSystem.IsBrowser())
        {
            s_initialized = true;

            if (!HotReloadAgent.TryCreate(s_reporter, out var agent))
            {
                return;
            }

            var existingAgent = Interlocked.CompareExchange(ref s_hotReloadAgent, agent, null);
            if (existingAgent != null)
            {
                throw new InvalidOperationException("Hot Reload agent already initialized");
            }

            await ApplyPreviousDeltasAsync(agent);
        }
    }

    private static async ValueTask ApplyPreviousDeltasAsync(HotReloadAgent agent)
    {
        string errorMessage;

        using var client = new HttpClient()
        {
            BaseAddress = new Uri(WebAssemblyNavigationManager.Instance.BaseUri, UriKind.Absolute)
        };

        try
        {
            var response = await client.GetAsync("/_framework/blazor-hotreload");
            if (response.IsSuccessStatusCode)
            {
                var deltasJson = await response.Content.ReadAsStringAsync();
                var updates = deltasJson != "" ? JsonSerializer.Deserialize<Update[]>(deltasJson, s_jsonSerializerOptions) : null;
                if (updates == null)
                {
                    s_reporter.Report($"No previous updates to apply.", AgentMessageSeverity.Verbose);
                    return;
                }

                var i = 1;
                foreach (var update in updates)
                {
                    s_reporter.Report($"Reapplying update {i}/{updates.Length}.", AgentMessageSeverity.Verbose);

                    agent.ApplyDeltas(
                        update.Deltas.Select(d => new UpdateDelta(Guid.Parse(d.ModuleId, CultureInfo.InvariantCulture), d.MetadataDelta, d.ILDelta, d.PdbDelta, d.UpdatedTypes)));

                    i++;
                }

                return;
            }

            errorMessage = $"HTTP GET '/_framework/blazor-hotreload' returned {response.StatusCode}";
        }
        catch (Exception e)
        {
            errorMessage = e.ToString();
        }

        s_reporter.Report($"Failed to retrieve and apply previous deltas from the server: ${errorMessage}", AgentMessageSeverity.Error);
    }

    private static HotReloadAgent? GetAgent()
        => s_hotReloadAgent ?? (s_initialized ? throw new InvalidOperationException("Hot Reload agent not initialized") : null);

    /// <summary>
    /// For framework use only.
    /// </summary>
    [Obsolete("Use ApplyHotReloadDeltas instead")]
    [JSInvokable(nameof(ApplyHotReloadDelta))]
    public static void ApplyHotReloadDelta(string moduleIdString, byte[] metadataDelta, byte[] ilDelta, byte[] pdbBytes, int[]? updatedTypes)
    {
        GetAgent()?.ApplyDeltas(
            [new UpdateDelta(Guid.Parse(moduleIdString, CultureInfo.InvariantCulture), metadataDelta, ilDelta, pdbBytes, updatedTypes ?? [])]);
    }

    /// <summary>
    /// For framework use only.
    /// </summary>
    [JSInvokable(nameof(ApplyHotReloadDeltas))]
    public static LogEntry[] ApplyHotReloadDeltas(Delta[] deltas, int loggingLevel)
    {
        var agent = GetAgent();

        agent?.ApplyDeltas(
            deltas.Select(d => new UpdateDelta(Guid.Parse(d.ModuleId, CultureInfo.InvariantCulture), d.MetadataDelta, d.ILDelta, d.PdbDelta, d.UpdatedTypes)));

        return s_reporter.GetAndClearLogEntries((ResponseLoggingLevel)loggingLevel)
            .Select(log => new LogEntry() { Message = log.message, Severity = (int)log.severity }).ToArray();
    }

    /// <summary>
    /// For framework use only.
    /// </summary>
    [JSInvokable(nameof(GetApplyUpdateCapabilities))]
    public static string GetApplyUpdateCapabilities()
        => GetAgent()?.Capabilities ?? "";
}
