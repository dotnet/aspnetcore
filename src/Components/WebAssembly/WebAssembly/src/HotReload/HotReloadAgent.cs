// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Based on the implementation in https://raw.githubusercontent.com/dotnet/sdk/aad0424c0bfaa60c8bd136a92fd131e53d14561a/src/BuiltInTools/DotNetDeltaApplier/HotReloadAgent.cs

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.HotReload;

[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Hot reload is only expected to work when trimming is disabled.")]
internal sealed class HotReloadAgent : IDisposable
{
    private const string MetadataUpdaterTypeName = "System.Reflection.Metadata.MetadataUpdater";
    private const string ApplyUpdateMethodName = "ApplyUpdate";
    private const string GetCapabilitiesMethodName = "GetCapabilities";

    private delegate void ApplyUpdateDelegate(Assembly assembly, ReadOnlySpan<byte> metadataDelta, ReadOnlySpan<byte> ilDelta, ReadOnlySpan<byte> pdbDelta);

    public AgentReporter Reporter { get; } = new();

    private readonly ConcurrentDictionary<Guid, List<UpdateDelta>> _deltas = new();
    private readonly ConcurrentDictionary<Assembly, Assembly> _appliedAssemblies = new();
    private readonly ApplyUpdateDelegate? _applyUpdate;
    private readonly string? _capabilities;
    private readonly MetadataUpdateHandlerInvoker _metadataUpdateHandlerInvoker;

    public HotReloadAgent()
    {
        _metadataUpdateHandlerInvoker = new(Reporter);

        GetUpdaterMethodsAndCapabilities(out _applyUpdate, out _capabilities);

        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
    }

    public void Dispose()
    {
        AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
    }

    private void GetUpdaterMethodsAndCapabilities(out ApplyUpdateDelegate? applyUpdate, out string? capabilities)
    {
        applyUpdate = null;
        capabilities = null;

        var metadataUpdater = Type.GetType(MetadataUpdaterTypeName + ", System.Runtime.Loader", throwOnError: false);
        if (metadataUpdater == null)
        {
            Reporter.Report($"Type not found: {MetadataUpdaterTypeName}", AgentMessageSeverity.Error);
            return;
        }

        var applyUpdateMethod = metadataUpdater.GetMethod(ApplyUpdateMethodName, BindingFlags.Public | BindingFlags.Static, binder: null, [typeof(Assembly), typeof(ReadOnlySpan<byte>), typeof(ReadOnlySpan<byte>), typeof(ReadOnlySpan<byte>)], modifiers: null);
        if (applyUpdateMethod == null)
        {
            Reporter.Report($"{MetadataUpdaterTypeName}.{ApplyUpdateMethodName} not found.", AgentMessageSeverity.Error);
            return;
        }

        applyUpdate = (ApplyUpdateDelegate)applyUpdateMethod.CreateDelegate(typeof(ApplyUpdateDelegate));

        var getCapabilities = metadataUpdater.GetMethod(GetCapabilitiesMethodName, BindingFlags.NonPublic | BindingFlags.Static, binder: null, Type.EmptyTypes, modifiers: null);
        if (getCapabilities == null)
        {
            Reporter.Report($"{MetadataUpdaterTypeName}.{GetCapabilitiesMethodName} not found.", AgentMessageSeverity.Error);
            return;
        }

        try
        {
            capabilities = getCapabilities.Invoke(obj: null, parameters: null) as string;
        }
        catch (Exception e)
        {
            Reporter.Report($"Error retrieving capabilities: {e.Message}", AgentMessageSeverity.Error);
        }
    }

    public string Capabilities => _capabilities ?? string.Empty;

    private void OnAssemblyLoad(object? _, AssemblyLoadEventArgs eventArgs)
    {
        _metadataUpdateHandlerInvoker.Clear();

        var loadedAssembly = eventArgs.LoadedAssembly;
        var moduleId = TryGetModuleId(loadedAssembly);
        if (moduleId is null)
        {
            return;
        }

        if (_deltas.TryGetValue(moduleId.Value, out var updateDeltas) && _appliedAssemblies.TryAdd(loadedAssembly, loadedAssembly))
        {
            // A delta for this specific Module exists and we haven't called ApplyUpdate on this instance of Assembly as yet.
            ApplyDeltas(loadedAssembly, updateDeltas);
        }
    }

    public IReadOnlyCollection<(string message, AgentMessageSeverity severity)> GetAndClearLogEntries(ResponseLoggingLevel loggingLevel)
        => Reporter.GetAndClearLogEntries(loggingLevel);

    public void ApplyDeltas(IEnumerable<UpdateDelta> deltas)
    {
        Debug.Assert(Capabilities.Length > 0);
        Debug.Assert(_applyUpdate != null);

        foreach (var delta in deltas)
        {
            Reporter.Report($"Applying delta to module {delta.ModuleId}.", AgentMessageSeverity.Verbose);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (TryGetModuleId(assembly) is Guid moduleId && moduleId == delta.ModuleId)
                {
                    _applyUpdate(assembly, delta.MetadataDelta, delta.ILDelta, delta.PdbDelta);
                }
            }

            // Additionally stash the deltas away so it may be applied to assemblies loaded later.
            var cachedDeltas = _deltas.GetOrAdd(delta.ModuleId, static _ => new());
            cachedDeltas.Add(delta);
        }

        _metadataUpdateHandlerInvoker.Invoke(GetMetadataUpdateTypes(deltas));
    }

    private Type[] GetMetadataUpdateTypes(IEnumerable<UpdateDelta> deltas)
    {
        List<Type>? types = null;

        foreach (var delta in deltas)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => TryGetModuleId(assembly) is Guid moduleId && moduleId == delta.ModuleId);
            if (assembly is null)
            {
                continue;
            }

            foreach (var updatedType in delta.UpdatedTypes)
            {
                // Must be a TypeDef.
                Debug.Assert(updatedType >> 24 == 0x02);

                // The type has to be in the manifest module since Hot Reload does not support multi-module assemblies:
                try
                {
                    var type = assembly.ManifestModule.ResolveType(updatedType);
                    types ??= new();
                    types.Add(type);
                }
                catch (Exception e)
                {
                    Reporter.Report($"Failed to load type 0x{updatedType:X8}: {e.Message}", AgentMessageSeverity.Warning);
                }
            }
        }

        return types?.ToArray() ?? Type.EmptyTypes;
    }

    private void ApplyDeltas(Assembly assembly, IReadOnlyList<UpdateDelta> deltas)
    {
        Debug.Assert(_applyUpdate != null);

        try
        {
            foreach (var item in deltas)
            {
                _applyUpdate(assembly, item.MetadataDelta, item.ILDelta, item.PdbDelta);
            }

            Reporter.Report("Deltas applied.", AgentMessageSeverity.Verbose);
        }
        catch (Exception ex)
        {
            Reporter.Report(ex.ToString(), AgentMessageSeverity.Warning);
        }
    }

    private static Guid? TryGetModuleId(Assembly loadedAssembly)
    {
        try
        {
            return loadedAssembly.Modules.FirstOrDefault()?.ModuleVersionId;
        }
        catch
        {
            // Assembly.Modules might throw. See https://github.com/dotnet/aspnetcore/issues/33152
        }

        return default;
    }
}
