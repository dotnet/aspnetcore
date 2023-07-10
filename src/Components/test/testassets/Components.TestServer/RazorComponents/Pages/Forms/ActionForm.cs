// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

public class ActionForm : ComponentBase
{
    [Parameter] public RenderFragment<FormMappingContext>? ChildContent { get; set; }

    [EditorRequired][Parameter] public EventCallback<FormMappingContext> OnSubmit { get; set; }

    [CascadingParameter] public FormMappingContext BindingContext { get; set; }

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
            builder.OpenComponent<FormMappingScope>(0);
            builder.AddComponentParameter(1, nameof(FormMappingScope.Name), FormHandlerName);
            builder.AddComponentParameter(2, nameof(FormMappingScope.ChildContent), (RenderFragment<FormMappingContext>)RenderWithNamedContext);
            builder.CloseComponent();
        }
        else
        {
            RenderFormContents(builder, BindingContext);
        }

        RenderFragment RenderWithNamedContext(FormMappingContext context)
        {
            return builder => RenderFormContents(builder, context);
        }

        void RenderFormContents(RenderTreeBuilder builder, FormMappingContext? bindingContext)
        {
            builder.OpenElement(0, "form");
            builder.AddAttribute(1, "name", bindingContext.Name);

            if (!string.IsNullOrEmpty(bindingContext?.MappingContextId))
            {
                builder.AddAttribute(2, "action", bindingContext.MappingContextId);
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
