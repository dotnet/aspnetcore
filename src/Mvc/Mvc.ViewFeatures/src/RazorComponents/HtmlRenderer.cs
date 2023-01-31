// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Rendering;

internal sealed class HtmlRenderer : Renderer
{
    private static readonly HashSet<string> SelfClosingElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"
    };

    private static readonly Task CanceledRenderTask = Task.FromCanceled(new CancellationToken(canceled: true));
    private readonly IViewBufferScope _viewBufferScope;

    public HtmlRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IViewBufferScope viewBufferScope)
        : base(serviceProvider, loggerFactory)
    {
        _viewBufferScope = viewBufferScope;
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

    public async Task<ComponentRenderedText> RenderComponentAsync(Type componentType, ParameterView initialParameters)
    {
        var component = InstantiateComponent(componentType);
        var componentId = AssignRootComponentId(component);

        await RenderRootComponentAsync(componentId, initialParameters);
        return new ComponentRenderedText(componentId, new ComponentHtmlContent(this, componentId));
    }

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
                context.HtmlContentBuilder.Append(frame.TextContent);
                return ++position;
            case RenderTreeFrameType.Markup:
                context.HtmlContentBuilder.AppendHtml(frame.MarkupContent);
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
        var result = context.HtmlContentBuilder;
        result.AppendHtml("<");
        result.AppendHtml(frame.ElementName);
        int afterElement;
        var isTextArea = string.Equals(frame.ElementName, "textarea", StringComparison.OrdinalIgnoreCase);
        // We don't want to include value attribute of textarea element.
        var afterAttributes = RenderAttributes(context, frames, position + 1, frame.ElementSubtreeLength - 1, !isTextArea, out var capturedValueAttribute);

        // When we see an <option> as a descendant of a <select>, and the option's "value" attribute matches the
        // "value" attribute on the <select>, then we auto-add the "selected" attribute to that option. This is
        // a way of converting Blazor's select binding feature to regular static HTML.
        if (context.ClosestSelectValueAsString != null
            && string.Equals(frame.ElementName, "option", StringComparison.OrdinalIgnoreCase)
            && string.Equals(capturedValueAttribute, context.ClosestSelectValueAsString, StringComparison.Ordinal))
        {
            result.AppendHtml(" selected");
        }

        var remainingElements = frame.ElementSubtreeLength + position - afterAttributes;
        if (remainingElements > 0 || isTextArea)
        {
            result.AppendHtml(">");

            var isSelect = string.Equals(frame.ElementName, "select", StringComparison.OrdinalIgnoreCase);
            if (isSelect)
            {
                context.ClosestSelectValueAsString = capturedValueAttribute;
            }

            if (isTextArea && !string.IsNullOrEmpty(capturedValueAttribute))
            {
                // Textarea is a special type of form field where the value is given as text content instead of a 'value' attribute
                // So, if we captured a value attribute, use that instead of any child content
                result.Append(capturedValueAttribute);
                afterElement = position + frame.ElementSubtreeLength; // Skip descendants
            }
            else
            {
                afterElement = RenderChildren(context, frames, afterAttributes, remainingElements);
            }

            if (isSelect)
            {
                // There's no concept of nested <select> elements, so as soon as we're exiting one of them,
                // we can safely say there is no longer any value for this
                context.ClosestSelectValueAsString = null;
            }

            result.AppendHtml("</");
            result.AppendHtml(frame.ElementName);
            result.AppendHtml(">");
            Debug.Assert(afterElement == position + frame.ElementSubtreeLength);
            return afterElement;
        }
        else
        {
            if (SelfClosingElements.Contains(frame.ElementName))
            {
                result.AppendHtml(" />");
            }
            else
            {
                result.AppendHtml(">");
                result.AppendHtml("</");
                result.AppendHtml(frame.ElementName);
                result.AppendHtml(">");
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

    private static int RenderAttributes(
        HtmlRenderingContext context,
        ArrayRange<RenderTreeFrame> frames, int position, int maxElements, bool includeValueAttribute, out string capturedValueAttribute)
    {
        capturedValueAttribute = null;

        if (maxElements == 0)
        {
            return position;
        }

        var result = context.HtmlContentBuilder;

        for (var i = 0; i < maxElements; i++)
        {
            var candidateIndex = position + i;
            ref var frame = ref frames.Array[candidateIndex];

            if (frame.FrameType != RenderTreeFrameType.Attribute)
            {
                if (frame.FrameType == RenderTreeFrameType.ElementReferenceCapture)
                {
                    continue;
                }

                return candidateIndex;
            }

            if (frame.AttributeName.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                capturedValueAttribute = frame.AttributeValue as string;

                if (!includeValueAttribute)
                {
                    continue;
                }
            }

            switch (frame.AttributeValue)
            {
                case bool flag when flag:
                    result.AppendHtml(" ");
                    result.AppendHtml(frame.AttributeName);
                    break;
                case string value:
                    result.AppendHtml(" ");
                    result.AppendHtml(frame.AttributeName);
                    result.AppendHtml("=");
                    result.AppendHtml("\"");
                    result.Append(value);
                    result.AppendHtml("\"");
                    break;
                default:
                    break;
            }
        }

        return position + maxElements;
    }

    private ViewBuffer GetRenderedHtmlContent(int componentId)
    {
        var viewBuffer = new ViewBuffer(_viewBufferScope, nameof(HtmlRenderer), ViewBuffer.ViewPageSize);
        var context = new HtmlRenderingContext(viewBuffer);

        var frames = GetCurrentRenderTreeFrames(componentId);
        var newPosition = RenderFrames(context, frames, 0, frames.Count);
        Debug.Assert(newPosition == frames.Count);

        return viewBuffer;
    }

    private sealed class HtmlRenderingContext
    {
        public HtmlRenderingContext(ViewBuffer viewBuffer)
        {
            HtmlContentBuilder = viewBuffer;
        }

        public ViewBuffer HtmlContentBuilder { get; }

        public string ClosestSelectValueAsString { get; set; }
    }

    /// <summary>
    /// A <see cref="IHtmlContent"/> that defers rendering component markup until <see cref="IHtmlContent.WriteTo(TextWriter, HtmlEncoder)"/>
    /// is called.
    /// </summary>
    private sealed class ComponentHtmlContent : IHtmlContent
    {
        private readonly HtmlRenderer _renderer;
        private readonly int _componentId;

        public ComponentHtmlContent(HtmlRenderer renderer, int componentId)
        {
            _renderer = renderer;
            _componentId = componentId;
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            var actualHtmlContent = _renderer.GetRenderedHtmlContent(_componentId);
            actualHtmlContent.WriteTo(writer, encoder);
        }
    }
}
