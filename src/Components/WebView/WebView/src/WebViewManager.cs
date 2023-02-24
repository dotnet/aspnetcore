// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.StaticWebAssets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Components.WebView;

/// <summary>
/// Manages activities within a web view that hosts Razor components. Platform authors
/// should subclass this to wire up the abstract and protected methods to the APIs of
/// the platform's web view.
/// </summary>
public abstract class WebViewManager : IAsyncDisposable
{
    // These services are not DI services, because their lifetime isn't limited to a single
    // per-page-load scope. Instead, their lifetime matches the webview itself.
    private readonly IServiceProvider _provider;
    private readonly Dispatcher _dispatcher;
    private readonly IpcSender _ipcSender;
    private readonly IpcReceiver _ipcReceiver;
    private readonly Uri _appBaseUri;
    private readonly StaticContentProvider _staticContentProvider;
    private readonly JSComponentConfigurationStore _jsComponents;
    private readonly Dictionary<string, RootComponent> _rootComponentsBySelector = new();

    // Each time a web page connects, we establish a new per-page context
    private PageContext _currentPageContext;
    private bool _disposed;

    /// <summary>
    /// Constructs an instance of <see cref="WebViewManager"/>.
    /// </summary>
    /// <param name="provider">The <see cref="IServiceProvider"/> for the application.</param>
    /// <param name="dispatcher">A <see cref="Dispatcher"/> instance that can marshal calls to the required thread or sync context.</param>
    /// <param name="appBaseUri">The base URI for the application. Since this is a webview, the base URI is typically on a private origin such as http://0.0.0.0/ or app://example/</param>
    /// <param name="fileProvider">Provides static content to the webview.</param>
    /// <param name="jsComponents">Describes configuration for adding, removing, and updating root components from JavaScript code.</param>
    /// <param name="hostPageRelativePath">Path to the host page within the <paramref name="fileProvider"/>.</param>
    public WebViewManager(IServiceProvider provider, Dispatcher dispatcher, Uri appBaseUri, IFileProvider fileProvider, JSComponentConfigurationStore jsComponents, string hostPageRelativePath)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _appBaseUri = EnsureTrailingSlash(appBaseUri ?? throw new ArgumentNullException(nameof(appBaseUri)));
        fileProvider = StaticWebAssetsLoader.UseStaticWebAssets(fileProvider);
        _jsComponents = jsComponents;
        _staticContentProvider = new StaticContentProvider(fileProvider, appBaseUri, hostPageRelativePath);
        _ipcSender = new IpcSender(_dispatcher, SendMessage);
        _ipcReceiver = new IpcReceiver(AttachToPageAsync);
    }

    /// <summary>
    /// Gets the <see cref="Dispatcher"/> used by this <see cref="WebViewManager"/> instance.
    /// </summary>
    public Dispatcher Dispatcher => _dispatcher;

    /// <summary>
    /// Instructs the web view to navigate to the specified location, bypassing any
    /// client-side routing.
    /// </summary>
    /// <param name="url">The URL, which may be absolute or relative to the application root.</param>
    public void Navigate([StringSyntax(StringSyntaxAttribute.Uri)] string url)
        => NavigateCore(new Uri(_appBaseUri, url));

    /// <summary>
    /// Instructs the web view to navigate to the specified location, bypassing any
    /// client-side routing.
    /// </summary>
    /// <param name="absoluteUri">The absolute URI.</param>
    protected abstract void NavigateCore(Uri absoluteUri);

    /// <summary>
    /// Sends a message to JavaScript code running in the attached web view. This must
    /// be forwarded to the Blazor JavaScript code.
    /// </summary>
    /// <param name="message">The message.</param>
    protected abstract void SendMessage(string message);

    /// <summary>
    /// Adds a root component to the attached page.
    /// </summary>
    /// <param name="componentType">The type of the root component. This must implement <see cref="IComponent"/>.</param>
    /// <param name="selector">The CSS selector describing where in the page the component should be placed.</param>
    /// <param name="parameters">Parameters for the component.</param>
    public Task AddRootComponentAsync(Type componentType, string selector, ParameterView parameters)
    {
        var rootComponent = new RootComponent { ComponentType = componentType, Parameters = parameters };
        if (!_rootComponentsBySelector.TryAdd(selector, rootComponent))
        {
            throw new InvalidOperationException($"There is already a root component with selector '{selector}'.");
        }

        // If the page is already attached, add the root component to it now. Otherwise we'll
        // add it when the page attaches later.
        if (_currentPageContext != null)
        {
            return Dispatcher.InvokeAsync(() =>
            {
                rootComponent.ComponentId = _currentPageContext.Renderer.AddRootComponent(
                    componentType, selector);
                return _currentPageContext.Renderer.RenderRootComponentAsync(
                    rootComponent.ComponentId.Value, rootComponent.Parameters);
            });
        }
        else
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Removes a previously-attached root component from the current page.
    /// </summary>
    /// <param name="selector">The CSS selector describing where in the page the component was placed. This must exactly match the selector provided on an earlier call to <see cref="AddRootComponentAsync(Type, string, ParameterView)"/>.</param>
    public Task RemoveRootComponentAsync(string selector)
    {
        if (!_rootComponentsBySelector.Remove(selector, out var rootComponent))
        {
            throw new InvalidOperationException($"There is no root component with selector '{selector}'.");
        }

        // If the page is already attached, remove the root component from it now. Otherwise it's
        // enough to have updated the dictionary.
        if (_currentPageContext != null && rootComponent.ComponentId.HasValue)
        {
            return Dispatcher.InvokeAsync(() => _currentPageContext.Renderer.RemoveRootComponent(rootComponent.ComponentId.Value));
        }
        else
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Notifies the <see cref="WebViewManager"/> about a message from JavaScript running within the web view.
    /// </summary>
    /// <param name="sourceUri">The source URI for the message.</param>
    /// <param name="message">The message.</param>
    protected void MessageReceived(Uri sourceUri, string message)
    {
        if (!_appBaseUri.IsBaseOf(sourceUri))
        {
            // It's important that we ignore messages from other origins, otherwise if the webview
            // navigates to a remote location, it could send commands that execute locally
            return;
        }

        _ = _dispatcher.InvokeAsync(async () =>
        {
            // TODO: Verify this produces the correct exception-surfacing behaviors.
            // For example, JS interop exceptions should flow back into JS, whereas
            // renderer exceptions should be fatal.
            try
            {
                await _ipcReceiver.OnMessageReceivedAsync(_currentPageContext, message);
            }
            catch (Exception ex)
            {
                _ipcSender.NotifyUnhandledException(ex);
                throw;
            }
        });
    }

    /// <summary>
    /// Calls the specified <paramref name="workItem"/> asynchronously and passes in the scoped services available to Razor components.
    /// This method will not throw any exceptions if it is unable to call the specified <paramref name="workItem"/>, but if it does call it, then exceptions may still be thrown by the <paramref name="workItem"/> itself.
    /// </summary>
    /// <param name="workItem">The action to call.</param>
    /// <returns>Returns a <see cref="Task"/> representing <c>true</c> if the <paramref name="workItem"/> was called, or <c>false</c> if it was not called because Blazor is not currently running.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="workItem"/> is <c>null</c>.</exception>
    public async Task<bool> TryDispatchAsync(Action<IServiceProvider> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        var capturedCurrentPageContext = _currentPageContext;

        if (capturedCurrentPageContext is null)
        {
            return false;
        }

        return await capturedCurrentPageContext.Renderer.Dispatcher.InvokeAsync(() =>
        {
            if (capturedCurrentPageContext != _currentPageContext)
            {
                // If the captured context doesn't match the current context, that means that there was something like
                // a navigation event that caused the original page to be detached and a new one attached. Thus, we
                // cancel out of the operation and return failure.
                return false;
            }
            workItem(_currentPageContext.ServiceProvider);
            return true;
        });
    }

    /// <summary>
    /// Tries to provide the response content for a given network request.
    /// </summary>
    /// <param name="uri">The uri of the request</param>
    /// <param name="allowFallbackOnHostPage">Whether or not to fallback to the host page.</param>
    /// <param name="statusCode">The status code of the response.</param>
    /// <param name="statusMessage">The response status message.</param>
    /// <param name="content">The response content</param>
    /// <param name="headers">The response headers</param>
    /// <returns><c>true</c> if the response can be provided; <c>false</c> otherwise.</returns>
    protected bool TryGetResponseContent(string uri, bool allowFallbackOnHostPage, out int statusCode, out string statusMessage, out Stream content, out IDictionary<string, string> headers)
        => _staticContentProvider.TryGetResponseContent(uri, allowFallbackOnHostPage, out statusCode, out statusMessage, out content, out headers);

    internal async Task AttachToPageAsync(string baseUrl, string startUrl)
    {
        // If there was some previous attached page, dispose all its resources. We're not eagerly disposing
        // page contexts when the user navigates away, because we don't get notified about that. We could
        // change this if any important reason emerges.
        if (_currentPageContext != null)
        {
            await _currentPageContext.DisposeAsync();
        }

        var serviceScope = _provider.CreateAsyncScope();

        _currentPageContext = new PageContext(_dispatcher, serviceScope, _ipcSender, _jsComponents, baseUrl, startUrl);

        // Add any root components that were registered before the page attached. We don't await any of the
        // returned render tasks so that the components can be processed in parallel.
        var pendingRenders = new List<Task>(_rootComponentsBySelector.Count);
        foreach (var (selector, rootComponent) in _rootComponentsBySelector)
        {
            rootComponent.ComponentId = _currentPageContext.Renderer.AddRootComponent(
                rootComponent.ComponentType, selector);
            pendingRenders.Add(_currentPageContext.Renderer.RenderRootComponentAsync(
                rootComponent.ComponentId.Value, rootComponent.Parameters));
        }

        // Now we wait for all components to finish rendering.
        await Task.WhenAll(pendingRenders);
    }

    private static Uri EnsureTrailingSlash(Uri uri)
        => uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + '/');

    private sealed class RootComponent
    {
        public Type ComponentType { get; init; }
        public ParameterView Parameters { get; init; }
        public int? ComponentId { get; set; }
    }

    /// <summary>
    /// Disposes the current <see cref="WebViewManager"/> instance.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            _disposed = true;

            if (_currentPageContext != null)
            {
                await _currentPageContext.DisposeAsync();
            }
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        // Do not change this code. Put cleanup code in 'DisposeAsync(bool disposing)' method
        GC.SuppressFinalize(this);
        await DisposeAsyncCore();
    }

    private sealed class StaticWebAssetsLoader
    {
        internal static IFileProvider UseStaticWebAssets(IFileProvider fileProvider)
        {
            var manifestPath = ResolveRelativeToAssembly();
            if (File.Exists(manifestPath))
            {
                using var manifestStream = File.OpenRead(manifestPath);
                var manifest = ManifestStaticWebAssetFileProvider.StaticWebAssetManifest.Parse(manifestStream);
                if (manifest.ContentRoots.Length > 0)
                {
                    var manifestProvider = new ManifestStaticWebAssetFileProvider(manifest, (path) => new PhysicalFileProvider(path));
                    return new CompositeFileProvider(manifestProvider, fileProvider);
                }
            }

            return fileProvider;
        }

        private static string? ResolveRelativeToAssembly()
        {
            var assembly = Assembly.GetEntryAssembly();
            if (string.IsNullOrEmpty(assembly?.Location))
            {
                return null;
            }

            var name = Path.GetFileNameWithoutExtension(assembly.Location);

            return Path.Combine(Path.GetDirectoryName(assembly.Location)!, $"{name}.staticwebassets.runtime.json");
        }
    }
}
