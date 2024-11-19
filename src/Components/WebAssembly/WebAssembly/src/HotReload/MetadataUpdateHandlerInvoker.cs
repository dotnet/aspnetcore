// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.DotNet.HotReload;

/// <summary>
/// Finds and invokes metadata update handlers.
/// </summary>
#if NET
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Hot reload is only expected to work when trimming is disabled.")]
[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Hot reload is only expected to work when trimming is disabled.")]
#endif
internal sealed class MetadataUpdateHandlerInvoker(AgentReporter reporter)
{
    internal sealed class RegisteredActions(IReadOnlyList<Action<Type[]?>> clearCache, IReadOnlyList<Action<Type[]?>> updateApplication)
    {
        public void Invoke(Type[] updatedTypes)
        {
            foreach (var action in clearCache)
            {
                action(updatedTypes);
            }

            foreach (var action in updateApplication)
            {
                action(updatedTypes);
            }
        }

        /// <summary>
        /// For testing.
        /// </summary>
        internal IEnumerable<Action<Type[]?>> ClearCache => clearCache;

        /// <summary>
        /// For testing.
        /// </summary>
        internal IEnumerable<Action<Type[]?>> UpdateApplication => updateApplication;
    }

    private const string ClearCacheHandlerName = "ClearCache";
    private const string UpdateApplicationHandlerName = "UpdateApplication";

    private RegisteredActions? _actions;

    /// <summary>
    /// Call when a new assembly is loaded.
    /// </summary>
    internal void Clear()
        => Interlocked.Exchange(ref _actions, null);

    /// <summary>
    /// Invokes all registerd handlers.
    /// </summary>
    internal void Invoke(Type[] updatedTypes)
    {
        try
        {
            // Defer discovering metadata updata handlers until after hot reload deltas have been applied.
            // This should give enough opportunity for AppDomain.GetAssemblies() to be sufficiently populated.
            var actions = _actions;
            if (actions == null)
            {
                Interlocked.CompareExchange(ref _actions, GetMetadataUpdateHandlerActions(), null);
                actions = _actions;
            }

            reporter.Report($"Invoking metadata update handlers. {updatedTypes.Length} type(s) updated.", AgentMessageSeverity.Verbose);

            actions.Invoke(updatedTypes);

            reporter.Report("Deltas applied.", AgentMessageSeverity.Verbose);
        }
        catch (Exception e)
        {
            reporter.Report(e.ToString(), AgentMessageSeverity.Warning);
        }
    }

    private IEnumerable<Type> GetHandlerTypes()
    {
        // We need to execute MetadataUpdateHandlers in a well-defined order. For v1, the strategy that is used is to topologically
        // sort assemblies so that handlers in a dependency are executed before the dependent (e.g. the reflection cache action
        // in System.Private.CoreLib is executed before System.Text.Json clears its own cache.)
        // This would ensure that caches and updates more lower in the application stack are up to date
        // before ones higher in the stack are recomputed.
        var sortedAssemblies = TopologicalSort(AppDomain.CurrentDomain.GetAssemblies());

        foreach (var assembly in sortedAssemblies)
        {
            foreach (var attr in TryGetCustomAttributesData(assembly))
            {
                // Look up the attribute by name rather than by type. This would allow netstandard targeting libraries to
                // define their own copy without having to cross-compile.
                if (attr.AttributeType.FullName != "System.Reflection.Metadata.MetadataUpdateHandlerAttribute")
                {
                    continue;
                }

                IList<CustomAttributeTypedArgument> ctorArgs = attr.ConstructorArguments;
                if (ctorArgs.Count != 1 ||
                    ctorArgs[0].Value is not Type handlerType)
                {
                    reporter.Report($"'{attr}' found with invalid arguments.", AgentMessageSeverity.Warning);
                    continue;
                }

                yield return handlerType;
            }
        }
    }

    public RegisteredActions GetMetadataUpdateHandlerActions()
        => GetMetadataUpdateHandlerActions(GetHandlerTypes());

    /// <summary>
    /// Internal for testing.
    /// </summary>
    internal RegisteredActions GetMetadataUpdateHandlerActions(IEnumerable<Type> handlerTypes)
    {
        var clearCacheActions = new List<Action<Type[]?>>();
        var updateApplicationActions = new List<Action<Type[]?>>();

        foreach (var handlerType in handlerTypes)
        {
            bool methodFound = false;

            if (GetUpdateMethod(handlerType, ClearCacheHandlerName) is MethodInfo clearCache)
            {
                clearCacheActions.Add(CreateAction(clearCache));
                methodFound = true;
            }

            if (GetUpdateMethod(handlerType, UpdateApplicationHandlerName) is MethodInfo updateApplication)
            {
                updateApplicationActions.Add(CreateAction(updateApplication));
                methodFound = true;
            }

            if (!methodFound)
            {
                reporter.Report(
                    $"Expected to find a static method '{ClearCacheHandlerName}' or '{UpdateApplicationHandlerName}' on type '{handlerType.AssemblyQualifiedName}' but neither exists.",
                    AgentMessageSeverity.Warning);
            }
        }

        return new RegisteredActions(clearCacheActions, updateApplicationActions);

        Action<Type[]?> CreateAction(MethodInfo update)
        {
            var action = (Action<Type[]?>)update.CreateDelegate(typeof(Action<Type[]?>));
            return types =>
            {
                try
                {
                    action(types);
                }
                catch (Exception ex)
                {
                    reporter.Report($"Exception from '{action}': {ex}", AgentMessageSeverity.Warning);
                }
            };
        }

        MethodInfo? GetUpdateMethod(Type handlerType, string name)
        {
            if (handlerType.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, binder: null, [typeof(Type[])], modifiers: null) is MethodInfo updateMethod &&
                updateMethod.ReturnType == typeof(void))
            {
                return updateMethod;
            }

            foreach (MethodInfo method in handlerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                if (method.Name == name)
                {
                    reporter.Report($"Type '{handlerType}' has method '{method}' that does not match the required signature.", AgentMessageSeverity.Warning);
                    break;
                }
            }

            return null;
        }
    }

    private IList<CustomAttributeData> TryGetCustomAttributesData(Assembly assembly)
    {
        try
        {
            return assembly.GetCustomAttributesData();
        }
        catch (Exception e)
        {
            // In cross-platform scenarios, such as debugging in VS through WSL, Roslyn
            // runs on Windows, and the agent runs on Linux. Assemblies accessible to Windows
            // may not be available or loaded on linux (such as WPF's assemblies).
            // In such case, we can ignore the assemblies and continue enumerating handlers for
            // the rest of the assemblies of current domain.
            reporter.Report($"'{assembly.FullName}' is not loaded ({e.Message})", AgentMessageSeverity.Verbose);
            return [];
        }
    }

    /// <summary>
    /// Internal for testing.
    /// </summary>
    internal static List<Assembly> TopologicalSort(Assembly[] assemblies)
    {
        var sortedAssemblies = new List<Assembly>(assemblies.Length);

        var visited = new HashSet<string>(StringComparer.Ordinal);

        foreach (var assembly in assemblies)
        {
            Visit(assemblies, assembly, sortedAssemblies, visited);
        }

        static void Visit(Assembly[] assemblies, Assembly assembly, List<Assembly> sortedAssemblies, HashSet<string> visited)
        {
            var assemblyIdentifier = assembly.GetName().Name!;
            if (!visited.Add(assemblyIdentifier))
            {
                return;
            }

            foreach (var dependencyName in assembly.GetReferencedAssemblies())
            {
                var dependency = Array.Find(assemblies, a => a.GetName().Name == dependencyName.Name);
                if (dependency is not null)
                {
                    Visit(assemblies, dependency, sortedAssemblies, visited);
                }
            }

            sortedAssemblies.Add(assembly);
        }

        return sortedAssemblies;
    }
}
