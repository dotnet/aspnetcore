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

        var (shouldPrerenderServer, shouldPrerenderWebAssembly) = serializationMode switch
        {
            PersistedStateSerializationMode.Server => (true, false),
            PersistedStateSerializationMode.WebAssembly => (false, true),
            PersistedStateSerializationMode.ServerAndWebAssembly => (true, true),
            var x => throw new InvalidOperationException($"Invalid serialization mode '{x}'."),
        };

        var manager = _httpContext.RequestServices.GetRequiredService<ComponentStatePersistenceManager>();
        PrerenderComponentApplicationStore? serverStore = null;
        PrerenderComponentApplicationStore? webAssemblyStore = null;

        if (shouldPrerenderServer)
        {
            serverStore = new ProtectedPrerenderComponentApplicationStore(_httpContext.RequestServices.GetRequiredService<IDataProtectionProvider>())
            {
                SerializationModeFilter = static serializationMode => serializationMode is PersistedStateSerializationMode.ServerAndWebAssembly or PersistedStateSerializationMode.Server,
            };
            await manager.PersistStateAsync(serverStore, Dispatcher);
        }

        if (shouldPrerenderWebAssembly)
        {
            webAssemblyStore = new PrerenderComponentApplicationStore()
            {
                SerializationModeFilter = static serializationMode => serializationMode is PersistedStateSerializationMode.ServerAndWebAssembly or PersistedStateSerializationMode.WebAssembly,
            };
            await manager.PersistStateAsync(webAssemblyStore, Dispatcher);
        }

        return new ComponentStateHtmlContent(serverStore, webAssemblyStore);
    }

    // Internal for test only
    internal static void UpdateSaveStateRenderMode(HttpContext httpContext, IComponentRenderMode? mode)
    {
        if (ModeEnablesPrerendering(mode))
        {
            var currentInvocation = mode switch
            {
                ServerRenderMode => InvokedRenderModes.Mode.Server,
                WebAssemblyRenderMode => InvokedRenderModes.Mode.WebAssembly,
                AutoRenderMode => InvokedRenderModes.Mode.ServerAndWebAssembly,
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
            if (_serverStore is not null)
            {
                writer.Write("<!--Blazor-Server-Component-State:");
                writer.Write(_serverStore.PersistedState);
                writer.Write("-->");
            }

            if (_webAssemblyStore is not null)
            {
                writer.Write("<!--Blazor-WebAssembly-Component-State:");
                writer.Write(_webAssemblyStore.PersistedState);
                writer.Write("-->");
            }
        }
    }
}
