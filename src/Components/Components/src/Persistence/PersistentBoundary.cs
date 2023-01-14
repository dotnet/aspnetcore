// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components;

// Notes about persistent boundary:
// The app can disambiguate the usages for a persistent component, since it is allowed to add their
// own PersistentBoundary.
// Component library authors are responsible for disambiguating within the persistent components
// they provide.
// Persistent components rendered outside a persistent boundary fallback to rendering the component
// transparently
// The app can disable the persistent boundary of a given component by rendering it inside a disabled
// boundary explicitly. This lets the app take charge of the persistence if desired and avoid duplication
// if the component has the capacity to persist the data.

// TODO: Right now we assume that the component that loads the data is the one responsible for persisting
// it and restoring it. In other words, components should only persist their own state, and not parameters
// from parent components.
// This has the huge benefit that we control the exact conditions and point in time when the data is
// restored, and we have access to the parameters used to compute the data.
// QUESTION: Can it be desirable to let children component handle the persistence of the data even if
// a parent component is responsible for loading it?
//   * This presents the chicken and egg problem. The parent must persist some data to at least produce
//     an initial render during the restore operation.
//   * The children components might not themselves have a key, but that can be solved by rendering a
//     persistent boundary around each of them.

/// <summary>
/// TODO: Docs
/// </summary>
public class PersistentBoundary : IComponent, IComponentMutationObserver, IDisposable
{
    private RenderHandle _renderHandle;
    private PersistingComponentStateSubscription _subscription;
    private PersistentScope _scope = null!;

    /// <summary>
    /// TODO: Docs
    /// </summary>
    [Parameter] public string Name { get; set; } = null!;

    /// <summary>
    /// TODO: Docs
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; } = null!;

    /// <summary>
    /// TODO: Docs
    /// </summary>
    [CascadingParameter] public PersistentScope ParentScope { get; set; } = null!;

    /// <summary>
    /// TODO: Docs
    /// </summary>
    [Inject] public PersistentComponentState PersistentState { get; set; } = null!;

    /// <inheritdoc/>
    public void Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
        _subscription = PersistentState.RegisterOnPersisting(PersistComponentsOnScope);
    }

    private Task PersistComponentsOnScope()
    {
        if (!_scope.HasTrackedComponents)
        {
            return Task.CompletedTask;
        }

        foreach (var component in _scope.GetRegisteredComponents())
        {
            component.PersistState(new ScopedPersistentComponentState(_scope, PersistentState));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        var scope = _scope;
        _scope ??= new(ParentScope, Name, _renderHandle.ObserveChildComponentRemoved, PersistentState);

        if (Name == null)
        {
            throw new InvalidOperationException("Persistent boundaries need to have a valid, deterministic and unique name.");
        }

        if (_scope.Name != Name)
        {
            throw new InvalidOperationException("The persistent boundary name can't be changed after the component has been initialized.");
        }

        _renderHandle.Render(builder =>
        {
            builder.OpenComponent<CascadingValue<PersistentScope>>(0);
            builder.AddAttribute(1, nameof(CascadingValue<PersistentScope>.Value), _scope);
            builder.AddAttribute(2, nameof(CascadingValue<PersistentScope>.IsFixed), true);
            builder.AddAttribute(3, nameof(CascadingValue<PersistentScope>.ChildContent), ChildContent);
            builder.CloseComponent();
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose() => ((IDisposable)_subscription).Dispose();

    void IComponentMutationObserver.ComponentRemoved(IComponent component) =>
        _scope.Unregister((IHandleComponentPersistentState)component);
}
