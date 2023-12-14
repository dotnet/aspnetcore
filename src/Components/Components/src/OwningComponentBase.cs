// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// A base class that creates a service provider scope.
/// </summary>
/// <remarks>
/// Use the <see cref="OwningComponentBase"/> class as a base class to author components that control
/// the lifetime of a service provider scope. This is useful when using a transient or scoped service that
/// requires disposal such as a repository or database abstraction. Using <see cref="OwningComponentBase"/>
/// as a base class ensures that the service provider scope is disposed with the component.
/// </remarks>
public abstract class OwningComponentBase : ComponentBase, IDisposable
{
    private AsyncServiceScope? _scope;

    [Inject] IServiceScopeFactory ScopeFactory { get; set; } = default!;

    /// <summary>
    /// Gets a value determining if the component and associated services have been disposed.
    /// </summary>
    protected bool IsDisposed { get; private set; }

    /// <summary>
    /// Gets the scoped <see cref="IServiceProvider"/> that is associated with this component.
    /// </summary>
    protected IServiceProvider ScopedServices
    {
        get
        {
            if (ScopeFactory == null)
            {
                throw new InvalidOperationException("Services cannot be accessed before the component is initialized.");
            }

            ObjectDisposedException.ThrowIf(IsDisposed, this);

            _scope ??= ScopeFactory.CreateAsyncScope();
            return _scope.Value.ServiceProvider;
        }
    }

    void IDisposable.Dispose()
    {
        if (!IsDisposed)
        {
            _scope?.Dispose();
            _scope = null;
            Dispose(disposing: true);
            IsDisposed = true;
        }
    }

    /// <inheritdoc />
    protected virtual void Dispose(bool disposing)
    {
    }
}

/// <summary>
/// A base class that creates a service provider scope, and resolves a service of type <typeparamref name="TService"/>.
/// </summary>
/// <typeparam name="TService">The service type.</typeparam>
/// <remarks>
/// Use the <see cref="OwningComponentBase{TService}"/> class as a base class to author components that control
/// the lifetime of a service or multiple services. This is useful when using a transient or scoped service that
/// requires disposal such as a repository or database abstraction. Using <see cref="OwningComponentBase{TService}"/>
/// as a base class ensures that the service and relates services that share its scope are disposed with the component.
/// </remarks>
public abstract class OwningComponentBase<TService> : OwningComponentBase, IDisposable where TService : notnull
{
    private TService _item = default!;

    /// <summary>
    /// Gets the <typeparamref name="TService"/> that is associated with this component.
    /// </summary>
    protected TService Service
    {
        get
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            // We cache this because we don't know the lifetime. We have to assume that it could be transient.
            _item ??= ScopedServices.GetRequiredService<TService>();
            return _item;
        }
    }
}
