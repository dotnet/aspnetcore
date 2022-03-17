// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents a collection of parameters supplied to an <see cref="IComponent"/>
/// by its parent in the render tree.
/// </summary>
public readonly struct ParameterView
{
    private static readonly RenderTreeFrame[] _emptyFrames = new RenderTreeFrame[]
    {
            RenderTreeFrame.Element(0, string.Empty).WithComponentSubtreeLength(1)
    };

    private static readonly ParameterView _empty = new ParameterView(ParameterViewLifetime.Unbound, _emptyFrames, 0, Array.Empty<CascadingParameterState>());

    private readonly ParameterViewLifetime _lifetime;
    private readonly RenderTreeFrame[] _frames;
    private readonly int _ownerIndex;
    private readonly IReadOnlyList<CascadingParameterState> _cascadingParameters;

    internal ParameterView(in ParameterViewLifetime lifetime, RenderTreeFrame[] frames, int ownerIndex)
        : this(lifetime, frames, ownerIndex, Array.Empty<CascadingParameterState>())
    {
    }

    private ParameterView(in ParameterViewLifetime lifetime, RenderTreeFrame[] frames, int ownerIndex, IReadOnlyList<CascadingParameterState> cascadingParameters)
    {
        _lifetime = lifetime;
        _frames = frames;
        _ownerIndex = ownerIndex;
        _cascadingParameters = cascadingParameters;
    }

    /// <summary>
    /// Gets an empty <see cref="ParameterView"/>.
    /// </summary>
    public static ParameterView Empty => _empty;

    internal ParameterViewLifetime Lifetime => _lifetime;

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="ParameterView"/>.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public Enumerator GetEnumerator()
    {
        _lifetime.AssertNotExpired();
        return new Enumerator(_frames, _ownerIndex, _cascadingParameters);
    }

    /// <summary>
    /// Gets the value of the parameter with the specified name.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="result">Receives the result, if any.</param>
    /// <returns>True if a matching parameter was found; false otherwise.</returns>
    public bool TryGetValue<TValue>(string parameterName, [MaybeNullWhen(false)] out TValue result)
    {
        foreach (var entry in this)
        {
            if (string.Equals(entry.Name, parameterName))
            {
                result = (TValue)entry.Value;
                return true;
            }
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Gets the value of the parameter with the specified name, or a default value
    /// if no such parameter exists in the collection.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The parameter value if found; otherwise the default value for the specified type.</returns>
    public TValue? GetValueOrDefault<TValue>(string parameterName)
        => GetValueOrDefault<TValue?>(parameterName, default);

    /// <summary>
    /// Gets the value of the parameter with the specified name, or a specified default value
    /// if no such parameter exists in the collection.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="defaultValue">The default value to return if no such parameter exists in the collection.</param>
    /// <returns>The parameter value if found; otherwise <paramref name="defaultValue"/>.</returns>
    public TValue GetValueOrDefault<TValue>(string parameterName, TValue defaultValue)
        => TryGetValue<TValue>(parameterName, out TValue? result) ? result : defaultValue;

    /// <summary>
    /// Returns a dictionary populated with the contents of the <see cref="ParameterView"/>.
    /// </summary>
    /// <returns>A dictionary populated with the contents of the <see cref="ParameterView"/>.</returns>
    public IReadOnlyDictionary<string, object> ToDictionary()
    {
        var result = new Dictionary<string, object>();
        foreach (var entry in this)
        {
            result[entry.Name] = entry.Value;
        }
        return result;
    }

    internal ParameterView Clone()
    {
        if (ReferenceEquals(_frames, _emptyFrames))
        {
            return Empty;
        }

        var numEntries = GetEntryCount();
        var cloneBuffer = new RenderTreeFrame[1 + numEntries];
        cloneBuffer[0] = RenderTreeFrame.PlaceholderChildComponentWithSubtreeLength(1 + numEntries);
        _frames.AsSpan(1, numEntries).CopyTo(cloneBuffer.AsSpan(1));

        return new ParameterView(Lifetime, cloneBuffer, _ownerIndex);
    }

    internal ParameterView WithCascadingParameters(IReadOnlyList<CascadingParameterState> cascadingParameters)
        => new ParameterView(_lifetime, _frames, _ownerIndex, cascadingParameters);

    // It's internal because there isn't a known use case for user code comparing
    // ParameterView instances, and even if there was, it's unlikely it should
    // use these equality rules which are designed for their effect on rendering.
    internal bool DefinitelyEquals(ParameterView oldParameters)
    {
        // In general we can't detect mutations on arbitrary objects. We can't trust
        // things like .Equals or .GetHashCode because they usually only tell us about
        // shallow changes, not deep mutations. So we return false if both:
        //  [1] All the parameters are known to be immutable (i.e., Type.IsPrimitive
        //      or is in a known set of common immutable types)
        //  [2] And all the parameter values are equal to their previous values
        // Otherwise be conservative and return false.
        // To make this check cheaper, since parameters are virtually always generated in
        // a deterministic order, we don't bother to account for reordering, so if any
        // of the names don't match sequentially we just return false too.
        //
        // The logic here may look kind of epic, and would certainly be simpler if we
        // used ParameterEnumerator.GetEnumerator(), but it's perf-critical and this
        // implementation requires a lot fewer instructions than a GetEnumerator-based one.

        var oldIndex = oldParameters._ownerIndex;
        var newIndex = _ownerIndex;
        var oldEndIndexExcl = oldIndex + oldParameters._frames[oldIndex].ComponentSubtreeLengthField;
        var newEndIndexExcl = newIndex + _frames[newIndex].ComponentSubtreeLengthField;
        while (true)
        {
            // First, stop if we've reached the end of either subtree
            oldIndex++;
            newIndex++;
            var oldFinished = oldIndex == oldEndIndexExcl;
            var newFinished = newIndex == newEndIndexExcl;
            if (oldFinished || newFinished)
            {
                return oldFinished == newFinished; // Same only if we have same number of parameters
            }
            else
            {
                // Since neither subtree has finished, it's safe to read the next frame from both
                ref var oldFrame = ref oldParameters._frames[oldIndex];
                ref var newFrame = ref _frames[newIndex];

                // Stop if we've reached the end of either subtree's sequence of attributes
                oldFinished = oldFrame.FrameTypeField != RenderTreeFrameType.Attribute;
                newFinished = newFrame.FrameTypeField != RenderTreeFrameType.Attribute;
                if (oldFinished || newFinished)
                {
                    return oldFinished == newFinished; // Same only if we have same number of parameters
                }
                else
                {
                    if (!string.Equals(oldFrame.AttributeNameField, newFrame.AttributeNameField, StringComparison.Ordinal))
                    {
                        return false; // Different names
                    }

                    var oldValue = oldFrame.AttributeValueField;
                    var newValue = newFrame.AttributeValueField;
                    if (ChangeDetection.MayHaveChanged(oldValue, newValue))
                    {
                        return false;
                    }
                }
            }
        }
    }

    internal void CaptureSnapshot(ArrayBuilder<RenderTreeFrame> builder)
    {
        builder.Clear();

        var numEntries = GetEntryCount();

        // We need to prefix the captured frames with an "owner" frame that
        // describes the length of the buffer so that ParameterView
        // knows how far to iterate through it.
        var owner = RenderTreeFrame.PlaceholderChildComponentWithSubtreeLength(1 + numEntries);
        builder.Append(owner);

        if (numEntries > 0)
        {
            builder.Append(_frames, _ownerIndex + 1, numEntries);
        }
    }

    private int GetEntryCount()
    {
        var numEntries = 0;
        foreach (var _ in this)
        {
            numEntries++;
        }

        return numEntries;
    }

    /// <summary>
    /// Creates a new <see cref="ParameterView"/> from the given <see cref="IDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <param name="parameters">The <see cref="IDictionary{TKey, TValue}"/> with the parameters.</param>
    /// <returns>A <see cref="ParameterView"/>.</returns>
    public static ParameterView FromDictionary(IDictionary<string, object?> parameters)
    {
        var builder = new ParameterViewBuilder(parameters.Count);
        foreach (var kvp in parameters)
        {
            builder.Add(kvp.Key, kvp.Value);
        }

        return builder.ToParameterView();
    }

    /// <summary>
    /// For each parameter property on <paramref name="target"/>, updates its value to
    /// match the corresponding entry in the <see cref="ParameterView"/>.
    /// </summary>
    /// <param name="target">An object that has a public writable property matching each parameter's name and type.</param>
    public void SetParameterProperties(object target)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        ComponentProperties.SetProperties(this, target);
    }

    /// <summary>
    /// An enumerator that iterates through a <see cref="ParameterView"/>.
    /// </summary>
    // Note that this intentionally does not implement IEnumerator<>. This is the same pattern as Span<>.Enumerator
    // it's valid to foreach over a type that doesn't implement IEnumerator<>.
    public struct Enumerator
    {
        private RenderTreeFrameParameterEnumerator _directParamsEnumerator;
        private CascadingParameterEnumerator _cascadingParameterEnumerator;
        private bool _isEnumeratingDirectParams;

        internal Enumerator(RenderTreeFrame[] frames, int ownerIndex, IReadOnlyList<CascadingParameterState> cascadingParameters)
        {
            _directParamsEnumerator = new RenderTreeFrameParameterEnumerator(frames, ownerIndex);
            _cascadingParameterEnumerator = new CascadingParameterEnumerator(cascadingParameters);
            _isEnumeratingDirectParams = true;
        }

        /// <summary>
        /// Gets the current value of the enumerator.
        /// </summary>
        public ParameterValue Current => _isEnumeratingDirectParams
            ? _directParamsEnumerator.Current
            : _cascadingParameterEnumerator.Current;

        /// <summary>
        /// Instructs the enumerator to move to the next value in the sequence.
        /// </summary>
        /// <returns>A flag to indicate whether or not there is a next value.</returns>
        public bool MoveNext()
        {
            if (_isEnumeratingDirectParams)
            {
                if (_directParamsEnumerator.MoveNext())
                {
                    return true;
                }
                else
                {
                    _isEnumeratingDirectParams = false;
                }
            }

            return _cascadingParameterEnumerator.MoveNext();
        }
    }

    private struct RenderTreeFrameParameterEnumerator
    {
        private readonly RenderTreeFrame[] _frames;
        private readonly int _ownerIndex;
        private readonly int _ownerDescendantsEndIndexExcl;
        private int _currentIndex;
        private ParameterValue _current;

        internal RenderTreeFrameParameterEnumerator(RenderTreeFrame[] frames, int ownerIndex)
        {
            _frames = frames;
            _ownerIndex = ownerIndex;
            _ownerDescendantsEndIndexExcl = ownerIndex + _frames[ownerIndex].ElementSubtreeLengthField;
            _currentIndex = ownerIndex;
            _current = default;
        }

        public ParameterValue Current => _current;

        public bool MoveNext()
        {
            // Stop iteration if you get to the end of the owner's descendants...
            var nextIndex = _currentIndex + 1;
            if (nextIndex == _ownerDescendantsEndIndexExcl)
            {
                return false;
            }

            // ... or if you get to its first non-attribute descendant (because attributes
            // are always before any other type of descendant)
            if (_frames[nextIndex].FrameTypeField != RenderTreeFrameType.Attribute)
            {
                return false;
            }

            _currentIndex = nextIndex;

            ref var frame = ref _frames[_currentIndex];
            _current = new ParameterValue(frame.AttributeNameField, frame.AttributeValueField, false);

            return true;
        }
    }

    private struct CascadingParameterEnumerator
    {
        private readonly IReadOnlyList<CascadingParameterState> _cascadingParameters;
        private int _currentIndex;
        private ParameterValue _current;

        public CascadingParameterEnumerator(IReadOnlyList<CascadingParameterState> cascadingParameters)
        {
            _cascadingParameters = cascadingParameters;
            _currentIndex = -1;
            _current = default;
        }

        public ParameterValue Current => _current;

        public bool MoveNext()
        {
            var nextIndex = _currentIndex + 1;
            if (nextIndex < _cascadingParameters.Count)
            {
                _currentIndex = nextIndex;

                var state = _cascadingParameters[_currentIndex];
                _current = new ParameterValue(state.LocalValueName, state.ValueSupplier.CurrentValue!, true);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
