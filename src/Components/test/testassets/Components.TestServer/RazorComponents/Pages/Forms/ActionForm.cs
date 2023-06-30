// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

public class ActionForm : ComponentBase
{
    [Parameter] public RenderFragment<ModelBindingContext>? ChildContent { get; set; }

    [EditorRequired][Parameter] public EventCallback<ModelBindingContext> OnSubmit { get; set; }

    [CascadingParameter] public ModelBindingContext BindingContext { get; set; }

    [Parameter] public string? FormHandlerName { get; set; }

    protected override void OnParametersSet()
    {
        if (!OnSubmit.HasDelegate)
        {
            throw new InvalidOperationException($"{nameof(ActionForm)} requires a {nameof(OnSubmit)} parameter.");
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (FormHandlerName != null)
        {
            builder.OpenComponent<CascadingModelBinder>(0);
            builder.AddComponentParameter(1, nameof(CascadingModelBinder.Name), FormHandlerName);
            builder.AddComponentParameter(2, nameof(CascadingModelBinder.ChildContent), (RenderFragment<ModelBindingContext>)RenderWithNamedContext);
            builder.CloseComponent();
        }
        else
        {
            RenderFormContents(builder, BindingContext);
        }

        RenderFragment RenderWithNamedContext(ModelBindingContext context)
        {
            return builder => RenderFormContents(builder, context);
        }

        void RenderFormContents(RenderTreeBuilder builder, ModelBindingContext? bindingContext)
        {
            builder.OpenElement(0, "form");
            builder.AddAttribute(1, "name", bindingContext.Name);

            if (!string.IsNullOrEmpty(bindingContext?.BindingContextId))
            {
                builder.AddAttribute(2, "action", bindingContext.BindingContextId);
            }

            builder.AddAttribute(3, "method", "POST");
            builder.AddAttribute(4, "onsubmit", async () => await OnSubmit.InvokeAsync(bindingContext));

            if (bindingContext != null)
            {
                builder.SetEventHandlerName(bindingContext.Name);
            }
            builder.AddContent(5, ChildContent?.Invoke(bindingContext));

            builder.CloseElement();
        }
    }
}
