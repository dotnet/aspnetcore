// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Sections;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using System.Buffers;

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
    private readonly string? _componentTypeName;

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

        _renderer.RegisterComponentState(component, ComponentId, this);

        if (_cascadingParameters.Count != 0)
        {
            _hasCascadingParameters = true;
            _hasAnyCascadingParameterSubscriptions = AddCascadingParameterSubscriptions();
        }

        if (_renderer.ComponentMetrics != null && _renderer.ComponentMetrics.IsParametersEnabled)
        {
            _componentTypeName = component.GetType().FullName;
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
        var parametersStartTimestamp = _renderer.ComponentMetrics != null && _renderer.ComponentMetrics.IsParametersEnabled ? Stopwatch.GetTimestamp() : 0;

        // Normalize sync and async exceptions into a Task
        Task setParametersAsyncTask;
        try
        {
            setParametersAsyncTask = Component.SetParametersAsync(directAndCascadingParameters);

            // collect metrics
            if (_renderer.ComponentMetrics != null && _renderer.ComponentMetrics.IsParametersEnabled)
            {
                _ = _renderer.ComponentMetrics.CaptureParametersDuration(setParametersAsyncTask, parametersStartTimestamp, _componentTypeName);
            }
        }
        catch (Exception ex)
        {
            if (_renderer.ComponentMetrics != null && _renderer.ComponentMetrics.IsParametersEnabled)
            {
                _renderer.ComponentMetrics.FailParametersSync(ex, parametersStartTimestamp, _componentTypeName);
            }

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

    /// <summary>
    /// Gets the component key for this component instance.
    /// This is used for state persistence and component identification across render modes.
    /// </summary>
    /// <returns>The component key, or null if no key is available.</returns>
    protected internal virtual object? GetComponentKey()
    {
        if (ParentComponentState is not { } parentComponentState)
        {
            return null;
        }

        // Check if the parentComponentState has a `@key` directive applied to the current component.
        var frames = parentComponentState.CurrentRenderTree.GetFrames();
        for (var i = 0; i < frames.Count; i++)
        {
            ref var currentFrame = ref frames.Array[i];
            if (currentFrame.FrameType != RenderTree.RenderTreeFrameType.Component ||
                !ReferenceEquals(Component, currentFrame.Component))
            {
                // Skip any frame that is not the current component.
                continue;
            }

            return currentFrame.ComponentKey;
        }

        return null;
    }

    /// <summary>
    /// Computes a pseudo-unique key for state persistence.
    /// This considers the property name, component type, and position within the component tree.
    /// </summary>
    /// <param name="propertyName">The name of the property being persisted.</param>
    /// <returns>A unique key for the property.</returns>
    internal string ComputeKey(string propertyName)
    {
        var parentComponentType = GetParentComponentType();
        var componentType = GetComponentType();

        var preKey = ComputePreKey(parentComponentType, componentType, propertyName);
        var finalKey = ComputeFinalKey(preKey);

        return finalKey;
    }

    private static readonly ConcurrentDictionary<(string, string, string), byte[]> _keyCache = new();

    private static byte[] ComputePreKey(string parentComponentType, string componentType, string propertyName)
    {
        return _keyCache.GetOrAdd((parentComponentType, componentType, propertyName), 
            parts => SHA256.HashData(Encoding.UTF8.GetBytes(string.Join(".", parts.Item1, parts.Item2, parts.Item3))));
    }

    private string ComputeFinalKey(byte[] preKey)
    {
        Span<byte> keyHash = stackalloc byte[SHA256.HashSizeInBytes];

        var key = GetSerializableKey();
        byte[]? pool = null;
        try
        {
            Span<byte> keyBuffer = stackalloc byte[1024];
            var currentBuffer = keyBuffer;
            preKey.CopyTo(keyBuffer);
            
            if (key is IUtf8SpanFormattable spanFormattable)
            {
                var wroteKey = false;
                while (!wroteKey)
                {
                    currentBuffer = keyBuffer[preKey.Length..];
                    wroteKey = spanFormattable.TryFormat(currentBuffer, out var written, "", CultureInfo.InvariantCulture);
                    if (!wroteKey)
                    {
                        Debug.Assert(written == 0);
                        GrowBuffer(ref pool, ref keyBuffer);
                    }
                    else
                    {
                        currentBuffer = currentBuffer[..written];
                    }
                }
            }
            else
            {
                var keySpan = ResolveKeySpan(key);
                var wroteKey = false;
                while (!wroteKey)
                {
                    currentBuffer = keyBuffer[preKey.Length..];
                    wroteKey = Encoding.UTF8.TryGetBytes(keySpan, currentBuffer, out var written);
                    if (!wroteKey)
                    {
                        Debug.Assert(written == 0);
                        GrowBuffer(ref pool, ref keyBuffer, keySpan.Length * 4 + preKey.Length);
                    }
                    else
                    {
                        currentBuffer = currentBuffer[..written];
                    }
                }
            }

            keyBuffer = keyBuffer[..(preKey.Length + currentBuffer.Length)];

            var hashSucceeded = SHA256.TryHashData(keyBuffer, keyHash, out _);
            Debug.Assert(hashSucceeded);
            return Convert.ToBase64String(keyHash);
        }
        finally
        {
            if (pool != null)
            {
                ArrayPool<byte>.Shared.Return(pool, clearArray: true);
            }
        }
    }

    /// <summary>
    /// Gets the serializable component key for this component instance.
    /// Returns null if the component key is not serializable.
    /// </summary>
    /// <returns>The serializable component key, or null.</returns>
    private object? GetSerializableKey()
    {
        var key = GetComponentKey();
        return !IsSerializableKey(key) ? null : key;
    }

    private static bool IsSerializableKey(object? key)
    {
        if (key == null)
        {
            return false;
        }
        var keyType = key.GetType();
        var result = Type.GetTypeCode(keyType) != TypeCode.Object
            || keyType == typeof(Guid)
            || keyType == typeof(DateTimeOffset)
            || keyType == typeof(DateOnly)
            || keyType == typeof(TimeOnly);

        return result;
    }

    private static ReadOnlySpan<char> ResolveKeySpan(object? key)
    {
        if (key is IFormattable formattable)
        {
            var keyString = formattable.ToString("", CultureInfo.InvariantCulture);
            return keyString.AsSpan();
        }
        else if (key is IConvertible convertible)
        {
            var keyString = convertible.ToString(CultureInfo.InvariantCulture);
            return keyString.AsSpan();
        }
        return default;
    }

    private static void GrowBuffer(ref byte[]? pool, ref Span<byte> keyBuffer, int? size = null)
    {
        var newPool = pool == null ? ArrayPool<byte>.Shared.Rent(size ?? 2048) : ArrayPool<byte>.Shared.Rent(pool.Length * 2);
        keyBuffer.CopyTo(newPool);
        keyBuffer = newPool;
        if (pool != null)
        {
            ArrayPool<byte>.Shared.Return(pool, clearArray: true);
        }
        pool = newPool;
    }

    private string GetComponentType() => Component.GetType().FullName!;

    private string GetParentComponentType()
    {
        // Filter out SSRRenderModeBoundary from the parent component type calculation
        // We walk up the parent chain until we find a component that is not SSRRenderModeBoundary
        var current = ParentComponentState;
        while (current != null)
        {
            // Check if this is an SSRRenderModeBoundary by checking if it doesn't have a render mode
            // but its parent does (or is null)
            if (current.ParentComponentState?.ParentComponentState is { } grandParent)
            {
                var grandParentRenderMode = _renderer.GetComponentRenderMode(grandParent.Component);
                if (grandParentRenderMode is null)
                {
                    // This indicates current.ParentComponentState is likely an SSRRenderModeBoundary
                    // Skip it and continue to the grandparent
                    current = grandParent;
                    continue;
                }
            }
            
            return current.Component.GetType().FullName!;
        }

        return "";
    }

    private string GetDebuggerDisplay()
    {
        return $"ComponentId = {ComponentId}, Type = {Component.GetType().Name}, Disposed = {_componentWasDisposed}";
    }
}
