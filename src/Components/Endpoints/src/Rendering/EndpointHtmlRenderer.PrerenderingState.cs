// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class EndpointHtmlRenderer
{
    private static readonly object InvokedRenderModesKey = new object();

    public async ValueTask<IHtmlContent> PrerenderPersistedStateAsync(HttpContext httpContext)
    {
        SetHttpContext(httpContext);

        var manager = _httpContext.RequestServices.GetRequiredService<ComponentStatePersistenceManager>();

        var renderModesMetadata = httpContext.GetEndpoint()?.Metadata.GetMetadata<ConfiguredRenderModesMetadata>();

        IPersistentComponentStateStore? store = null;

        // There is configured render modes metadata, use this to determine where to persist state if possible
        if (renderModesMetadata != null)
        {
            // No render modes are configured, do not persist state
            if (renderModesMetadata.ConfiguredRenderModes.Length == 0)
            {
                return ComponentStateHtmlContent.Empty;
            }

            // Single render mode, no need to perform inference. Any component that tried to render an
            // incompatible render mode would have failed at this point.
            if (renderModesMetadata.ConfiguredRenderModes.Length == 1)
            {
                store = renderModesMetadata.ConfiguredRenderModes[0] switch
                {
                    InteractiveServerRenderMode => new ProtectedPrerenderComponentApplicationStore(_httpContext.RequestServices.GetRequiredService<IDataProtectionProvider>()),
                    InteractiveWebAssemblyRenderMode => new PrerenderComponentApplicationStore(),
                    _ => throw new InvalidOperationException("Invalid configured render mode."),
                };
            }
        }

        if (store != null)
        {
            await manager.PersistStateAsync(store, this);
            return store switch
            {
                ProtectedPrerenderComponentApplicationStore protectedStore => new ComponentStateHtmlContent(protectedStore, null),
                PrerenderComponentApplicationStore prerenderStore => new ComponentStateHtmlContent(null, prerenderStore),
                _ => throw new InvalidOperationException("Invalid store."),
            };
        }
        else
        {
            // We were not able to resolve a store from the configured render modes metadata, we need to capture
            // all possible destinations for the state and persist it in all of them.
            var serverStore = new ProtectedPrerenderComponentApplicationStore(_httpContext.RequestServices.GetRequiredService<IDataProtectionProvider>());
            var webAssemblyStore = new PrerenderComponentApplicationStore();

            // The persistence state manager checks if the store implements
            // IEnumerable<IPersistentComponentStateStore> and if so, it invokes PersistStateAsync on each store
            // for each of the render mode callbacks defined.
            // We pass in a composite store with fake stores for each render mode that only take care of
            // creating a copy of the state for each render mode.
            // Then, we copy the state from the auto store to the server and webassembly stores and persist
            // the real state for server and webassembly render modes.
            // This makes sure that:
            // 1. The persistence state manager is agnostic to the render modes.
            // 2. The callbacks are run only once, even if the state ends up persisted in multiple locations.
            var server = new CopyOnlyStore<InteractiveServerRenderMode>();
            var auto = new CopyOnlyStore<InteractiveAutoRenderMode>();
            var webAssembly = new CopyOnlyStore<InteractiveWebAssemblyRenderMode>();
            store = new CompositeStore(server, auto, webAssembly);

            await manager.PersistStateAsync(store, this);

            foreach (var kvp in auto.Saved)
            {
                server.Saved.Add(kvp.Key, kvp.Value);
                webAssembly.Saved.Add(kvp.Key, kvp.Value);
            }

            // Persist state only if there is state to persist
            var saveServerTask = server.Saved.Count > 0
                ? serverStore.PersistStateAsync(server.Saved)
                : Task.CompletedTask;

            var saveWebAssemblyTask = webAssembly.Saved.Count > 0
                ? webAssemblyStore.PersistStateAsync(webAssembly.Saved)
                : Task.CompletedTask;

            await Task.WhenAll(
                saveServerTask,
                saveWebAssemblyTask);

            // Do not return any HTML content if there is no state to persist for a given mode.
            return new ComponentStateHtmlContent(
                server.Saved.Count > 0 ? serverStore : null,
                webAssembly.Saved.Count > 0 ? webAssemblyStore : null);
        }
    }

    public async ValueTask<IHtmlContent> PrerenderPersistedStateAsync(HttpContext httpContext, PersistedStateSerializationMode serializationMode)
    {
        SetHttpContext(httpContext);

        // First we resolve "infer" mode to a specific mode
        if (serializationMode == PersistedStateSerializationMode.Infer)
        {
            switch (GetPersistStateRenderMode(_httpContext))
            {
                case InvokedRenderModes.Mode.None:
                    return ComponentStateHtmlContent.Empty;
                case InvokedRenderModes.Mode.ServerAndWebAssembly:
                    throw new InvalidOperationException(
                        Resources.FailedToInferComponentPersistenceMode);
                case InvokedRenderModes.Mode.Server:
                    serializationMode = PersistedStateSerializationMode.Server;
                    break;
                case InvokedRenderModes.Mode.WebAssembly:
                    serializationMode = PersistedStateSerializationMode.WebAssembly;
                    break;
                default:
                    throw new InvalidOperationException("Invalid persistence mode");
            }
        }

        var manager = _httpContext.RequestServices.GetRequiredService<ComponentStatePersistenceManager>();

        // Now given the mode, we obtain a particular store for that mode
        // and persist the state and return the HTML content
        switch (serializationMode)
        {
            case PersistedStateSerializationMode.Server:
                var protectedStore = new ProtectedPrerenderComponentApplicationStore(_httpContext.RequestServices.GetRequiredService<IDataProtectionProvider>());
                await manager.PersistStateAsync(protectedStore, this);
                return new ComponentStateHtmlContent(protectedStore, null);

            case PersistedStateSerializationMode.WebAssembly:
                var store = new PrerenderComponentApplicationStore();
                await manager.PersistStateAsync(store, this);
                return new ComponentStateHtmlContent(null, store);

            default:
                throw new InvalidOperationException("Invalid persistence mode.");
        }
    }

    // Internal for test only
    internal static void UpdateSaveStateRenderMode(HttpContext httpContext, IComponentRenderMode? mode)
    {
        // TODO: This will all have to change when we support multiple render modes in the same response
        if (ModeEnablesPrerendering(mode))
        {
            var currentInvocation = mode switch
            {
                InteractiveServerRenderMode => InvokedRenderModes.Mode.Server,
                InteractiveWebAssemblyRenderMode => InvokedRenderModes.Mode.WebAssembly,
                InteractiveAutoRenderMode => throw new NotImplementedException("TODO: To be able to support InteractiveAutoRenderMode, we have to serialize persisted state in both WebAssembly and Server formats, or unify the two formats."),
                _ => throw new ArgumentException(Resources.FormatUnsupportedRenderMode(mode), nameof(mode)),
            };

            if (!httpContext.Items.TryGetValue(InvokedRenderModesKey, out var result))
            {
                httpContext.Items[InvokedRenderModesKey] = new InvokedRenderModes(currentInvocation);
            }
            else
            {
                var invokedMode = (InvokedRenderModes)result!;
                if (invokedMode.Value != currentInvocation)
                {
                    invokedMode.Value = InvokedRenderModes.Mode.ServerAndWebAssembly;
                }
            }
        }
    }

    private static bool ModeEnablesPrerendering(IComponentRenderMode? mode) => mode switch
    {
        InteractiveServerRenderMode { Prerender: true } => true,
        InteractiveWebAssemblyRenderMode { Prerender: true } => true,
        InteractiveAutoRenderMode { Prerender: true } => true,
        _ => false
    };

    internal static InvokedRenderModes.Mode GetPersistStateRenderMode(HttpContext httpContext)
    {
        return httpContext.Items.TryGetValue(InvokedRenderModesKey, out var result)
            ? ((InvokedRenderModes)result!).Value
            : InvokedRenderModes.Mode.None;
    }

    internal sealed class ComponentStateHtmlContent : IHtmlContent
    {
        public static ComponentStateHtmlContent Empty { get; } = new(null, null);

        internal PrerenderComponentApplicationStore? ServerStore { get; }

        internal PrerenderComponentApplicationStore? WebAssemblyStore { get; }

        public ComponentStateHtmlContent(PrerenderComponentApplicationStore? serverStore, PrerenderComponentApplicationStore? webAssemblyStore)
        {
            WebAssemblyStore = webAssemblyStore;
            ServerStore = serverStore;
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (ServerStore is not null && ServerStore.PersistedState is not null)
            {
                writer.Write("<!--Blazor-Server-Component-State:");
                writer.Write(ServerStore.PersistedState);
                writer.Write("-->");
            }

            if (WebAssemblyStore is not null && WebAssemblyStore.PersistedState is not null)
            {
                writer.Write("<!--Blazor-WebAssembly-Component-State:");
                writer.Write(WebAssemblyStore.PersistedState);
                writer.Write("-->");
            }
        }
    }

    internal class CompositeStore : IPersistentComponentStateStore, IEnumerable<IPersistentComponentStateStore>
    {
        public CompositeStore(
            CopyOnlyStore<InteractiveServerRenderMode> server,
            CopyOnlyStore<InteractiveAutoRenderMode> auto,
            CopyOnlyStore<InteractiveWebAssemblyRenderMode> webassembly)
        {
            Server = server;
            Auto = auto;
            Webassembly = webassembly;
        }

        public CopyOnlyStore<InteractiveServerRenderMode> Server { get; }
        public CopyOnlyStore<InteractiveAutoRenderMode> Auto { get; }
        public CopyOnlyStore<InteractiveWebAssemblyRenderMode> Webassembly { get; }

        public IEnumerator<IPersistentComponentStateStore> GetEnumerator()
        {
            yield return Server;
            yield return Auto;
            yield return Webassembly;
        }

        public Task<IDictionary<string, byte[]>> GetPersistedStateAsync() => throw new NotImplementedException();

        public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state) => Task.CompletedTask;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal class CopyOnlyStore<T> : IPersistentComponentStateStore where T : IComponentRenderMode
    {
        public Dictionary<string, byte[]> Saved { get; private set; } = new();

        public Task<IDictionary<string, byte[]>> GetPersistedStateAsync() => throw new NotImplementedException();

        public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
        {
            Saved = new Dictionary<string, byte[]>(state);
            return Task.CompletedTask;
        }

        public bool SupportsRenderMode(IComponentRenderMode renderMode) => renderMode is T;
    }
}
