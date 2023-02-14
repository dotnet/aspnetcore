// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
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
    private Channel<StreamingComponentUpdate> _streamingRenderBatches;

    public HtmlRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IViewBufferScope viewBufferScope)
        : base(serviceProvider, loggerFactory)
    {
        _viewBufferScope = viewBufferScope;
        _serviceProvider = serviceProvider;
    }

    public HtmlRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IViewBufferScope viewBufferScope, IComponentActivator componentActivator)
        : base(serviceProvider, loggerFactory, componentActivator)
    {
        _viewBufferScope = viewBufferScope;
        _serviceProvider = serviceProvider;
    }

    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    public ChannelReader<StreamingComponentUpdate> StreamingRenderBatches { get; private set; }

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
            // Note that we cannot pass the actual RenderBatch here because ChannelWriter<T>.WriteAsync
            // and the actual write-to-HTTP-response process are asynchronous, and in the meantime, the
            // underlying RenderBatch buffer may get reused by a subsequent render. But that's OK because
            // we weren't going to use the diffs anyway. All we actually have to pass is the list of
            // components being updated, and then PassiveComponentRenderer can (at an arbitrary later time)
            // flush out the latest content from those components, whether or not it has updated again.

            var update = StreamingComponentUpdate.SnapshotFromRenderBatch(renderBatch);
            if (update.UpdatedComponentIds.Count > 0)
            {
                return _streamingRenderBatches.Writer.WriteAsync(update).AsTask()
                    .ContinueWith(_ => CanceledRenderTask, TaskScheduler.Current);
            }
        }

        return CanceledRenderTask;
    }

    public async Task<ComponentRenderedText> RenderComponentAsync(Type componentType, ParameterView initialParameters)
    {
        var component = InstantiateComponent(componentType);
        var componentId = AssignRootComponentId(component);

        var blockingQuiescenceTask = RenderRootComponentAsync(componentId, initialParameters);
        await blockingQuiescenceTask;

        // For any subsequent render batches, we'll advertise them through a channel so that
        // streaming rendering can observe them
        var streamingQuiescenceTask = WaitForStreamingQuiescence();
        _streamingRenderBatches = Channel.CreateUnbounded<StreamingComponentUpdate>();
        StreamingRenderBatches = _streamingRenderBatches.Reader;
        _ = streamingQuiescenceTask.ContinueWith(task =>
        {
            _streamingRenderBatches.Writer.Complete(task.Exception);
        }, TaskScheduler.Current); // TODO: Preemptively cancel if request is cancelled?

        return new ComponentRenderedText(componentId, new ComponentHtmlContent(this, componentId));
    }

    public Task<ComponentRenderedText> RenderComponentAsync<TComponent>(ParameterView initialParameters) where TComponent : IComponent
    {
        return RenderComponentAsync(typeof(TComponent), initialParameters);
    }

    /// <inheritdoc />
    protected override void HandleException(Exception exception)
        => ExceptionDispatchInfo.Capture(exception).Throw();

    private string RenderTreeToHtmlString(ArrayRange<RenderTreeFrame> frames, int position, int maxElements)
    {
        var viewBuffer = new ViewBuffer(_viewBufferScope, nameof(HtmlRenderer), ViewBuffer.ViewPageSize);
        var context = new HtmlRenderingContext(this, viewBuffer, _serviceProvider);
        RenderFrames(context, frames, position, maxElements);

        using var sw = new StringWriter();
        viewBuffer.WriteTo(sw, HtmlEncoder.Default);
        return sw.GetStringBuilder().ToString();
    }

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

        if (frame.ComponentRenderMode != ComponentRenderMode.Unspecified)
        {
            interactiveMarker = context.SerializeInvocation(frames, position, frame.ComponentRenderMode);
            interactiveMarker.Value.AppendPreamble(context.HtmlContentBuilder);
        }
        else
        {
            // In case we have streaming rendering available, we want streaming SSR to be able to update arbitrary
            // subtrees within the output instead of having to resend the entire tree from the root. So we have to
            // let the client keep track of which parts of the document correspond to which components.
            // TODO: Try to find a way to avoid emitting any such markers before <!DOCTYPE html> as that could confuse
            //       some not-strictly-compliant tech. For example, keep track of whether we've yet called RenderElement
            //       within this flow, and only output the markers when we have. Then if the client can't find the marker
            //       for some content it is given, it assumes it to replace the whole document.
            context.HtmlContentBuilder.AppendHtml("<!--c");
            context.HtmlContentBuilder.AppendHtml(frame.ComponentId.ToString(CultureInfo.InvariantCulture)); // TODO: Avoid this allocation
            context.HtmlContentBuilder.AppendHtml("-->");
        }

        var childFrames = GetCurrentRenderTreeFrames(frame.ComponentId);
        RenderFrames(context, childFrames, 0, childFrames.Count);

        if (interactiveMarker.HasValue)
        {
            interactiveMarker.Value.AppendEpilogue(context.HtmlContentBuilder);
        }
        else
        {
            context.HtmlContentBuilder.AppendHtml("<!--/c-->");
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

            var isTextArea = string.Equals(frame.ElementName, "textarea", StringComparison.OrdinalIgnoreCase);
            int afterElement;
            if (isTextArea && !string.IsNullOrEmpty(capturedValueAttribute))
            {
                // Textarea is a special type of form field where the value is given as text content instead of a 'value' attribute
                // So, if we captured a value attribute, use that instead of any child content
                result.Append(capturedValueAttribute);
                afterElement = position + frame.ElementSubtreeLength; // Skip descendants
            }
            else
            {
                // Render descendants
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

    internal ViewBuffer GetRenderedHtmlContent(int componentId)
    {
        // We're about to walk through buffers (RenderTreeBuilder instances) that can get mutated during rendering
        // so it's essential that:
        // [1] ... our access is exclusive, which we validate by calling Dispatcher.AssertAccess
        // [2] ... this method's output is self-contained (i.e., doesn't point to anything in the mutable buffers)
        // [3] ... this method is synchronous, because if we yield, then during that time the buffers could mutate
        Dispatcher.AssertAccess();

        var viewBuffer = new ViewBuffer(_viewBufferScope, nameof(HtmlRenderer), ViewBuffer.ViewPageSize);
        var context = new HtmlRenderingContext(this, viewBuffer, _serviceProvider);

        var frames = GetCurrentRenderTreeFrames(componentId);
        var newPosition = RenderFrames(context, frames, 0, frames.Count);
        Debug.Assert(newPosition == frames.Count);

        return viewBuffer;
    }

    public Task VisitRenderedHtmlSubtrees(IEnumerable<int> componentIds, Func<int, ViewBuffer, Task> visitor)
    {
        return Dispatcher.InvokeAsync(async () =>
        {
            foreach (var componentId in componentIds)
            {
                var viewBuffer = GetRenderedHtmlContent(componentId);
                await visitor(componentId, viewBuffer);
                Console.WriteLine(componentId);
            }
        });
    }

    private readonly struct InteractiveComponentMarker
    {
        private readonly ServerComponentMarker? _serverMarker;
        private readonly WebAssemblyComponentMarker? _webAssemblyMarker;

        public InteractiveComponentMarker(ServerComponentMarker? serverMarker, WebAssemblyComponentMarker? webAssemblyMarker)
        {
            _serverMarker = serverMarker;
            _webAssemblyMarker = webAssemblyMarker;
        }

        public void AppendPreamble(ViewBuffer htmlContentBuilder)
        {
            if (_serverMarker.HasValue)
            {
                if (_webAssemblyMarker.HasValue)
                {
                    AutoComponentSerializer.AppendPreamble(htmlContentBuilder, _serverMarker.Value, _webAssemblyMarker.Value);
                }
                else
                {
                    ServerComponentSerializer.AppendPreamble(htmlContentBuilder, _serverMarker.Value);
                }
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
                if (_webAssemblyMarker.HasValue)
                {
                    AutoComponentSerializer.AppendEpilogue(htmlContentBuilder, _serverMarker.Value, _webAssemblyMarker.Value);
                }
                else
                {
                    ServerComponentSerializer.AppendEpilogue(htmlContentBuilder, _serverMarker.Value);
                }
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
        private readonly HtmlRenderer _htmlRenderer;
        private ServerComponentSerializer _serverComponentSerializer;
        private ServerComponentInvocationSequence _serverComponentSequence;

        public HtmlRenderingContext(HtmlRenderer htmlRenderer, ViewBuffer viewBuffer, IServiceProvider serviceProvider)
        {
            HtmlContentBuilder = viewBuffer;
            _htmlRenderer = htmlRenderer;
            _serviceProvider = serviceProvider;
        }

        public ViewBuffer HtmlContentBuilder { get; }

        public string ClosestSelectValueAsString { get; set; }

        public InteractiveComponentMarker SerializeInvocation(ArrayRange<RenderTreeFrame> frames, int position, ComponentRenderMode renderMode)
        {
            ref var componentFrame = ref frames.Array[position];
            var parameters = ParameterView.DangerouslyCaptureUnboundComponentParameters(frames, position);

            ServerComponentMarker? serverMarker = null;
            WebAssemblyComponentMarker? webAssemblyMarker = null;

            if (renderMode == WebComponentRenderMode.Server
                || renderMode == WebComponentRenderMode.Auto)
            {
                if (_serverComponentSerializer is null)
                {
                    var dataProtectionProvider = _serviceProvider.GetRequiredService<IDataProtectionProvider>();
                    _serverComponentSerializer = new ServerComponentSerializer(dataProtectionProvider);
                    _serverComponentSequence = new();
                }

                serverMarker = _serverComponentSerializer.SerializeInvocation(_serverComponentSequence, componentFrame.ComponentType, parameters, prerendered: true, PrepareRenderFragment);
            }

            if (renderMode == WebComponentRenderMode.WebAssembly
                || renderMode == WebComponentRenderMode.Auto)
            {
                webAssemblyMarker = WebAssemblyComponentSerializer.SerializeInvocation(componentFrame.ComponentType, parameters, prerendered: true, PrepareRenderFragment);
            }

            if (!serverMarker.HasValue && !webAssemblyMarker.HasValue)
            {
                throw new InvalidOperationException($"Component '{componentFrame.ComponentType.Name}' has unsupported render mode {renderMode}");
            }

            return new InteractiveComponentMarker(serverMarker, webAssemblyMarker);
        }

        private string PrepareRenderFragment(RenderFragment fragment)
        {
            // We can't just execute the RenderFragment delegate directly. We have to run it through the
            // renderer so that the renderer can do all the normal things to activate child components
            // and run their full lifecycle (disposal, etc.)
            var rootComponent = new FragmentRenderer { Fragment = fragment };
            string initialHtml = null;
            var renderTask = _htmlRenderer.Dispatcher.InvokeAsync(() =>
            {
                // WARNING: THIS IS NOT CORRECT AND CAN CAUSE AN INFINITE LOOP
                // We should *not* really be creating new root components as a side-effect of
                // parameter serialization, because that means the child content within this
                // RenderFragment subtree is recreated on each parameter serialization and may
                // be repeating work, or worse, keep causing ancestor components to re-render
                // and hence an infinite loop.
                // Instead, all this work should be done in a completely different place: the
                // diffing system. When the diffing system sees that a child component has "interactive"
                // rendermode, any RenderFragment parameters should be handled by creating something
                // like a FragmentRenderer instance as a new component and referencing it from the
                // the parameter's attribute frame. Then on successive diffs, we can reuse the
                // FragmentRenderer and hence preserve descendant components. Also this means we would
                // no longer be doing the RenderFragment rendering during parameter serialization: we'd
                // be doing it during diffing, so it would be integrated into normal the rendering flow.
                // The parameter serialization would then only need to know that for this special parameter
                // type, it can find the associated FragmentRenderer that already exists, and serialize
                // its render frames that already exist. And I think that means we don't have to deal
                // with asynchrony during the parameter serialization at all.
                var rootComponentId = _htmlRenderer.AssignRootComponentId(rootComponent);
                var renderTask = _htmlRenderer.RenderRootComponentAsync(rootComponentId);

                var frames = _htmlRenderer.GetCurrentRenderTreeFrames(rootComponentId);
                initialHtml = _htmlRenderer.RenderTreeToHtmlString(frames, 0, frames.Count);
            });

            // TODO: Include renderTask in the set of tasks we'd await if the top-level call
            // asked us to await quiescence. And if we're not awaiting quiescence, at least
            // merge it into the overall top-level returned task for error handling. The following
            // logic is just a cheap stand-in.
            if (renderTask.IsFaulted)
            {
                throw renderTask.Exception;
            }

            if (!renderTask.IsCompleted)
            {
                // Fail fast rather than the infinite loop mentioned above.
                throw new InvalidOperationException("The current implementation of RenderFragment serialization doesn't support child content that performs async work.");
            }

            return initialHtml;
        }

        class FragmentRenderer : IComponent
        {
            RenderHandle _renderHandle;

            public RenderFragment Fragment { get; set; }

            public void Attach(RenderHandle renderHandle)
            {
                _renderHandle = renderHandle;
            }

            public Task SetParametersAsync(ParameterView parameters)
            {
                _renderHandle.Render(Fragment);
                return Task.CompletedTask;
            }
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

