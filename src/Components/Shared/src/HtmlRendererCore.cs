// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.HtmlRendering;

internal class HtmlRendererCore : Renderer, IHtmlRendererContentProvider
{
    private static readonly Task CanceledRenderTask = Task.FromCanceled(new CancellationToken(canceled: true));

    public HtmlRendererCore(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        : base(serviceProvider, loggerFactory)
    {
    }

    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    public HtmlComponent BeginRenderingComponent(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
        ParameterView initialParameters)
    {
        var component = InstantiateComponent(componentType);
        var componentId = AssignRootComponentId(component);
        var quiescenceTask = RenderRootComponentAsync(componentId, initialParameters);

        if (quiescenceTask.IsFaulted)
        {
            ExceptionDispatchInfo.Capture(quiescenceTask.Exception.InnerException ?? quiescenceTask.Exception).Throw();
        }

        return new HtmlComponent(this, componentId, quiescenceTask);
    }

    protected override void HandleException(Exception exception)
        => ExceptionDispatchInfo.Capture(exception).Throw();

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

    public new ArrayRange<RenderTreeFrame> GetCurrentRenderTreeFrames(int componentId)
        => base.GetCurrentRenderTreeFrames(componentId);
}
