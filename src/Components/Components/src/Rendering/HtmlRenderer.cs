// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;

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

        private static readonly Task CanceledRenderTask = Task.FromCanceled(new CancellationToken(canceled: true));

        private readonly Func<string, string> _htmlEncoder;

        /// <summary>
        /// Initializes a new instance of <see cref="HtmlRenderer"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to use to instantiate components.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="htmlEncoder">A <see cref="Func{T, TResult}"/> that will HTML encode the given string.</param>
        public HtmlRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, Func<string, string> htmlEncoder)
            : base(serviceProvider, loggerFactory)
        {
            _htmlEncoder = htmlEncoder;
        }

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        /// <inheritdoc />
        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            // By default we return a canceled task. This has the effect of making it so that the
            // OnAfterRenderAsync callbacks on components don't run by default.
            // This way, by default prerendering gets the correct behavior and other renderers
            // override the UpdateDisplayAsync method already, so those components can
            // either complete a task when the client acknowledges the render, or return a canceled task
            // when the renderer gets disposed.

            // We believe that returning a canceled task is the right behavior as we expect that any class
            // that subclasses this class to provide an implementation for a given rendering scenario respects
            // the contract that OnAfterRender should only be called when the display has successfully been updated
            // and the application is interactive. (Element and component references are populated and JavaScript interop
            // is available).
            return CanceledRenderTask;
        }

        /// <summary>
        /// Renders a component into a sequence of <see cref="string"/> fragments that represent the textual representation
        /// of the HTML produced by the component.
        /// </summary>
        /// <param name="componentType">The type of the <see cref="IComponent"/>.</param>
        /// <param name="initialParameters">A <see cref="ParameterView"/> with the initial parameters to render the component.</param>
        /// <returns>A <see cref="Task"/> that on completion returns a sequence of <see cref="string"/> fragments that represent the HTML text of the component.</returns>
        public async Task<ComponentRenderedText> RenderComponentAsync(Type componentType, ParameterView initialParameters)
        {
            var (componentId, frames) = await CreateInitialRenderAsync(componentType, initialParameters);

            var context = new HtmlRenderingContext();
            var newPosition = RenderFrames(context, frames, 0, frames.Count);
            Debug.Assert(newPosition == frames.Count);
            return new ComponentRenderedText(componentId, context.Result);
        }

        /// <summary>
        /// Renders a component into a sequence of <see cref="string"/> fragments that represent the textual representation
        /// of the HTML produced by the component.
        /// </summary>
        /// <typeparam name="TComponent">The type of the <see cref="IComponent"/>.</typeparam>
        /// <param name="initialParameters">A <see cref="ParameterView"/> with the initial parameters to render the component.</param>
        /// <returns>A <see cref="Task"/> that on completion returns a sequence of <see cref="string"/> fragments that represent the HTML text of the component.</returns>
        public Task<ComponentRenderedText> RenderComponentAsync<TComponent>(ParameterView initialParameters) where TComponent : IComponent
        {
            return RenderComponentAsync(typeof(TComponent), initialParameters);
        }

        /// <inheritdoc />
        protected override void HandleException(Exception exception)
            => ExceptionDispatchInfo.Capture(exception).Throw();

        private int RenderFrames(HtmlRenderingContext context, ArrayRange<RenderTreeFrame> frames, int position, int maxElements)
        {
            var nextPosition = position;
            var endPosition = position + maxElements;
            while (position < endPosition)
            {
                nextPosition = RenderCore(context, frames, position);
                if (position == nextPosition)
                {
                    throw new InvalidOperationException("We didn't consume any input.");
                }
                position = nextPosition;
            }

            return nextPosition;
        }

        private int RenderCore(
            HtmlRenderingContext context,
            ArrayRange<RenderTreeFrame> frames,
            int position)
        {
            ref var frame = ref frames.Array[position];
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Element:
                    return RenderElement(context, frames, position);
                case RenderTreeFrameType.Attribute:
                    throw new InvalidOperationException($"Attributes should only be encountered within {nameof(RenderElement)}");
                case RenderTreeFrameType.Text:
                    context.Result.Add(_htmlEncoder(frame.TextContent));
                    return ++position;
                case RenderTreeFrameType.Markup:
                    context.Result.Add(frame.MarkupContent);
                    return ++position;
                case RenderTreeFrameType.Component:
                    return RenderChildComponent(context, frames, position);
                case RenderTreeFrameType.Region:
                    return RenderFrames(context, frames, position + 1, frame.RegionSubtreeLength - 1);
                case RenderTreeFrameType.ElementReferenceCapture:
                case RenderTreeFrameType.ComponentReferenceCapture:
                    return ++position;
                default:
                    throw new InvalidOperationException($"Invalid element frame type '{frame.FrameType}'.");
            }
        }

        private int RenderChildComponent(
            HtmlRenderingContext context,
            ArrayRange<RenderTreeFrame> frames,
            int position)
        {
            ref var frame = ref frames.Array[position];
            var childFrames = GetCurrentRenderTreeFrames(frame.ComponentId);
            RenderFrames(context, childFrames, 0, childFrames.Count);
            return position + frame.ComponentSubtreeLength;
        }

        private int RenderElement(
            HtmlRenderingContext context,
            ArrayRange<RenderTreeFrame> frames,
            int position)
        {
            ref var frame = ref frames.Array[position];
            var result = context.Result;
            result.Add("<");
            result.Add(frame.ElementName);
            var afterAttributes = RenderAttributes(context, frames, position + 1, frame.ElementSubtreeLength - 1, out var capturedValueAttribute);

            // When we see an <option> as a descendant of a <select>, and the option's "value" attribute matches the
            // "value" attribute on the <select>, then we auto-add the "selected" attribute to that option. This is
            // a way of converting Blazor's select binding feature to regular static HTML.
            if (context.ClosestSelectValueAsString != null
                && string.Equals(frame.ElementName, "option", StringComparison.OrdinalIgnoreCase)
                && string.Equals(capturedValueAttribute, context.ClosestSelectValueAsString, StringComparison.Ordinal))
            {
                result.Add(" selected");
            }

            var remainingElements = frame.ElementSubtreeLength + position - afterAttributes;
            if (remainingElements > 0)
            {
                result.Add(">");

                var isSelect = string.Equals(frame.ElementName, "select", StringComparison.OrdinalIgnoreCase);
                if (isSelect)
                {
                    context.ClosestSelectValueAsString = capturedValueAttribute;
                }

                var afterElement = RenderChildren(context, frames, afterAttributes, remainingElements);

                if (isSelect)
                {
                    // There's no concept of nested <select> elements, so as soon as we're exiting one of them,
                    // we can safely say there is no longer any value for this
                    context.ClosestSelectValueAsString = null;
                }

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

        private int RenderChildren(HtmlRenderingContext context, ArrayRange<RenderTreeFrame> frames, int position, int maxElements)
        {
            if (maxElements == 0)
            {
                return position;
            }

            return RenderFrames(context, frames, position, maxElements);
        }

        private int RenderAttributes(
            HtmlRenderingContext context,
            ArrayRange<RenderTreeFrame> frames, int position, int maxElements, out string capturedValueAttribute)
        {
            capturedValueAttribute = null;

            if (maxElements == 0)
            {
                return position;
            }

            var result = context.Result;

            for (var i = 0; i < maxElements; i++)
            {
                var candidateIndex = position + i;
                ref var frame = ref frames.Array[candidateIndex];
                if (frame.FrameType != RenderTreeFrameType.Attribute)
                {
                    return candidateIndex;
                }

                if (frame.AttributeName.Equals("value", StringComparison.OrdinalIgnoreCase))
                {
                    capturedValueAttribute = frame.AttributeValue as string;
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

        private async Task<(int, ArrayRange<RenderTreeFrame>)> CreateInitialRenderAsync(Type componentType, ParameterView initialParameters)
        {
            var component = InstantiateComponent(componentType);
            var componentId = AssignRootComponentId(component);

            await RenderRootComponentAsync(componentId, initialParameters);

            return (componentId, GetCurrentRenderTreeFrames(componentId));
        }

        private class HtmlRenderingContext
        {
            public List<string> Result { get; } = new List<string>();

            public string ClosestSelectValueAsString { get; set; }
        }
    }
}

