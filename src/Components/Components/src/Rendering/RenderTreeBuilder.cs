// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.RenderTree;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Rendering;

// IMPORTANT
//
// Many of these names are used in code generation. Keep these in sync with the code generation code
// See: dotnet/aspnetcore-tooling

/// <summary>
/// Provides methods for building a collection of <see cref="RenderTreeFrame"/> entries.
/// </summary>
public sealed class RenderTreeBuilder : IDisposable
{
    private static readonly object BoxedTrue = true;
    private static readonly object BoxedFalse = false;
    private static readonly string ComponentReferenceCaptureInvalidParentMessage = $"Component reference captures may only be added as children of frames of type {RenderTreeFrameType.Component}";

    private readonly RenderTreeFrameArrayBuilder _entries = new RenderTreeFrameArrayBuilder();
    private readonly Stack<int> _openElementIndices = new Stack<int>();
    private RenderTreeFrameType? _lastNonAttributeFrameType;
    private bool _hasSeenAddMultipleAttributes;
    private Dictionary<string, int>? _seenAttributeNames;

    /// <summary>
    /// The reserved parameter name used for supplying child content.
    /// </summary>
    private const string ChildContent = nameof(ChildContent);

    /// <summary>
    /// Appends a frame representing an element, i.e., a container for other frames.
    /// In order for the <see cref="RenderTreeBuilder"/> state to be valid, you must
    /// also call <see cref="CloseElement"/> immediately after appending the
    /// new element's child frames.
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="elementName">A value representing the type of the element.</param>
    public void OpenElement(int sequence, string elementName)
    {
        // We are entering a new scope, since we track the "duplicate attributes" per
        // element/component we might need to clean them up now.
        if (_hasSeenAddMultipleAttributes)
        {
            var indexOfLastElementOrComponent = _openElementIndices.Peek();
            ProcessDuplicateAttributes(first: indexOfLastElementOrComponent + 1);
        }

        _openElementIndices.Push(_entries.Count);
        _entries.AppendElement(sequence, elementName);
        _lastNonAttributeFrameType = RenderTreeFrameType.Element;
    }

    /// <summary>
    /// Marks a previously appended element frame as closed. Calls to this method
    /// must be balanced with calls to <see cref="OpenElement(int, string)"/>.
    /// </summary>
    public void CloseElement()
    {
        var indexOfEntryBeingClosed = _openElementIndices.Pop();

        // We might be closing an element with only attributes, run the duplicate cleanup pass
        // if necessary.
        if (_hasSeenAddMultipleAttributes)
        {
            ProcessDuplicateAttributes(first: indexOfEntryBeingClosed + 1);
        }

        _entries.Buffer[indexOfEntryBeingClosed].ElementSubtreeLengthField = _entries.Count - indexOfEntryBeingClosed;
    }

    /// <summary>
    /// Appends a frame representing markup content.
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="markupContent">Content for the new markup frame.</param>
    public void AddMarkupContent(int sequence, string? markupContent)
    {
        _entries.AppendMarkup(sequence, markupContent ?? string.Empty);
        _lastNonAttributeFrameType = RenderTreeFrameType.Markup;
    }

    /// <summary>
    /// Appends a frame representing text content.
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="textContent">Content for the new text frame.</param>
    public void AddContent(int sequence, string? textContent)
    {
        _entries.AppendText(sequence, textContent ?? string.Empty);
        _lastNonAttributeFrameType = RenderTreeFrameType.Text;
    }

    /// <summary>
    /// Appends frames representing an arbitrary fragment of content.
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="fragment">Content to append.</param>
    public void AddContent(int sequence, RenderFragment? fragment)
    {
        if (fragment != null)
        {
            // We surround the fragment with a region delimiter to indicate that the
            // sequence numbers inside the fragment are unrelated to the sequence numbers
            // outside it. If we didn't do this, the diffing logic might produce inefficient
            // diffs depending on how the sequence numbers compared.
            OpenRegion(sequence);
            fragment(this);
            CloseRegion();
        }
    }

    /// <summary>
    /// Appends frames representing an arbitrary fragment of content.
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="fragment">Content to append.</param>
    /// <param name="value">The value used by <paramref name="fragment"/>.</param>
    public void AddContent<TValue>(int sequence, RenderFragment<TValue>? fragment, TValue value)
    {
        if (fragment != null)
        {
            AddContent(sequence, fragment(value));
        }
    }

    /// <summary>
    /// Appends a frame representing markup content.
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="markupContent">Content for the new text frame, or null.</param>
    public void AddContent(int sequence, MarkupString? markupContent)
        => AddMarkupContent(sequence, markupContent?.Value);

    /// <summary>
    /// Appends a frame representing markup content.
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="markupContent">Content for the new markup frame.</param>
    public void AddContent(int sequence, MarkupString markupContent)
        => AddMarkupContent(sequence, markupContent.Value);

    /// <summary>
    /// Appends a frame representing text content.
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="textContent">Content for the new text frame.</param>
    public void AddContent(int sequence, object? textContent)
        => AddContent(sequence, textContent?.ToString());

    /// <summary>
    /// <para>
    /// Appends a frame representing a bool-valued attribute with value 'true'.
    /// </para>
    /// <para>
    /// The attribute is associated with the most recently added element.
    /// </para>
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="name">The name of the attribute.</param>
    public void AddAttribute(int sequence, string name)
    {
        if (_lastNonAttributeFrameType != RenderTreeFrameType.Element)
        {
            throw new InvalidOperationException($"Valueless attributes may only be added immediately after frames of type {RenderTreeFrameType.Element}");
        }

        _entries.AppendAttribute(sequence, name, BoxedTrue);
    }

    /// <summary>
    /// <para>
    /// Appends a frame representing a bool-valued attribute.
    /// </para>
    /// <para>
    /// The attribute is associated with the most recently added element. If the value is <c>false</c> and the
    /// current element is not a component, the frame will be omitted.
    /// </para>
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="name">The name of the attribute.</param>
    /// <param name="value">The value of the attribute.</param>
    public void AddAttribute(int sequence, string name, bool value)
    {
        AssertCanAddAttribute();
        if (_lastNonAttributeFrameType == RenderTreeFrameType.Component)
        {
            _entries.AppendAttribute(sequence, name, value ? BoxedTrue : BoxedFalse);
        }
        else if (value)
        {
            // Don't add 'false' attributes for elements. We want booleans to map to the presence
            // or absence of an attribute, and false => "False" which isn't falsy in js.
            _entries.AppendAttribute(sequence, name, BoxedTrue);
        }
        else
        {
            TrackAttributeName(name);
        }
    }

    /// <summary>
    /// <para>
    /// Appends a frame representing a string-valued attribute.
    /// </para>
    /// <para>
    /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
    /// current element is not a component, the frame will be omitted.
    /// </para>
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="name">The name of the attribute.</param>
    /// <param name="value">The value of the attribute.</param>
    public void AddAttribute(int sequence, string name, string? value)
    {
        AssertCanAddAttribute();
        if (value != null || _lastNonAttributeFrameType == RenderTreeFrameType.Component)
        {
            _entries.AppendAttribute(sequence, name, value);
        }
        else
        {
            TrackAttributeName(name);
        }
    }

    /// <summary>
    /// <para>
    /// Appends a frame representing a delegate-valued attribute.
    /// </para>
    /// <para>
    /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
    /// current element is not a component, the frame will be omitted.
    /// </para>
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="name">The name of the attribute.</param>
    /// <param name="value">The value of the attribute.</param>
    public void AddAttribute(int sequence, string name, MulticastDelegate? value)
    {
        AssertCanAddAttribute();
        if (value != null || _lastNonAttributeFrameType == RenderTreeFrameType.Component)
        {
            _entries.AppendAttribute(sequence, name, value);
        }
        else
        {
            TrackAttributeName(name);
        }
    }

    /// <summary>
    /// <para>
    /// Appends a frame representing an <see cref="EventCallback"/> attribute.
    /// </para>
    /// <para>
    /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
    /// current element is not a component, the frame will be omitted.
    /// </para>
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="name">The name of the attribute.</param>
    /// <param name="value">The value of the attribute.</param>
    /// <remarks>
    /// This method is provided for infrastructure purposes, and is used to support generated code
    /// that uses <see cref="EventCallbackFactory"/>.
    /// </remarks>
    public void AddAttribute(int sequence, string name, EventCallback value)
    {
        AssertCanAddAttribute();
        if (_lastNonAttributeFrameType == RenderTreeFrameType.Component)
        {
            // Since this is a component, we need to preserve the type of the EventCallback, so we have
            // to box.
            _entries.AppendAttribute(sequence, name, value);
        }
        else if (value.RequiresExplicitReceiver)
        {
            // If we need to preserve the receiver, we just box the EventCallback
            // so we can get it out on the other side.
            _entries.AppendAttribute(sequence, name, value);
        }
        else if (value.HasDelegate)
        {
            // In the common case the receiver is also the delegate's target, so we
            // just need to retain the delegate. This allows us to avoid an allocation.
            _entries.AppendAttribute(sequence, name, value.Delegate);
        }
        else
        {
            // Track the attribute name if needed since we elided the frame.
            TrackAttributeName(name);
        }
    }

    /// <summary>
    /// <para>
    /// Appends a frame representing an <see cref="EventCallback"/> attribute.
    /// </para>
    /// <para>
    /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
    /// current element is not a component, the frame will be omitted.
    /// </para>
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="name">The name of the attribute.</param>
    /// <param name="value">The value of the attribute.</param>
    /// <remarks>
    /// This method is provided for infrastructure purposes, and is used to support generated code
    /// that uses <see cref="EventCallbackFactory"/>.
    /// </remarks>
    public void AddAttribute<TArgument>(int sequence, string name, EventCallback<TArgument> value)
    {
        AssertCanAddAttribute();
        if (_lastNonAttributeFrameType == RenderTreeFrameType.Component)
        {
            // Since this is a component, we need to preserve the type of the EventCallback, so we have
            // to box.
            _entries.AppendAttribute(sequence, name, value);
        }
        else if (value.RequiresExplicitReceiver)
        {
            // If we need to preserve the receiver - we convert this to an untyped EventCallback. We don't
            // need to preserve the type of an EventCallback<T> when it's invoked from the DOM.
            _entries.AppendAttribute(sequence, name, value.AsUntyped());
        }
        else if (value.HasDelegate)
        {
            // In the common case the receiver is also the delegate's target, so we
            // just need to retain the delegate. This allows us to avoid an allocation.
            _entries.AppendAttribute(sequence, name, value.Delegate);
        }
        else
        {
            // Track the attribute name if needed since we elided the frame.
            TrackAttributeName(name);
        }
    }

    /// <summary>
    /// Appends a frame representing a string-valued attribute.
    /// The attribute is associated with the most recently added element. If the value is <c>null</c>, or
    /// the <see cref="System.Boolean" /> value <c>false</c> and the current element is not a component, the
    /// frame will be omitted.
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="name">The name of the attribute.</param>
    /// <param name="value">The value of the attribute.</param>
    public void AddAttribute(int sequence, string name, object? value)
    {
        // This looks a bit daunting because we need to handle the boxed/object version of all of the
        // types that AddAttribute special cases.
        if (_lastNonAttributeFrameType == RenderTreeFrameType.Element)
        {
            if (value == null)
            {
                // Treat 'null' attribute values for elements as a conditional attribute.
                TrackAttributeName(name);
            }
            else if (value is bool boolValue)
            {
                if (boolValue)
                {
                    _entries.AppendAttribute(sequence, name, BoxedTrue);
                }
                else
                {
                    // Don't add anything for false bool value.
                    TrackAttributeName(name);
                }
            }
            else if (value is IEventCallback callbackValue)
            {
                if (callbackValue.HasDelegate)
                {
                    _entries.AppendAttribute(sequence, name, callbackValue.UnpackForRenderTree());
                }
                else
                {
                    TrackAttributeName(name);
                }
            }
            else if (value is MulticastDelegate)
            {
                _entries.AppendAttribute(sequence, name, value);
            }
            else
            {
                // The value is either a string, or should be treated as a string.
                _entries.AppendAttribute(sequence, name, value.ToString());
            }
        }
        else if (_lastNonAttributeFrameType == RenderTreeFrameType.Component)
        {
            // If this is a component, we always want to preserve the original type.
            _entries.AppendAttribute(sequence, name, value);
        }
        else
        {
            // This is going to throw. Calling it just to get a consistent exception message.
            AssertCanAddAttribute();
        }
    }

    /// <summary>
    /// <para>
    /// Appends a frame representing an attribute.
    /// </para>
    /// <para>
    /// The attribute is associated with the most recently added element.
    /// </para>
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="frame">A <see cref="RenderTreeFrame"/> holding the name and value of the attribute.</param>
    public void AddAttribute(int sequence, RenderTreeFrame frame)
    {
        if (frame.FrameTypeField != RenderTreeFrameType.Attribute)
        {
            throw new ArgumentException($"The {nameof(frame.FrameType)} must be {RenderTreeFrameType.Attribute}.");
        }

        AssertCanAddAttribute();
        frame.SequenceField = sequence;
        _entries.Append(frame);
    }

    /// <summary>
    /// Adds frames representing multiple attributes with the same sequence number.
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="attributes">A collection of key-value pairs representing attributes.</param>
    public void AddMultipleAttributes(int sequence, IEnumerable<KeyValuePair<string, object>>? attributes)
    {
        // Calling this up-front just to make sure we validate before mutating anything.
        AssertCanAddAttribute();

        if (attributes != null)
        {
            _hasSeenAddMultipleAttributes = true;

            foreach (var attribute in attributes)
            {
                // This will call the AddAttribute(int, string, object) overload.
                //
                // This is fine because we try to make the object overload behave identically
                // to the others.
                AddAttribute(sequence, attribute.Key, attribute.Value);
            }
        }
    }

    /// <summary>
    /// <para>
    /// Indicates that the preceding attribute represents an event handler
    /// whose execution updates the attribute with name <paramref name="updatesAttributeName"/>.
    /// </para>
    /// <para>
    /// This information is used by the rendering system to determine whether
    /// to accept a value update for the other attribute when receiving a
    /// call to the event handler.
    /// </para>
    /// </summary>
    /// <param name="updatesAttributeName">The name of another attribute whose value can be updated when the event handler is executed.</param>
    public void SetUpdatesAttributeName(string updatesAttributeName)
    {
        if (_entries.Count == 0)
        {
            throw new InvalidOperationException("No preceding attribute frame exists.");
        }

        ref var prevFrame = ref _entries.Buffer[_entries.Count - 1];
        if (prevFrame.FrameTypeField != RenderTreeFrameType.Attribute)
        {
            throw new InvalidOperationException($"Incorrect frame type: '{prevFrame.FrameTypeField}'");
        }

        prevFrame.AttributeEventUpdatesAttributeNameField = updatesAttributeName;
    }

    /// <summary>
    /// Appends a frame representing a child component.
    /// </summary>
    /// <typeparam name="TComponent">The type of the child component.</typeparam>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    public void OpenComponent<[DynamicallyAccessedMembers(Component)] TComponent>(int sequence) where TComponent : notnull, IComponent
        => OpenComponentUnchecked(sequence, typeof(TComponent));

    /// <summary>
    /// Appends a frame representing a child component.
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="componentType">The type of the child component.</param>
    public void OpenComponent(int sequence, [DynamicallyAccessedMembers(Component)] Type componentType)
    {
        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException($"The component type must implement {typeof(IComponent).FullName}.");
        }

        OpenComponentUnchecked(sequence, componentType);
    }

    /// <summary>
    /// Appends a frame representing a component parameter.
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="name">The name of the attribute.</param>
    /// <param name="value">The value of the attribute.</param>
    public void AddComponentParameter(int sequence, string name, object? value)
    {
        AssertCanAddComponentParameter();
        _entries.AppendAttribute(sequence, name, value);
    }

    /// <summary>
    /// Assigns the specified key value to the current element or component.
    /// </summary>
    /// <param name="value">The value for the key.</param>
    public void SetKey(object? value)
    {
        if (value == null)
        {
            // Null is equivalent to not having set a key, which is valuable because Razor syntax doesn't have an
            // easy way to have conditional directive attributes
            return;
        }

        var parentFrameIndex = GetCurrentParentFrameIndex();
        if (!parentFrameIndex.HasValue)
        {
            throw new InvalidOperationException("Cannot set a key outside the scope of a component or element.");
        }

        var parentFrameIndexValue = parentFrameIndex.Value;
        ref var parentFrame = ref _entries.Buffer[parentFrameIndexValue];
        switch (parentFrame.FrameTypeField)
        {
            case RenderTreeFrameType.Element:
                parentFrame.ElementKeyField = value; // It's a ref var, so this writes to the array
                break;
            case RenderTreeFrameType.Component:
                parentFrame.ComponentKeyField = value; // It's a ref var, so this writes to the array
                break;
            default:
                throw new InvalidOperationException($"Cannot set a key on a frame of type {parentFrame.FrameTypeField}.");
        }
    }

    private void OpenComponentUnchecked(int sequence, [DynamicallyAccessedMembers(Component)] Type componentType)
    {
        // We are entering a new scope, since we track the "duplicate attributes" per
        // element/component we might need to clean them up now.
        if (_hasSeenAddMultipleAttributes)
        {
            var indexOfLastElementOrComponent = _openElementIndices.Peek();
            ProcessDuplicateAttributes(first: indexOfLastElementOrComponent + 1);
        }

        _openElementIndices.Push(_entries.Count);
        _entries.AppendComponent(sequence, componentType);
        _lastNonAttributeFrameType = RenderTreeFrameType.Component;
    }

    /// <summary>
    /// Marks a previously appended component frame as closed. Calls to this method
    /// must be balanced with calls to <see cref="OpenComponent{TComponent}"/>.
    /// </summary>
    public void CloseComponent()
    {
        var indexOfEntryBeingClosed = _openElementIndices.Pop();

        // We might be closing a component with only attributes. Run the attribute cleanup pass
        // if necessary.
        if (_hasSeenAddMultipleAttributes)
        {
            ProcessDuplicateAttributes(first: indexOfEntryBeingClosed + 1);
        }

        _entries.Buffer[indexOfEntryBeingClosed].ComponentSubtreeLengthField = _entries.Count - indexOfEntryBeingClosed;
    }

    /// <summary>
    /// Appends a frame representing an instruction to capture a reference to the parent element.
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="elementReferenceCaptureAction">An action to be invoked whenever the reference value changes.</param>
    public void AddElementReferenceCapture(int sequence, Action<ElementReference> elementReferenceCaptureAction)
    {
        if (GetCurrentParentFrameType() != RenderTreeFrameType.Element)
        {
            throw new InvalidOperationException($"Element reference captures may only be added as children of frames of type {RenderTreeFrameType.Element}");
        }

        _entries.AppendElementReferenceCapture(sequence, elementReferenceCaptureAction);
        _lastNonAttributeFrameType = RenderTreeFrameType.ElementReferenceCapture;
    }

    /// <summary>
    /// Appends a frame representing an instruction to capture a reference to the parent component.
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    /// <param name="componentReferenceCaptureAction">An action to be invoked whenever the reference value changes.</param>
    public void AddComponentReferenceCapture(int sequence, Action<object> componentReferenceCaptureAction)
    {
        var parentFrameIndex = GetCurrentParentFrameIndex();
        if (!parentFrameIndex.HasValue)
        {
            throw new InvalidOperationException(ComponentReferenceCaptureInvalidParentMessage);
        }

        var parentFrameIndexValue = parentFrameIndex.Value;
        if (_entries.Buffer[parentFrameIndexValue].FrameTypeField != RenderTreeFrameType.Component)
        {
            throw new InvalidOperationException(ComponentReferenceCaptureInvalidParentMessage);
        }

        _entries.AppendComponentReferenceCapture(sequence, componentReferenceCaptureAction, parentFrameIndexValue);
        _lastNonAttributeFrameType = RenderTreeFrameType.ComponentReferenceCapture;
    }

    /// <summary>
    /// Adds a frame indicating the render mode on the enclosing component frame.
    /// </summary>
    /// <param name="renderMode">The <see cref="IComponentRenderMode"/>.</param>
    public void AddComponentRenderMode(IComponentRenderMode? renderMode)
    {
        if (renderMode is null)
        {
            return;
        }

        // Note that a ComponentRenderMode frame is technically a child of the Component frame to which it applies,
        // hence the terminology of "adding" it rather than "setting" it. For performance reasons, the diffing system
        // will only look for ComponentRenderMode frames:
        // [a] when the HasCallerSpecifiedRenderMode flag is set on the Component frame
        // [b] up until the first child that is *not* a ComponentRenderMode frame or any other header frame type
        //     that we may define in the future

        var parentFrameIndex = GetCurrentParentFrameIndex();
        if (!parentFrameIndex.HasValue)
        {
            throw new InvalidOperationException("There is no enclosing component frame.");
        }

        var parentFrameIndexValue = parentFrameIndex.Value;
        ref var parentFrame = ref _entries.Buffer[parentFrameIndexValue];
        if (parentFrame.FrameTypeField != RenderTreeFrameType.Component)
        {
            throw new InvalidOperationException($"The enclosing frame is not of the required type '{nameof(RenderTreeFrameType.Component)}'.");
        }

        parentFrame.ComponentFrameFlagsField |= ComponentFrameFlags.HasCallerSpecifiedRenderMode;

        _entries.AppendComponentRenderMode(renderMode);
        _lastNonAttributeFrameType = RenderTreeFrameType.ComponentRenderMode;
    }

    /// <summary>
    /// Assigns a name to an event in the enclosing element.
    /// </summary>
    /// <param name="eventType">The event type, e.g., 'onsubmit'.</param>
    /// <param name="assignedName">The application-assigned name.</param>
    public void AddNamedEvent(string eventType, string assignedName)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentException.ThrowIfNullOrEmpty(assignedName);

        // Note that we could trivially extend this to a generic concept of "named values" that exist within the rendertree
        // and are tracked when added, removed, or updated. Currently we don't need that generality, but if we ever do, we
        // can replace RenderTreeFrameType.NamedEvent with RenderTreeFrameType.NamedValue and use it to implement named events.

        if (GetCurrentParentFrameType() != RenderTreeFrameType.Element)
        {
            throw new InvalidOperationException($"Named events may only be added as children of frames of type {RenderTreeFrameType.Element}");
        }

        _entries.AppendNamedEvent(eventType, assignedName);
        _lastNonAttributeFrameType = RenderTreeFrameType.NamedEvent;
    }

    /// <summary>
    /// Appends a frame representing a region of frames.
    /// </summary>
    /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
    public void OpenRegion(int sequence)
    {
        // We are entering a new scope, since we track the "duplicate attributes" per
        // element/component we might need to clean them up now.
        if (_hasSeenAddMultipleAttributes)
        {
            var indexOfLastElementOrComponent = _openElementIndices.Peek();
            ProcessDuplicateAttributes(first: indexOfLastElementOrComponent + 1);
        }

        _openElementIndices.Push(_entries.Count);
        _entries.AppendRegion(sequence);
        _lastNonAttributeFrameType = RenderTreeFrameType.Region;
    }

    /// <summary>
    /// Marks a previously appended region frame as closed. Calls to this method
    /// must be balanced with calls to <see cref="OpenRegion(int)"/>.
    /// </summary>
    public void CloseRegion()
    {
        var indexOfEntryBeingClosed = _openElementIndices.Pop();
        _entries.Buffer[indexOfEntryBeingClosed].RegionSubtreeLengthField = _entries.Count - indexOfEntryBeingClosed;
    }

    private void AssertCanAddAttribute()
    {
        if (_lastNonAttributeFrameType != RenderTreeFrameType.Element
            && _lastNonAttributeFrameType != RenderTreeFrameType.Component)
        {
            throw new InvalidOperationException($"Attributes may only be added immediately after frames of type {RenderTreeFrameType.Element} or {RenderTreeFrameType.Component}");
        }
    }

    private void AssertCanAddComponentParameter()
    {
        if (_lastNonAttributeFrameType != RenderTreeFrameType.Component)
        {
            throw new InvalidOperationException($"Component parameters may only be added immediately after frames of type {RenderTreeFrameType.Component}");
        }
    }

    private int? GetCurrentParentFrameIndex()
        => _openElementIndices.Count == 0 ? (int?)null : _openElementIndices.Peek();

    private RenderTreeFrameType? GetCurrentParentFrameType()
    {
        var parentIndex = GetCurrentParentFrameIndex();
        return parentIndex.HasValue
            ? _entries.Buffer[parentIndex.Value].FrameTypeField
            : (RenderTreeFrameType?)null;
    }

    /// <summary>
    /// Clears the builder.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
        _openElementIndices.Clear();
        _lastNonAttributeFrameType = null;
        _hasSeenAddMultipleAttributes = false;
        _seenAttributeNames?.Clear();
    }

    // internal because this should only be used during the post-event tree patching logic
    // It's expensive because it involves copying all the subsequent memory in the array
    internal bool InsertAttributeExpensive(int insertAtIndex, int sequence, string attributeName, object? attributeValue)
    {
        // Replicate the same attribute omission logic as used elsewhere
        if ((attributeValue == null) || (attributeValue is bool boolValue && !boolValue))
        {
            return false;
        }

        _entries.InsertExpensive(insertAtIndex, RenderTreeFrame.Attribute(sequence, attributeName, attributeValue));
        return true;
    }

    /// <summary>
    /// Returns the <see cref="RenderTreeFrame"/> values that have been appended.
    /// </summary>
    /// <returns>An array range of <see cref="RenderTreeFrame"/> values.</returns>
    public ArrayRange<RenderTreeFrame> GetFrames() =>
        _entries.ToRange();

    internal void AssertTreeIsValid(IComponent component)
    {
        if (_openElementIndices.Count > 0)
        {
            // It's never valid to leave an element/component/region unclosed. Doing so
            // could cause undefined behavior in diffing.
            ref var invalidFrame = ref _entries.Buffer[_openElementIndices.Peek()];
            throw new InvalidOperationException($"Render output is invalid for component of type '{component.GetType().FullName}'. A frame of type '{invalidFrame.FrameType}' was left unclosed. Do not use try/catch inside rendering logic, because partial output cannot be undone.");
        }
    }

    // Internal for testing
    internal void ProcessDuplicateAttributes(int first)
    {
        Debug.Assert(_hasSeenAddMultipleAttributes);

        // When AddMultipleAttributes method has been called, we need to postprocess attributes while closing
        // the element/component. However, we also don't know the end index we should look at because it
        // will contain nested content.
        var buffer = _entries.Buffer;
        var last = _entries.Count - 1;

        for (var i = first; i <= last; i++)
        {
            if (buffer[i].FrameTypeField != RenderTreeFrameType.Attribute)
            {
                last = i - 1;
                break;
            }
        }

        // Now that we've found the last attribute, we can iterate backwards and process duplicates.
        var seenAttributeNames = (_seenAttributeNames ??= new Dictionary<string, int>(SimplifiedStringHashComparer.Instance));
        for (var i = last; i >= first; i--)
        {
            ref var frame = ref buffer[i];
            Debug.Assert(frame.FrameTypeField == RenderTreeFrameType.Attribute, $"Frame type is {frame.FrameTypeField} at {i}");

            if (!seenAttributeNames.TryAdd(frame.AttributeNameField, i))
            {
                var index = seenAttributeNames[frame.AttributeNameField];
                if (index < i)
                {
                    // This attribute is overriding a "silent frame" where we didn't create a frame for an AddAttribute call.
                    // This is the case for a null event handler, or bool false value.
                    //
                    // We need to update our tracking, in case the attribute appeared 3 or more times.
                    seenAttributeNames[frame.AttributeNameField] = i;
                }
                else if (index > i)
                {
                    // This attribute has been overridden. For now, blank out its name to *mark* it. We'll do a pass
                    // later to wipe it out.
                    frame = default;
                }
                else
                {
                    // OK so index == i. How is that possible? Well it's possible for a "silent frame" immediately
                    // followed by setting the same attribute. Think of it this way, when we create a "silent frame"
                    // we have to track that attribute name with *some* index.
                    //
                    // The only index value we can safely use is _entries.Count (next available). This is fine because
                    // we never use these indexes to look stuff up, only for comparison.
                    //
                    // That gets you here, and there's no action to take.
                }
            }
        }

        // This is the pass where we cleanup attributes that have been wiped out.
        //
        // We copy the entries we're keeping into the earlier parts of the list (preserving order).
        //
        // Note that we iterate to the end of the list here, there might be additional frames after the attributes
        // (ref) or content) that need to move to the left.
        var offset = first;
        for (var i = first; i < _entries.Count; i++)
        {
            ref var frame = ref buffer[i];
            if (frame.FrameTypeField != RenderTreeFrameType.None)
            {
                buffer[offset++] = frame;
            }
        }

        // Clean up now unused space at the end of the list.
        var residue = _entries.Count - offset;
        for (var i = 0; i < residue; i++)
        {
            _entries.RemoveLast();
        }

        seenAttributeNames.Clear();
        _hasSeenAddMultipleAttributes = false;
    }

    // Internal for testing
    internal void TrackAttributeName(string name)
    {
        if (!_hasSeenAddMultipleAttributes)
        {
            return;
        }

        var seenAttributeNames = (_seenAttributeNames ??= new Dictionary<string, int>(SimplifiedStringHashComparer.Instance));
        seenAttributeNames[name] = _entries.Count; // See comment in ProcessAttributes for why this is OK.
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _entries.Dispose();
    }
}
