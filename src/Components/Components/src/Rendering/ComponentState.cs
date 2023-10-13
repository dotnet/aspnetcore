// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Sections;

namespace Microsoft.AspNetCore.Components.Rendering;

/// <summary>
/// Tracks the rendering state associated with an <see cref="IComponent"/> instance
/// within the context of a <see cref="Renderer"/>. This is an internal implementation
/// detail of <see cref="Renderer"/>.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class ComponentState : IAsyncDisposable
{
    private readonly Renderer _renderer;
    private readonly bool _hasAnyCascadingParameterSubscriptions;
    private IReadOnlyList<CascadingParameterState> _cascadingParameters;
    private bool _hasCascadingParameters;
    private bool _hasSingleDeliveryCascadingParameters;
    private RenderTreeBuilder _nextRenderTree;
    private ArrayBuilder<RenderTreeFrame>? _latestDirectParametersSnapshot; // Lazily instantiated
    private bool _componentWasDisposed;

    /// <summary>
    /// Constructs an instance of <see cref="ComponentState"/>.
    /// </summary>
    /// <param name="renderer">The <see cref="Renderer"/> with which the new instance should be associated.</param>
    /// <param name="componentId">The externally visible identifier for the <see cref="IComponent"/>. The identifier must be unique in the context of the <see cref="Renderer"/>.</param>
    /// <param name="component">The <see cref="IComponent"/> whose state is being tracked.</param>
    /// <param name="parentComponentState">The <see cref="ComponentState"/> for the parent component, or null if this is a root component.</param>
    public ComponentState(Renderer renderer, int componentId, IComponent component, ComponentState? parentComponentState)
    {
        ComponentId = componentId;
        ParentComponentState = parentComponentState;
        Component = component ?? throw new ArgumentNullException(nameof(component));
        LogicalParentComponentState = component is SectionOutlet.SectionOutletContentRenderer
            ? (GetSectionOutletLogicalParent(renderer, (SectionOutlet)parentComponentState!.Component) ?? parentComponentState)
            : parentComponentState;
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _cascadingParameters = CascadingParameterState.FindCascadingParameters(this, out _hasSingleDeliveryCascadingParameters);
        CurrentRenderTree = new RenderTreeBuilder();
        _nextRenderTree = new RenderTreeBuilder();

        if (_cascadingParameters.Count != 0)
        {
            _hasCascadingParameters = true;
            _hasAnyCascadingParameterSubscriptions = AddCascadingParameterSubscriptions();
        }
    }

    private static ComponentState? GetSectionOutletLogicalParent(Renderer renderer, SectionOutlet sectionOutlet)
    {
        // This will return null if the SectionOutlet is not currently rendering any content
        if (sectionOutlet.CurrentLogicalParent is { } logicalParent
            && renderer.GetComponentState(logicalParent) is { } logicalParentComponentState)
        {
            return logicalParentComponentState;
        }

        return null;
    }

    /// <summary>
    /// Gets the component ID.
    /// </summary>
    public int ComponentId { get; }

    /// <summary>
    /// Gets the component instance.
    /// </summary>
    public IComponent Component { get; }

    /// <summary>
    /// Gets the <see cref="ComponentState"/> of the parent component, or null if this is a root component.
    /// </summary>
    public ComponentState? ParentComponentState { get; }

    /// <summary>
    /// Gets the <see cref="ComponentState"/> of the logical parent component, or null if this is a root component.
    /// </summary>
    public ComponentState? LogicalParentComponentState { get; }

    internal RenderTreeBuilder CurrentRenderTree { get; set; }

    internal Renderer Renderer => _renderer;

    internal void RenderIntoBatch(RenderBatchBuilder batchBuilder, RenderFragment renderFragment, out Exception? renderFragmentException)
    {
        renderFragmentException = null;

        // A component might be in the render queue already before getting disposed by an
        // earlier entry in the render queue. In that case, rendering is a no-op.
        if (_componentWasDisposed)
        {
            return;
        }

        _nextRenderTree.Clear();

        try
        {
            renderFragment(_nextRenderTree);
        }
        catch (Exception ex)
        {
            // If an exception occurs in the render fragment delegate, we won't process the diff in any way, so child components,
            // event handlers, etc., will all be left untouched as if this component didn't re-render at all. The Renderer will
            // then forcibly clear the descendant subtree by rendering an empty fragment for this component.
            renderFragmentException = ex;
            return;
        }

        // We don't want to make errors from this be recoverable, because there's no legitimate reason for them to happen
        _nextRenderTree.AssertTreeIsValid(Component);

        // Swap the old and new tree builders
        (CurrentRenderTree, _nextRenderTree) = (_nextRenderTree, CurrentRenderTree);

        var diff = RenderTreeDiffBuilder.ComputeDiff(
            _renderer,
            batchBuilder,
            ComponentId,
            _nextRenderTree.GetFrames(),
            CurrentRenderTree.GetFrames());
        batchBuilder.UpdatedComponentDiffs.Append(diff);
        batchBuilder.InvalidateParameterViews();
    }

    // Callers expect this method to always return a faulted task.
    internal Task NotifyRenderCompletedAsync()
    {
        if (Component is IHandleAfterRender handlerAfterRender)
        {
            try
            {
                return handlerAfterRender.OnAfterRenderAsync();
            }
            catch (OperationCanceledException cex)
            {
                return Task.FromCanceled(cex.CancellationToken);
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        return Task.CompletedTask;
    }

    internal void SetDirectParameters(ParameterView parameters)
    {
        // Note: We should be careful to ensure that the framework never calls
        // IComponent.SetParametersAsync directly elsewhere. We should only call it
        // via ComponentState.SetDirectParameters (or NotifyCascadingValueChanged below).
        // If we bypass this, the component won't receive the cascading parameters nor
        // will it update its snapshot of direct parameters.

        if (_hasAnyCascadingParameterSubscriptions)
        {
            // We may need to replay these direct parameters later (in NotifyCascadingValueChanged),
            // but we can't guarantee that the original underlying data won't have mutated in the
            // meantime, since it's just an index into the parent's RenderTreeFrames buffer.
            if (_latestDirectParametersSnapshot == null)
            {
                _latestDirectParametersSnapshot = new ArrayBuilder<RenderTreeFrame>();
            }

            parameters.CaptureSnapshot(_latestDirectParametersSnapshot);
        }

        if (_hasCascadingParameters)
        {
            parameters = parameters.WithCascadingParameters(_cascadingParameters);
            if (_hasSingleDeliveryCascadingParameters)
            {
                StopSupplyingSingleDeliveryCascadingParameters();
            }
        }

        SupplyCombinedParameters(parameters);
    }

    private void StopSupplyingSingleDeliveryCascadingParameters()
    {
        // We're optimizing for the case where there are no single-delivery parameters, or if there were, we already
        // removed them. In those cases _cascadingParameters is already up-to-date and gets used as-is without any filtering.
        // In the unusual case were there are single-delivery parameters and we haven't yet removed them, it's OK to
        // go through the extra work and allocation of creating a new list.
        List<CascadingParameterState>? remainingCascadingParameters = null;
        foreach (var param in _cascadingParameters)
        {
            if (!param.ParameterInfo.Attribute.SingleDelivery)
            {
                remainingCascadingParameters ??= new(_cascadingParameters.Count /* upper bound on capacity needed */);
                remainingCascadingParameters.Add(param);
            }
        }

        // Now update all the tracking state to match the filtered set
        _hasCascadingParameters = remainingCascadingParameters is not null;
        _cascadingParameters = (IReadOnlyList<CascadingParameterState>?)remainingCascadingParameters ?? Array.Empty<CascadingParameterState>();
        _hasSingleDeliveryCascadingParameters = false;
    }

    internal void NotifyCascadingValueChanged(in ParameterViewLifetime lifetime)
    {
        // If the component was already disposed, we must not try to supply new parameters. Among other reasons,
        // _latestDirectParametersSnapshot will already have been disposed and that puts it into an invalid state
        // so we can't even read from it. Note that disposal doesn't instantly trigger unsubscription from cascading
        // values - that only happens when the ComponentState is processed later by the disposal queue.
        if (_componentWasDisposed)
        {
            return;
        }

        var directParams = _latestDirectParametersSnapshot != null
            ? new ParameterView(lifetime, _latestDirectParametersSnapshot.Buffer, 0)
            : ParameterView.Empty;
        var allParams = directParams.WithCascadingParameters(_cascadingParameters!);
        SupplyCombinedParameters(allParams);
    }

    // This should not be called from anywhere except SetDirectParameters or NotifyCascadingValueChanged.
    // Those two methods know how to correctly combine both cascading and non-cascading parameters to supply
    // a consistent set to the recipient.
    private void SupplyCombinedParameters(ParameterView directAndCascadingParameters)
    {
        // Normalise sync and async exceptions into a Task
        Task setParametersAsyncTask;
        try
        {
            setParametersAsyncTask = Component.SetParametersAsync(directAndCascadingParameters);
        }
        catch (Exception ex)
        {
            setParametersAsyncTask = Task.FromException(ex);
        }

        _renderer.AddToPendingTasksWithErrorHandling(setParametersAsyncTask, owningComponentState: this);
    }

    private bool AddCascadingParameterSubscriptions()
    {
        var hasSubscription = false;
        var numCascadingParameters = _cascadingParameters!.Count;

        for (var i = 0; i < numCascadingParameters; i++)
        {
            var valueSupplier = _cascadingParameters[i].ValueSupplier;
            if (!valueSupplier.IsFixed)
            {
                valueSupplier.Subscribe(this, _cascadingParameters[i].ParameterInfo);
                hasSubscription = true;
            }
        }

        return hasSubscription;
    }

    private void RemoveCascadingParameterSubscriptions()
    {
        var numCascadingParameters = _cascadingParameters!.Count;
        for (var i = 0; i < numCascadingParameters; i++)
        {
            var supplier = _cascadingParameters[i].ValueSupplier;
            if (!supplier.IsFixed)
            {
                supplier.Unsubscribe(this, _cascadingParameters[i].ParameterInfo);
            }
        }
    }

    /// <summary>
    /// Disposes this instance and its associated component.
    /// </summary>
    public virtual ValueTask DisposeAsync()
    {
        _componentWasDisposed = true;
        DisposeBuffers();

        // Components shouldn't need to implement IAsyncDisposable and IDisposable simultaneously,
        // but in case they do, we prefer the async overload since we understand the sync overload
        // is implemented for more "constrained" scenarios.
        // Component authors are responsible for their IAsyncDisposable implementations not taking
        // forever.
        if (Component is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }
        else
        {
            (Component as IDisposable)?.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private void DisposeBuffers()
    {
        ((IDisposable)_nextRenderTree).Dispose();
        ((IDisposable)CurrentRenderTree).Dispose();
        _latestDirectParametersSnapshot?.Dispose();
    }

    internal ValueTask DisposeInBatchAsync(RenderBatchBuilder batchBuilder)
    {
        // We don't expect these things to throw.
        RenderTreeDiffBuilder.DisposeFrames(batchBuilder, ComponentId, CurrentRenderTree.GetFrames());

        if (_hasAnyCascadingParameterSubscriptions)
        {
            RemoveCascadingParameterSubscriptions();
        }

        return DisposeAsync();
    }

    private string GetDebuggerDisplay()
    {
        return $"ComponentId = {ComponentId}, Type = {Component.GetType().Name}, Disposed = {_componentWasDisposed}";
    }
}
