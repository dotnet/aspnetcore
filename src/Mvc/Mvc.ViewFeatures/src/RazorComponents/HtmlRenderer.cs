// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text.Encodings.Web;
using System.Threading.Channels;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceProvider _serviceProvider;
    private Channel<RenderBatch> _streamingRenderBatches;

    public HtmlRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IViewBufferScope viewBufferScope)
        : base(serviceProvider, loggerFactory)
    {
        _viewBufferScope = viewBufferScope;
        _serviceProvider = serviceProvider;
    }

    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    public ChannelReader<RenderBatch> StreamingRenderBatches { get; private set; }

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

        if (_streamingRenderBatches is not null)
        {
            return _streamingRenderBatches.Writer.WriteAsync(renderBatch).AsTask()
                .ContinueWith(_ => CanceledRenderTask, TaskScheduler.Current);
        }

        return CanceledRenderTask;
    }

    public async Task<ComponentRenderedText> RenderComponentAsync(Type componentType, ParameterView initialParameters, bool awaitQuiescence)
    {
        var component = InstantiateComponent(componentType);
        var componentId = AssignRootComponentId(component);

        var quiescenceTask = RenderRootComponentAsync(componentId, initialParameters);

        if (awaitQuiescence)
        {
            await quiescenceTask;
        }
        else
        {
            // For any subsequent render batches, we'll advertise them through a channel so that
            // streaming rendering can observe them
            _streamingRenderBatches = Channel.CreateUnbounded<RenderBatch>();
            StreamingRenderBatches = _streamingRenderBatches.Reader;
            _ = quiescenceTask.ContinueWith(task =>
            {
                _streamingRenderBatches.Writer.Complete(task.Exception);
            }, TaskScheduler.Current); // TODO: Preemptively cancel if request is cancelled?
        }

        return new ComponentRenderedText(componentId, new ComponentHtmlContent(this, componentId));
    }

    public Task<ComponentRenderedText> RenderComponentAsync<TComponent>(ParameterView initialParameters, bool awaitQuiescence) where TComponent : IComponent
    {
        return RenderComponentAsync(typeof(TComponent), initialParameters, awaitQuiescence);
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

        var interactiveMarker = (InteractiveComponentMarker?)null;

        if (frame.ComponentRenderMode != ComponentRenderMode.Unspecified.NumericValue)
        {
            interactiveMarker = context.SerializeInvocation(frames, position, frame.ComponentRenderMode);
            interactiveMarker.Value.AppendPreamble(context.HtmlContentBuilder);
        }

        var childFrames = GetCurrentRenderTreeFrames(frame.ComponentId);
        RenderFrames(context, childFrames, 0, childFrames.Count);

        if (interactiveMarker.HasValue)
        {
            interactiveMarker.Value.AppendEpilogue(context.HtmlContentBuilder);
        }

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
        var afterAttributes = RenderAttributes(context, frames, position + 1, frame.ElementSubtreeLength - 1, out var capturedValueAttribute);

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
        if (remainingElements > 0)
        {
            result.AppendHtml(">");

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
        ArrayRange<RenderTreeFrame> frames, int position, int maxElements, out string capturedValueAttribute)
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
                return candidateIndex;
            }

            if (frame.AttributeName.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                capturedValueAttribute = frame.AttributeValue as string;
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
        var context = new HtmlRenderingContext(viewBuffer, _serviceProvider);

        var frames = GetCurrentRenderTreeFrames(componentId);
        var newPosition = RenderFrames(context, frames, 0, frames.Count);
        Debug.Assert(newPosition == frames.Count);

        return viewBuffer;
    }

    private readonly struct InteractiveComponentMarker
    {
        private readonly ServerComponentMarker? _serverMarker;
        private readonly WebAssemblyComponentMarker? _webAssemblyMarker;

        public InteractiveComponentMarker(ServerComponentMarker marker)
        {
            _serverMarker = marker;
        }

        public InteractiveComponentMarker(WebAssemblyComponentMarker marker)
        {
            _webAssemblyMarker = marker;
        }

        public void AppendPreamble(ViewBuffer htmlContentBuilder)
        {
            if (_serverMarker.HasValue)
            {
                ServerComponentSerializer.AppendPreamble(htmlContentBuilder, _serverMarker.Value);
            }
            else
            {
                WebAssemblyComponentSerializer.AppendPreamble(htmlContentBuilder, _webAssemblyMarker.Value);
            }
        }

        public void AppendEpilogue(ViewBuffer htmlContentBuilder)
        {
            if (_serverMarker.HasValue)
            {
                ServerComponentSerializer.AppendEpilogue(htmlContentBuilder, _serverMarker.Value);
            }
            else
            {
                WebAssemblyComponentSerializer.AppendEpilogue(htmlContentBuilder, _webAssemblyMarker.Value);
            }
        }
    }

    private sealed class HtmlRenderingContext
    {
        private readonly IServiceProvider _serviceProvider;
        private ServerComponentSerializer _serverComponentSerializer;
        private ServerComponentInvocationSequence _serverComponentSequence;

        public HtmlRenderingContext(ViewBuffer viewBuffer, IServiceProvider serviceProvider)
        {
            HtmlContentBuilder = viewBuffer;
            _serviceProvider = serviceProvider;
        }

        public ViewBuffer HtmlContentBuilder { get; }

        public string ClosestSelectValueAsString { get; set; }

        public InteractiveComponentMarker SerializeInvocation(ArrayRange<RenderTreeFrame> frames, int position, byte renderModeNumericValue)
        {
            ref var componentFrame = ref frames.Array[position];
            var parameters = ParameterView.DangerouslyCaptureUnboundComponentParameters(frames, position);

            if (renderModeNumericValue == WebComponentRenderMode.Server.NumericValue)
            {
                if (_serverComponentSerializer is null)
                {
                    var dataProtectionProvider = _serviceProvider.GetRequiredService<IDataProtectionProvider>();
                    _serverComponentSerializer = new ServerComponentSerializer(dataProtectionProvider);
                    _serverComponentSequence = new();
                }

                var marker = _serverComponentSerializer.SerializeInvocation(_serverComponentSequence, componentFrame.ComponentType, parameters, prerendered: true);
                return new InteractiveComponentMarker(marker);
            }

            if (renderModeNumericValue == WebComponentRenderMode.WebAssembly.NumericValue)
            {
                var marker = WebAssemblyComponentSerializer.SerializeInvocation(componentFrame.ComponentType, parameters, prerendered: true);
                return new InteractiveComponentMarker(marker);
            }

            throw new InvalidOperationException($"Component '{componentFrame.ComponentType.Name}' has unsupported render mode {renderModeNumericValue}");
        }
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

