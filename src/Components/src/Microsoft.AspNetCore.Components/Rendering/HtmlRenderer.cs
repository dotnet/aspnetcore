// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Rendering
{
    /// <summary>
    /// A <see cref="Renderer"/> that produces HTML.
    /// </summary>
    public class HtmlRenderer : Renderer
    {
        private static readonly HashSet<string> SelfClosingElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"
        };

        private readonly Func<string, string> _htmlEncoder;

        /// <summary>
        /// Initializes a new instance of <see cref="HtmlRenderer"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to use to instantiate components.</param>
        /// <param name="htmlEncoder">A <see cref="Func{T, TResult}"/> that will HTML encode the given string.</param>
        public HtmlRenderer(IServiceProvider serviceProvider, Func<string, string> htmlEncoder) : base(serviceProvider)
        {
            _htmlEncoder = htmlEncoder;
        }

        /// <inheritdoc />
        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Renders a component into a sequence of <see cref="string"/> fragments that represent the textual representation
        /// of the HTML produced by the component.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="IComponent"/>.</typeparam>
        /// <param name="initialParameters">A <see cref="ParameterCollection"/> with the initial parameters to render the component.</param>
        /// <returns>A sequence of <see cref="string"/> fragments that represent the HTML text of the component.</returns>
        public IEnumerable<string> RenderComponent<T>(ParameterCollection initialParameters) where T : IComponent
        {
            return RenderComponent(typeof(T), initialParameters);
        }

        /// <summary>
        /// Renders a component into a sequence of <see cref="string"/> fragments that represent the textual representation
        /// of the HTML produced by the component.
        /// </summary>
        /// <param name="componentType">The type of the <see cref="IComponent"/>.</param>
        /// <param name="initialParameters">A <see cref="ParameterCollection"/> with the initial parameters to render the component.</param>
        /// <returns>A sequence of <see cref="string"/> fragments that represent the HTML text of the component.</returns>
        private IEnumerable<string> RenderComponent(Type componentType, ParameterCollection initialParameters)
        {
            var frames = CreateInitialRender(componentType, initialParameters);

            if (frames.Count == 0)
            {
                return Array.Empty<string>();
            }
            else
            {
                var result = new List<string>();
                var newPosition = RenderFrames(result, frames, 0, frames.Count);
                Debug.Assert(newPosition == frames.Count);
                return result;
            }
        }

        private int RenderFrames(List<string> result, ArrayRange<RenderTreeFrame> frames, int position, int maxElements)
        {
            var nextPosition = position;
            var endPosition = position + maxElements;
            while (position < endPosition)
            {
                nextPosition = RenderCore(result, frames, position, maxElements);
                if (position == nextPosition)
                {
                    throw new InvalidOperationException("We didn't consume any input.");
                }
                position = nextPosition;
            }

            return nextPosition;
        }

        private int RenderCore(
            List<string> result,
            ArrayRange<RenderTreeFrame> frames,
            int position,
            int length)
        {
            ref var frame = ref frames.Array[position];
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Element:
                    return RenderElement(result, frames, position);
                case RenderTreeFrameType.Attribute:
                    return RenderAttributes(result, frames, position, 1);
                case RenderTreeFrameType.Text:
                    result.Add(_htmlEncoder(frame.TextContent));
                    return ++position;
                case RenderTreeFrameType.Markup:
                    result.Add(frame.MarkupContent);
                    return ++position;
                case RenderTreeFrameType.Component:
                    return RenderChildComponent(result, frames, position);
                case RenderTreeFrameType.Region:
                    return RenderFrames(result, frames, position + 1, frame.RegionSubtreeLength - 1);
                case RenderTreeFrameType.ElementReferenceCapture:
                case RenderTreeFrameType.ComponentReferenceCapture:
                    return ++position;
                default:
                    throw new InvalidOperationException($"Invalid element frame type '{frame.FrameType}'.");
            }
        }

        private int RenderChildComponent(
            List<string> result,
            ArrayRange<RenderTreeFrame> frames,
            int position)
        {
            ref var frame = ref frames.Array[position];
            var childFrames = GetCurrentRenderTreeFrames(frame.ComponentId);
            RenderFrames(result, childFrames, 0, childFrames.Count);
            return position + frame.ComponentSubtreeLength;
        }

        private int RenderElement(
            List<string> result,
            ArrayRange<RenderTreeFrame> frames,
            int position)
        {
            ref var frame = ref frames.Array[position];
            result.Add("<");
            result.Add(frame.ElementName);
            var afterAttributes = RenderAttributes(result, frames, position + 1, frame.ElementSubtreeLength - 1);
            var remainingElements = frame.ElementSubtreeLength + position - afterAttributes;
            if (remainingElements > 0)
            {
                result.Add(">");
                var afterElement = RenderChildren(result, frames, afterAttributes, remainingElements);
                result.Add("</");
                result.Add(frame.ElementName);
                result.Add(">");
                Debug.Assert(afterElement == position + frame.ElementSubtreeLength);
                return afterElement;
            }
            else
            {
                if (SelfClosingElements.Contains(frame.ElementName))
                {
                    result.Add(" />");
                }
                else
                {
                    result.Add(">");
                    result.Add("</");
                    result.Add(frame.ElementName);
                    result.Add(">");
                }
                Debug.Assert(afterAttributes == position + frame.ElementSubtreeLength);
                return afterAttributes;
            }
        }

        private int RenderChildren(List<string> result, ArrayRange<RenderTreeFrame> frames, int position, int maxElements)
        {
            if (maxElements == 0)
            {
                return position;
            }

            return RenderFrames(result, frames, position, maxElements);
        }

        private int RenderAttributes(
            List<string> result,
            ArrayRange<RenderTreeFrame> frames, int position, int maxElements)
        {
            if (maxElements == 0)
            {
                return position;
            }

            for (var i = 0; i < maxElements; i++)
            {
                var candidateIndex = position + i;
                ref var frame = ref frames.Array[candidateIndex];
                if (frame.FrameType != RenderTreeFrameType.Attribute)
                {
                    return candidateIndex;
                }

                switch (frame.AttributeValue)
                {
                    case bool flag when flag:
                        result.Add(" ");
                        result.Add(frame.AttributeName);
                        break;
                    case string value:
                        result.Add(" ");
                        result.Add(frame.AttributeName);
                        result.Add("=");
                        result.Add("\"");
                        result.Add(_htmlEncoder(value));
                        result.Add("\"");
                        break;
                    default:
                        break;
                }
            }

            return position + maxElements;
        }

        private ArrayRange<RenderTreeFrame> CreateInitialRender(Type componentType, ParameterCollection initialParameters)
        {
            var component = InstantiateComponent(componentType);
            var componentId = AssignRootComponentId(component);

            RenderRootComponent(componentId, initialParameters);

            return GetCurrentRenderTreeFrames(componentId);
        }
    }
}

