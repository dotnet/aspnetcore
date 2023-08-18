// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        // Finally, persist the state and return the HTML content
        switch (serializationMode)
        {
            case PersistedStateSerializationMode.Server:
                return await PrerenderPersistedStateOnServerAsync();
            case PersistedStateSerializationMode.WebAssembly:
                return await PrerenderPersistedStateOnWebAssemblyAsync();
            default:
                throw new InvalidOperationException("Invalid persistence mode.");
        }
    }

    public async ValueTask<IHtmlContent> PrerenderPersistedStateOnServerAsync()
    {
        var manager = _httpContext.RequestServices.GetRequiredService<ComponentStatePersistenceManager>();
        var protectedStore = new ProtectedPrerenderComponentApplicationStore(_httpContext.RequestServices.GetRequiredService<IDataProtectionProvider>());
        await manager.PersistStateOnServerAsync(protectedStore, Dispatcher);
        return new ComponentStateHtmlContent(protectedStore, PersistedStateSerializationMode.Server);
    }

    public async ValueTask<IHtmlContent> PrerenderPersistedStateOnWebAssemblyAsync()
    {
        var manager = _httpContext.RequestServices.GetRequiredService<ComponentStatePersistenceManager>();
        var store = new PrerenderComponentApplicationStore();
        await manager.PersistStateOnWebAssemblyAsync(store, Dispatcher);
        return new ComponentStateHtmlContent(store, PersistedStateSerializationMode.WebAssembly);
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
        private PrerenderComponentApplicationStore? _store;
        private readonly PersistedStateSerializationMode _serializationMode;

        public static ComponentStateHtmlContent Empty { get; }
            = new ComponentStateHtmlContent(null, PersistedStateSerializationMode.Infer);

        public ComponentStateHtmlContent(PrerenderComponentApplicationStore? store, PersistedStateSerializationMode serializationMode)
        {
            _store = store;
            _serializationMode = serializationMode;
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (_store == null || _store.PersistedState == null)
            {
                return;
            }

            var commentPrefix = _serializationMode switch
            {
                PersistedStateSerializationMode.Server => "<!--Blazor-Server-Component-State:",
                PersistedStateSerializationMode.WebAssembly => "<!--Blazor-WebAssembly-Component-State:",
                _ =>
                    throw new InvalidOperationException("Invalid persistence mode.")
            };

            writer.Write(commentPrefix);
            writer.Write(_store.PersistedState);
            writer.Write("-->");
            _store = null;
        }
    }
}
