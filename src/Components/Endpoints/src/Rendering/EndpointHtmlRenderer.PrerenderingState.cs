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

    public async ValueTask<IHtmlContent> PrerenderPersistedStateOnFirstRenderAsync(HttpContext httpContext)
    {
        // This is not the first render, do nothing
        if (IsProgressivelyEnhancedNavigation(httpContext.Request))
        {
            return ComponentStateHtmlContent.Empty;
        }

        SetHttpContext(httpContext);

        var manager = _httpContext.RequestServices.GetRequiredService<ComponentStatePersistenceManager>();

        var serverStore = new ProtectedPrerenderComponentApplicationStore(_httpContext.RequestServices.GetRequiredService<IDataProtectionProvider>());
        var webAssemblyStore = new PrerenderComponentApplicationStore();

        // In order to avoid running callbacks for auto render mode twice we use composite store
        var server = new CopyOnlyStore<ServerRenderMode>();
        var auto = new CopyOnlyStore<AutoRenderMode>();
        var webAssembly = new CopyOnlyStore<WebAssemblyRenderMode>();
        var store = new CompositeStore(server, auto, webAssembly);

        await manager.PersistStateAsync(store, this);

        foreach (var kvp in auto.Saved)
        {
            server.Saved.Add(kvp.Key, kvp.Value);
            webAssembly.Saved.Add(kvp.Key, kvp.Value);
        }

        await Task.WhenAll(
            serverStore.PersistStateAsync(server.Saved),
            webAssemblyStore.PersistStateAsync(webAssembly.Saved));

        return new ComponentStateHtmlContent(serverStore, webAssemblyStore);
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
                ServerRenderMode => InvokedRenderModes.Mode.Server,
                WebAssemblyRenderMode => InvokedRenderModes.Mode.WebAssembly,
                AutoRenderMode => throw new NotImplementedException("TODO: To be able to support AutoRenderMode, we have to serialize persisted state in both WebAssembly and Server formats, or unify the two formats."),
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
        ServerRenderMode { Prerender: true } => true,
        WebAssemblyRenderMode { Prerender: true } => true,
        AutoRenderMode { Prerender: true } => true,
        _ => false
    };

    internal static InvokedRenderModes.Mode GetPersistStateRenderMode(HttpContext httpContext)
    {
        return httpContext.Items.TryGetValue(InvokedRenderModesKey, out var result)
            ? ((InvokedRenderModes)result!).Value
            : InvokedRenderModes.Mode.None;
    }

    private sealed class ComponentStateHtmlContent : IHtmlContent
    {
        private readonly PrerenderComponentApplicationStore? _serverStore;
        private readonly PrerenderComponentApplicationStore? _webAssemblyStore;

        public static ComponentStateHtmlContent Empty { get; } = new(null, null);

        public ComponentStateHtmlContent(PrerenderComponentApplicationStore? serverStore, PrerenderComponentApplicationStore? webAssemblyStore)
        {
            _webAssemblyStore = webAssemblyStore;
            _serverStore = serverStore;
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (_serverStore is not null && _serverStore.PersistedState is not null)
            {
                writer.Write("<!--Blazor-Server-Component-State:");
                writer.Write(_serverStore.PersistedState);
                writer.Write("-->");
            }

            if (_webAssemblyStore is not null && _webAssemblyStore.PersistedState is not null)
            {
                writer.Write("<!--Blazor-WebAssembly-Component-State:");
                writer.Write(_webAssemblyStore.PersistedState);
                writer.Write("-->");
            }
        }
    }

    internal class CompositeStore : IPersistentComponentStateStore, IEnumerable<IPersistentComponentStateStore>
    {
        public CompositeStore(
            CopyOnlyStore<ServerRenderMode> server,
            CopyOnlyStore<AutoRenderMode> auto,
            CopyOnlyStore<WebAssemblyRenderMode> webassembly)
        {
            Server = server;
            Auto = auto;
            Webassembly = webassembly;
        }

        public CopyOnlyStore<ServerRenderMode> Server { get; }
        public CopyOnlyStore<AutoRenderMode> Auto { get; }
        public CopyOnlyStore<WebAssemblyRenderMode> Webassembly { get; }

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
