// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// An implementation for <see cref="IServiceProvidersFeature"/> for accessing request services.
/// </summary>
public class RequestServicesFeature : IServiceProvidersFeature, IDisposable, IAsyncDisposable
{
    private readonly IServiceScopeFactory? _scopeFactory;
    private IServiceProvider? _requestServices;
    private IServiceScope? _scope;
    private bool _requestServicesSet;
    private readonly HttpContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="RequestServicesFeature"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="scopeFactory">The <see cref="IServiceScopeFactory"/>.</param>
    public RequestServicesFeature(HttpContext context, IServiceScopeFactory? scopeFactory)
    {
        _context = context;
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public IServiceProvider RequestServices
    {
        get
        {
            if (!_requestServicesSet && _scopeFactory != null)
            {
                _context.Response.RegisterForDisposeAsync(this);
                _scope = _scopeFactory.CreateScope();
                _requestServices = _scope.ServiceProvider;
                _requestServicesSet = true;
            }
            return _requestServices!;
        }

        set
        {
            _requestServices = value;
            _requestServicesSet = true;
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        switch (_scope)
        {
            case IAsyncDisposable asyncDisposable:
                var vt = asyncDisposable.DisposeAsync();
                if (!vt.IsCompletedSuccessfully)
                {
                    return Awaited(this, vt);
                }
                // If its a IValueTaskSource backed ValueTask,
                // inform it its result has been read so it can reset
                vt.GetAwaiter().GetResult();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }

        _scope = null;
        _requestServices = null;

        return default;

        static async ValueTask Awaited(RequestServicesFeature servicesFeature, ValueTask vt)
        {
            await vt;
            servicesFeature._scope = null;
            servicesFeature._requestServices = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
