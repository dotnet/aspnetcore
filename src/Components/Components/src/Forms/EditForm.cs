// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.RenderTree;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Renders a form element that cascades an <see cref="EditContext"/> to descendants.
    /// </summary>
    public class EditForm : ComponentBase
    {
        private readonly Func<Task> _handleSubmitDelegate; // Cache to avoid per-render allocations

        private EditContext _fixedEditContext;

        /// <summary>
        /// Constructs an instance of <see cref="EditForm"/>.
        /// </summary>
        public EditForm()
        {
            _handleSubmitDelegate = HandleSubmitAsync;
        }

        /// <summary>
        /// Supplies the edit context explicitly. If using this parameter, do not
        /// also supply <see cref="Model"/>, since the model value will be taken
        /// from the <see cref="EditContext.Model"/> property.
        /// </summary>
        [Parameter] EditContext EditContext { get; set; }

        /// <summary>
        /// Specifies the top-level model object for the form. An edit context will
        /// be constructed for this model. If using this parameter, do not also supply
        /// a value for <see cref="EditContext"/>.
        /// </summary>
        [Parameter] object Model { get; set; }

        /// <summary>
        /// Specifies the content to be rendered inside this <see cref="EditForm"/>.
        /// </summary>
        [Parameter] RenderFragment<EditContext> ChildContent { get; set; }

        /// <summary>
        /// A callback that will be invoked when the form is submitted.
        ///
        /// If using this parameter, you are responsible for triggering any validation
        /// manually, e.g., by calling <see cref="EditContext.Validate"/>.
        /// </summary>
        [Parameter] Func<EditContext, Task> OnSubmit { get; set; }

        /// <summary>
        /// A callback that will be invoked when the form is submitted and the
        /// <see cref="EditContext"/> is determined to be valid.
        /// </summary>
        [Parameter] Func<EditContext, Task> OnValidSubmit { get; set; }

        /// <summary>
        /// A callback that will be invoked when the form is submitted and the
        /// <see cref="EditContext"/> is determined to be invalid.
        /// </summary>
        [Parameter] Func<EditContext, Task> OnInvalidSubmit { get; set; }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            if ((EditContext == null) == (Model == null))
            {
                throw new InvalidOperationException($"{nameof(EditForm)} requires a {nameof(Model)} " +
                    $"parameter, or an {nameof(EditContext)} parameter, but not both.");
            }

            // If you're using OnSubmit, it becomes your responsibility to trigger validation manually
            // (e.g., so you can display a "pending" state in the UI). In that case you don't want the
            // system to trigger a second validation implicitly, so don't combine it with the simplified
            // OnValidSubmit/OnInvalidSubmit handlers.
            if (OnSubmit != null && (OnValidSubmit != null || OnInvalidSubmit != null))
            {
                throw new InvalidOperationException($"When supplying an {nameof(OnSubmit)} parameter to " +
                    $"{nameof(EditForm)}, do not also supply {nameof(OnValidSubmit)} or {nameof(OnInvalidSubmit)}.");
            }

            // Update _fixedEditContext if we don't have one yet, or if they are supplying a
            // potentially new EditContext, or if they are supplying a different Model
            if (_fixedEditContext == null || EditContext != null || Model != _fixedEditContext.Model)
            {
                _fixedEditContext = EditContext ?? new EditContext(Model);
            }
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            base.BuildRenderTree(builder);

            // If _fixedEditContext changes, tear down and recreate all descendants.
            // This is so we can safely use the IsFixed optimization on CascadingValue,
            // optimizing for the common case where _fixedEditContext never changes.
            builder.OpenRegion(_fixedEditContext.GetHashCode());

            builder.OpenElement(0, "form");
            builder.AddAttribute(1, "onsubmit", _handleSubmitDelegate);
            builder.OpenComponent<CascadingValue<EditContext>>(2);
            builder.AddAttribute(3, "IsFixed", true);
            builder.AddAttribute(4, "Value", _fixedEditContext);

            // TODO: Once the event routing bug is fixed, replace the following with
            // builder.AddAttribute(5, RenderTreeBuilder.ChildContent, ChildContent?.Invoke(_fixedEditContext));
            builder.AddAttribute(5, RenderTreeBuilder.ChildContent, (RenderFragment)RenderChildContentInWorkaround);

            builder.CloseComponent();
            builder.CloseElement();

            builder.CloseRegion();
        }

        private void RenderChildContentInWorkaround(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TemporaryEventRoutingWorkaround>(0);
            builder.AddAttribute(1, RenderTreeBuilder.ChildContent, ChildContent?.Invoke(_fixedEditContext));
            builder.CloseComponent();
        }

        private async Task HandleSubmitAsync()
        {
            if (OnSubmit != null)
            {
                // When using OnSubmit, the developer takes control of the validation lifecycle
                await OnSubmit(_fixedEditContext);
            }
            else
            {
                // Otherwise, the system implicitly runs validation on form submission
                var isValid = _fixedEditContext.Validate(); // This will likely become ValidateAsync later

                var task = isValid
                    ? OnValidSubmit?.Invoke(_fixedEditContext)
                    : OnInvalidSubmit?.Invoke(_fixedEditContext);

                if (isValid && OnValidSubmit != null)
                {
                    await OnValidSubmit(_fixedEditContext);
                }

                if (!isValid && OnInvalidSubmit != null)
                {
                    await OnInvalidSubmit(_fixedEditContext);
                }
            }
        }
    }
}
