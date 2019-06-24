// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal class CircuitPrerenderer : IComponentPrerenderer
    {
        private static object CircuitHostKey = new object();
        private static object CancellationStatusKey = new object();
        private static readonly JsonSerializerOptions _jsonSerializationOptions =
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly CircuitFactory _circuitFactory;
        private readonly CircuitRegistry _registry;

        public CircuitPrerenderer(
            CircuitFactory circuitFactory,
            CircuitRegistry registry)
        {
            _circuitFactory = circuitFactory;
            _registry = registry;
        }

        public async Task<ComponentPrerenderResult> PrerenderComponentAsync(ComponentPrerenderingContext prerenderingContext)
        {
            var context = prerenderingContext.Context;
            var cancellationStatus = GetOrCreateCancellationStatus(context);
            if (cancellationStatus.Canceled)
            {
                // Avoid creating a circuit host if other component earlier in the pipeline already triggered
                // cancellation (e.g., by navigating or throwing). Instead render nothing.
                return new ComponentPrerenderResult(Array.Empty<string>());
            }
            var circuitHost = GetOrCreateCircuitHost(context, cancellationStatus);
            ComponentRenderedText renderResult;
            try
            {
                renderResult = await circuitHost.PrerenderComponentAsync(
                    prerenderingContext.ComponentType,
                    prerenderingContext.Parameters);
            }
            catch (NavigationException navigationException)
            {
                // Cleanup the state as we won't need it any longer.
                // Signal callbacks that we don't have to register the circuit.
                await CleanupCircuitState(context, cancellationStatus, circuitHost);

                // Navigation was attempted during prerendering.
                if (prerenderingContext.Context.Response.HasStarted)
                {
                    // We can't perform a redirect as the server already started sending the response.
                    // This is considered an application error as the developer should buffer the response until
                    // all components have rendered.
                    throw new InvalidOperationException("A navigation command was attempted during prerendering after the server already started sending the response. " +
                        "Navigation commands can not be issued during server-side prerendering after the response from the server has started. Applications must buffer the" +
                        "reponse and avoid using features like FlushAsync() before all components on the page have been rendered to prevent failed navigation commands.", navigationException);
                }

                context.Response.Redirect(navigationException.Location);
                return new ComponentPrerenderResult(Array.Empty<string>());
            }
            catch
            {
                // If prerendering any component fails, cancel prerendering entirely and dispose the DI scope
                await CleanupCircuitState(context, cancellationStatus, circuitHost);
                throw;
            }

            circuitHost.Descriptors.Add(new ComponentDescriptor
            {
                ComponentType = prerenderingContext.ComponentType,
                Prerendered = true
            });

            var record = JsonSerializer.ToString(new PrerenderedComponentRecord(
                    circuitHost.CircuitId.Replace("--", ".."), // We need to do this due to the fact that -- is not allowed within HTML comments and HTML doesn't encode '-'.
                    circuitHost.Renderer.Id,
                    renderResult.ComponentId),
                _jsonSerializationOptions);

            var result = (new[] {
                $"<!-- M.A.C.Component: {record} -->",
            }).Concat(renderResult.Tokens).Concat(
                new[] {
                    $"<!-- M.A.C.Component: {renderResult.ComponentId} -->"
                });

            return new ComponentPrerenderResult(result);
        }

        private PrerenderingCancellationStatus GetOrCreateCancellationStatus(HttpContext context)
        {
            if (context.Items.TryGetValue(CancellationStatusKey, out var existingValue))
            {
                return (PrerenderingCancellationStatus)existingValue;
            }
            else
            {
                var cancellationStatus = new PrerenderingCancellationStatus();
                context.Items[CancellationStatusKey] = cancellationStatus;
                return cancellationStatus;
            }
        }

        private static async Task CleanupCircuitState(HttpContext context, PrerenderingCancellationStatus cancellationStatus, CircuitHost circuitHost)
        {
            cancellationStatus.Canceled = true;
            context.Items.Remove(CircuitHostKey);
            await circuitHost.DisposeAsync();
        }

        private CircuitHost GetOrCreateCircuitHost(HttpContext context, PrerenderingCancellationStatus cancellationStatus)
        {
            if (context.Items.TryGetValue(CircuitHostKey, out var existingHost))
            {
                return (CircuitHost)existingHost;
            }
            else
            {
                var result = _circuitFactory.CreateCircuitHost(
                    context,
                    client: new CircuitClientProxy(), // This creates an "offline" client.
                    GetFullUri(context.Request),
                    GetFullBaseUri(context.Request));

                result.UnhandledException += CircuitHost_UnhandledException;
                context.Response.OnCompleted(() =>
                {
                    result.UnhandledException -= CircuitHost_UnhandledException;
                    if (!cancellationStatus.Canceled)
                    {
                        _registry.RegisterDisconnectedCircuit(result);
                    }

                    return Task.CompletedTask;
                });
                context.Items.Add(CircuitHostKey, result);

                return result;
            }
        }

        private void CircuitHost_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Throw all exceptions encountered during pre-rendering so the default developer
            // error page can respond.
            ExceptionDispatchInfo.Capture((Exception)e.ExceptionObject).Throw();
        }

        private string GetFullUri(HttpRequest request)
        {
            return UriHelper.BuildAbsolute(
                request.Scheme,
                request.Host,
                request.PathBase,
                request.Path,
                request.QueryString);
        }

        private string GetFullBaseUri(HttpRequest request)
        {
            var result = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase);

            // PathBase may be "/" or "/some/thing", but to be a well-formed base URI
            // it has to end with a trailing slash
            if (!result.EndsWith('/'))
            {
                result += '/';
            }

            return result;
        }

        private struct PrerenderedComponentRecord
        {
            public PrerenderedComponentRecord(string circuitId, int rendererId, int componentId)
            {
                CircuitId = circuitId;
                RendererId = rendererId;
                ComponentId = componentId;
            }

            public string CircuitId { get; }

            public int RendererId { get; }

            public int ComponentId { get; }
        }

        private class PrerenderingCancellationStatus
        {
            public bool Canceled { get; set; }
        }
    }
}
