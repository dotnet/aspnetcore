// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Renders a form element that cascades an <see cref="EditContext"/> to descendants.
/// </summary>
public class EditForm : ComponentBase
{
    private readonly Func<Task> _handleSubmitDelegate; // Cache to avoid per-render allocations

    private EditContext? _editContext;
    private bool _hasSetEditContextExplicitly;

    /// <summary>
    /// Constructs an instance of <see cref="EditForm"/>.
    /// </summary>
    public EditForm()
    {
        _handleSubmitDelegate = HandleSubmitAsync;
    }

    /// <summary>
    /// Gets or sets a collection of additional attributes that will be applied to the created <c>form</c> element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Supplies the edit context explicitly. If using this parameter, do not
    /// also supply <see cref="Model"/>, since the model value will be taken
    /// from the <see cref="EditContext.Model"/> property.
    /// </summary>
    [Parameter]
    public EditContext? EditContext
    {
        get => _editContext;
        set
        {
            _editContext = value;
            _hasSetEditContextExplicitly = value != null;
        }
    }

    /// <summary>
    /// If enabled, form submission is performed without fully reloading the page. This is
    /// equivalent to adding <code>data-enhance</code> to the form.
    ///
    /// This flag is only relevant in server-side rendering (SSR) scenarios. For interactive
    /// rendering, the flag has no effect since there is no full-page reload on submit anyway.
    /// </summary>
    [Parameter] public bool Enhance { get; set; }

    /// <summary>
    /// Specifies the top-level model object for the form. An edit context will
    /// be constructed for this model. If using this parameter, do not also supply
    /// a value for <see cref="EditContext"/>.
    /// </summary>
    [Parameter] public object? Model { get; set; }

    /// <summary>
    /// Specifies the content to be rendered inside this <see cref="EditForm"/>.
    /// </summary>
    [Parameter] public RenderFragment<EditContext>? ChildContent { get; set; }

    /// <summary>
    /// A callback that will be invoked when the form is submitted.
    ///
    /// If using this parameter, you are responsible for triggering any validation
    /// manually, e.g., by calling <see cref="EditContext.Validate"/>.
    /// </summary>
    [Parameter] public EventCallback<EditContext> OnSubmit { get; set; }

    /// <summary>
    /// A callback that will be invoked when the form is submitted and the
    /// <see cref="EditContext"/> is determined to be valid.
    /// </summary>
    [Parameter] public EventCallback<EditContext> OnValidSubmit { get; set; }

    /// <summary>
    /// A callback that will be invoked when the form is submitted and the
    /// <see cref="EditContext"/> is determined to be invalid.
    /// </summary>
    [Parameter] public EventCallback<EditContext> OnInvalidSubmit { get; set; }

    [CascadingParameter] private FormMappingContext? MappingContext { get; set; }

    /// <summary>
    /// Gets or sets the form handler name. This is required for posting it to a server-side endpoint.
    /// It is not used during interactive rendering.
    /// </summary>
    [Parameter] public string? FormName { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (_hasSetEditContextExplicitly && Model != null)
        {
            throw new InvalidOperationException($"{nameof(EditForm)} requires a {nameof(Model)} " +
                $"parameter, or an {nameof(EditContext)} parameter, but not both.");
        }
        else if (!_hasSetEditContextExplicitly && Model == null)
        {
            throw new InvalidOperationException($"{nameof(EditForm)} requires either a {nameof(Model)} " +
                $"parameter, or an {nameof(EditContext)} parameter, please provide one of these.");
        }

        // If you're using OnSubmit, it becomes your responsibility to trigger validation manually
        // (e.g., so you can display a "pending" state in the UI). In that case you don't want the
        // system to trigger a second validation implicitly, so don't combine it with the simplified
        // OnValidSubmit/OnInvalidSubmit handlers.
        if (OnSubmit.HasDelegate && (OnValidSubmit.HasDelegate || OnInvalidSubmit.HasDelegate))
        {
            throw new InvalidOperationException($"When supplying an {nameof(OnSubmit)} parameter to " +
                $"{nameof(EditForm)}, do not also supply {nameof(OnValidSubmit)} or {nameof(OnInvalidSubmit)}.");
        }

        // Update _editContext if we don't have one yet, or if they are supplying a
        // potentially new EditContext, or if they are supplying a different Model
        if (Model != null && Model != _editContext?.Model)
        {
            _editContext = new EditContext(Model!);
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        Debug.Assert(_editContext != null);

        // If _editContext changes, tear down and recreate all descendants.
        // This is so we can safely use the IsFixed optimization on CascadingValue,
        // optimizing for the common case where _editContext never changes.
        builder.OpenRegion(_editContext.GetHashCode());

        builder.OpenElement(0, "form");

        if (MappingContext != null)
        {
            builder.AddAttribute(2, "method", "post");
        }

        if (Enhance)
        {
            builder.AddAttribute(3, "data-enhance", "");
        }

        builder.AddMultipleAttributes(4, AdditionalAttributes);
        builder.AddAttribute(5, "onsubmit", _handleSubmitDelegate);

        // In SSR cases, we register onsubmit as a named event and emit other child elements
        // to include the handler and antiforgery token in the post data
        if (MappingContext != null)
        {
            if (!string.IsNullOrEmpty(FormName))
            {
                builder.AddNamedEvent("onsubmit", FormName);
            }

            RenderSSRFormHandlingChildren(builder, 6);
        }

        builder.OpenComponent<CascadingValue<EditContext>>(7);
        builder.AddComponentParameter(7, "IsFixed", true);
        builder.AddComponentParameter(8, "Value", _editContext);
        builder.AddComponentParameter(9, "ChildContent", ChildContent?.Invoke(_editContext));
        builder.CloseComponent();

        builder.CloseElement();

        builder.CloseRegion();
    }

    private void RenderSSRFormHandlingChildren(RenderTreeBuilder builder, int sequence)
    {
        builder.OpenRegion(sequence);

        builder.OpenComponent<FormMappingValidator>(1);
        builder.AddComponentParameter(2, nameof(FormMappingValidator.CurrentEditContext), EditContext);
        builder.CloseComponent();

        builder.OpenComponent<AntiforgeryToken>(3);
        builder.CloseComponent();

        builder.CloseRegion();
    }

    private async Task HandleSubmitAsync()
    {
        Debug.Assert(_editContext != null);

        if (OnSubmit.HasDelegate)
        {
            // When using OnSubmit, the developer takes control of the validation lifecycle
            await OnSubmit.InvokeAsync(_editContext);
        }
        else
        {
            // Otherwise, the system implicitly runs validation on form submission
            var isValid = _editContext.Validate(); // This will likely become ValidateAsync later

            if (isValid && OnValidSubmit.HasDelegate)
            {
                await OnValidSubmit.InvokeAsync(_editContext);
            }

            if (!isValid && OnInvalidSubmit.HasDelegate)
            {
                await OnInvalidSubmit.InvokeAsync(_editContext);
            }
        }
    }
}
