// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    // IMPORTANT
    //
    // Many of these names are used in code generation. Keep these in sync with the code generation code
    // See: src/Microsoft.AspNetCore.Components.Razor.Extensions/BlazorApi.cs

    /// <summary>
    /// Provides methods for building a collection of <see cref="RenderTreeFrame"/> entries.
    /// </summary>
    public class RenderTreeBuilder
    {
        private readonly static object BoxedTrue = true;
        private readonly static object BoxedFalse = false;
        private readonly static string ComponentReferenceCaptureInvalidParentMessage = $"Component reference captures may only be added as children of frames of type {RenderTreeFrameType.Component}";

        private readonly Renderer _renderer;
        private readonly ArrayBuilder<RenderTreeFrame> _entries = new ArrayBuilder<RenderTreeFrame>(10);
        private readonly Stack<int> _openElementIndices = new Stack<int>();
        private RenderTreeFrameType? _lastNonAttributeFrameType;

        /// <summary>
        /// The reserved parameter name used for supplying child content.
        /// </summary>
        public const string ChildContent = nameof(ChildContent);

        /// <summary>
        /// Constructs an instance of <see cref="RenderTreeBuilder"/>.
        /// </summary>
        /// <param name="renderer">The associated <see cref="Renderer"/>.</param>
        public RenderTreeBuilder(Renderer renderer)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

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
            _openElementIndices.Push(_entries.Count);
            Append(RenderTreeFrame.Element(sequence, elementName));
        }

        /// <summary>
        /// Marks a previously appended element frame as closed. Calls to this method
        /// must be balanced with calls to <see cref="OpenElement(int, string)"/>.
        /// </summary>
        public void CloseElement()
        {
            var indexOfEntryBeingClosed = _openElementIndices.Pop();
            ref var entry = ref _entries.Buffer[indexOfEntryBeingClosed];
            entry = entry.WithElementSubtreeLength(_entries.Count - indexOfEntryBeingClosed);
        }

        /// <summary>
        /// Appends a frame representing markup content.
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="markupContent">Content for the new markup frame.</param>
        public void AddMarkupContent(int sequence, string markupContent)
            => Append(RenderTreeFrame.Markup(sequence, markupContent ?? string.Empty));

        /// <summary>
        /// Appends a frame representing text content.
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="textContent">Content for the new text frame.</param>
        public void AddContent(int sequence, string textContent)
            => Append(RenderTreeFrame.Text(sequence, textContent ?? string.Empty));

        /// <summary>
        /// Appends frames representing an arbitrary fragment of content.
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="fragment">Content to append.</param>
        public void AddContent(int sequence, RenderFragment fragment)
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
        public void AddContent<T>(int sequence, RenderFragment<T> fragment, T value)
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
        /// <param name="markupContent">Content for the new markup frame.</param>
        public void AddContent(int sequence, MarkupString markupContent)
            => AddMarkupContent(sequence, markupContent.Value);

        /// <summary>
        /// Appends a frame representing text content.
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="textContent">Content for the new text frame.</param>
        public void AddContent(int sequence, object textContent)
            => AddContent(sequence, textContent?.ToString());

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
                Append(RenderTreeFrame.Attribute(sequence, name, value ? BoxedTrue : BoxedFalse));
            }
            else if (value)
            {
                // Don't add 'false' attributes for elements. We want booleans to map to the presence
                // or absence of an attribute, and false => "False" which isn't falsy in js.
                Append(RenderTreeFrame.Attribute(sequence, name, BoxedTrue));
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
        public void AddAttribute(int sequence, string name, string value)
        {
            AssertCanAddAttribute();
            if (value != null || _lastNonAttributeFrameType == RenderTreeFrameType.Component)
            {
                Append(RenderTreeFrame.Attribute(sequence, name, value));
            }
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Action"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public void AddAttribute(int sequence, string name, Action value)
        {
            AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing an <see cref="Action{UIEventArgs}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public void AddAttribute(int sequence, string name, Action<UIEventArgs> value)
        {
            AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing a <see cref="Func{Task}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public void AddAttribute(int sequence, string name, Func<Task> value)
        {
            AddAttribute(sequence, name, (MulticastDelegate)value);
        }

        /// <summary>
        /// <para>
        /// Appends a frame representing a <see cref="Func{UIEventArgs, Task}"/>-valued attribute.
        /// </para>
        /// <para>
        /// The attribute is associated with the most recently added element. If the value is <c>null</c> and the
        /// current element is not a component, the frame will be omitted.
        /// </para>
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        public void AddAttribute(int sequence, string name, Func<UIEventArgs, Task> value)
        {
            AddAttribute(sequence, name, (MulticastDelegate)value);
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
        /// <remarks>
        /// This method is provided for infrastructure purposes, and is used to be
        /// <see cref="UIEventArgsRenderTreeBuilderExtensions"/> to provide support for delegates of specific
        /// types. For a good programming experience when using a custom delegate type, define an
        /// extension method similar to 
        /// <see cref="UIEventArgsRenderTreeBuilderExtensions.AddAttribute(RenderTreeBuilder, int, string, Action{UIChangeEventArgs})"/>
        /// that calls this method.
        /// </remarks>
        public void AddAttribute(int sequence, string name, MulticastDelegate value)
        {
            AssertCanAddAttribute();
            if (value != null || _lastNonAttributeFrameType == RenderTreeFrameType.Component)
            {
                Append(RenderTreeFrame.Attribute(sequence, name, value));
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
        public void AddAttribute(int sequence, string name, object value)
        {
            // This looks a bit daunting because we need to handle the boxed/object version of all of the
            // types that AddAttribute special cases.
            if (_lastNonAttributeFrameType == RenderTreeFrameType.Element)
            {
                if (value == null)
                {
                    // Do nothing, treat 'null' attribute values for elements as a conditional attribute.
                }
                else if (value is bool boolValue)
                {
                    if (boolValue)
                    {
                        Append(RenderTreeFrame.Attribute(sequence, name, BoxedTrue));
                    }

                    // Don't add anything for false bool value.
                }
                else if (value is MulticastDelegate)
                {
                    Append(RenderTreeFrame.Attribute(sequence, name, value));
                }
                else
                {
                    // The value is either a string, or should be treated as a string.
                    Append(RenderTreeFrame.Attribute(sequence, name, value.ToString()));
                }
            }
            else if (_lastNonAttributeFrameType == RenderTreeFrameType.Component)
            {
                Append(RenderTreeFrame.Attribute(sequence, name, value));
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
        public void AddAttribute(int sequence, in RenderTreeFrame frame)
        {
            if (frame.FrameType != RenderTreeFrameType.Attribute)
            {
                throw new ArgumentException($"The {nameof(frame.FrameType)} must be {RenderTreeFrameType.Attribute}.");
            }

            AssertCanAddAttribute();
            Append(frame.WithAttributeSequence(sequence));
        }

        /// <summary>
        /// Appends a frame representing a child component.
        /// </summary>
        /// <typeparam name="TComponent">The type of the child component.</typeparam>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        public void OpenComponent<TComponent>(int sequence) where TComponent : IComponent
            => OpenComponentUnchecked(sequence, typeof(TComponent));

        /// <summary>
        /// Appends a frame representing a child component.
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="componentType">The type of the child component.</param>
        public void OpenComponent(int sequence, Type componentType)
        {
            if (!typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"The component type must implement {typeof(IComponent).FullName}.");
            }

            OpenComponentUnchecked(sequence, componentType);
        }

        private void OpenComponentUnchecked(int sequence, Type componentType)
        {
            _openElementIndices.Push(_entries.Count);
            Append(RenderTreeFrame.ChildComponent(sequence, componentType));
        }

        /// <summary>
        /// Marks a previously appended component frame as closed. Calls to this method
        /// must be balanced with calls to <see cref="OpenComponent{TComponent}"/>.
        /// </summary>
        public void CloseComponent()
        {
            var indexOfEntryBeingClosed = _openElementIndices.Pop();
            ref var entry = ref _entries.Buffer[indexOfEntryBeingClosed];
            entry = entry.WithComponentSubtreeLength(_entries.Count - indexOfEntryBeingClosed);
        }

        /// <summary>
        /// Appends a frame representing an instruction to capture a reference to the parent element.
        /// </summary>
        /// <param name="sequence">An integer that represents the position of the instruction in the source code.</param>
        /// <param name="elementReferenceCaptureAction">An action to be invoked whenever the reference value changes.</param>
        public void AddElementReferenceCapture(int sequence, Action<ElementRef> elementReferenceCaptureAction)
        {
            if (GetCurrentParentFrameType() != RenderTreeFrameType.Element)
            {
                throw new InvalidOperationException($"Element reference captures may only be added as children of frames of type {RenderTreeFrameType.Element}");
            }

            Append(RenderTreeFrame.ElementReferenceCapture(sequence, elementReferenceCaptureAction));
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
            if (_entries.Buffer[parentFrameIndexValue].FrameType != RenderTreeFrameType.Component)
            {
                throw new InvalidOperationException(ComponentReferenceCaptureInvalidParentMessage);
            }

            Append(RenderTreeFrame.ComponentReferenceCapture(sequence, componentReferenceCaptureAction, parentFrameIndexValue));
        }

        // Internal for tests
        // Not public because there's no current use case for user code defining regions arbitrarily.
        // Currently the sole use case for regions is when appending a RenderFragment.
        internal void OpenRegion(int sequence)
        {
            _openElementIndices.Push(_entries.Count);
            Append(RenderTreeFrame.Region(sequence));
        }

        // See above for why this is not public
        internal void CloseRegion()
        {
            var indexOfEntryBeingClosed = _openElementIndices.Pop();
            ref var entry = ref _entries.Buffer[indexOfEntryBeingClosed];
            entry = entry.WithRegionSubtreeLength(_entries.Count - indexOfEntryBeingClosed);
        }

        private void AssertCanAddAttribute()
        {
            if (_lastNonAttributeFrameType != RenderTreeFrameType.Element
                && _lastNonAttributeFrameType != RenderTreeFrameType.Component)
            {
                throw new InvalidOperationException($"Attributes may only be added immediately after frames of type {RenderTreeFrameType.Element} or {RenderTreeFrameType.Component}");
            }
        }

        private int? GetCurrentParentFrameIndex()
            => _openElementIndices.Count == 0 ? (int?)null : _openElementIndices.Peek();

        private RenderTreeFrameType? GetCurrentParentFrameType()
        {
            var parentIndex = GetCurrentParentFrameIndex();
            return parentIndex.HasValue
                ? _entries.Buffer[parentIndex.Value].FrameType
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
        }

        /// <summary>
        /// Returns the <see cref="RenderTreeFrame"/> values that have been appended.
        /// </summary>
        /// <returns>An array range of <see cref="RenderTreeFrame"/> values.</returns>
        public ArrayRange<RenderTreeFrame> GetFrames() =>
            _entries.ToRange();

        private void Append(in RenderTreeFrame frame)
        {
            _entries.Append(frame);

            var frameType = frame.FrameType;
            if (frameType != RenderTreeFrameType.Attribute)
            {
                _lastNonAttributeFrameType = frame.FrameType;
            }
        }
    }
}
