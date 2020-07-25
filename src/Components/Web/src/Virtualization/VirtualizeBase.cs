// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    /// <summary>
    /// Provides common functionality for virtualized lists.
    /// </summary>
    /// <typeparam name="TItem">The <c>context</c> type for the items being rendered.</typeparam>
    public abstract class VirtualizeBase<TItem> : ComponentBase, IAsyncDisposable
    {
        private const string JsFunctionsPrefix = "Blazor._internal.Virtualize";

        private DotNetObjectReference<VirtualizeBase<TItem>>? _selfReference;

        private ElementReference _spacerBefore;

        private ElementReference _spacerAfter;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        /// <summary>
        /// Gets the number of items rendered before the visible part of the container.
        /// </summary>
        protected int ItemsBefore { get; private set; }

        /// <summary>
        /// Gets the number of items the container can visibly fit.
        /// </summary>
        protected int VisibleItemCapacity { get; private set; }

        /// <summary>
        /// Gets the number of items currently visible within the container.
        /// </summary>
        protected int ItemsVisible { get; private set; }

        /// <summary>
        /// Gets the total number of items.
        /// </summary>
        protected abstract int ItemCount { get; }

        /// <summary>
        /// Gets the size of each item in pixels.
        /// </summary>
        [Parameter]
        public float ItemSize { get; set; }

        /// <summary>
        /// Renders the visible items in the list.
        /// </summary>
        /// <param name="builder">A <see cref="RenderTreeBuilder"/> to receive the render output.</param>
        protected abstract void RenderItems(RenderTreeBuilder builder);

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);

            if (ItemSize <= 0)
            {
                throw new InvalidOperationException(
                    $"{GetType()} requires a positive value for parameter '{nameof(ItemSize)}' to perform virtualization.");
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _selfReference = DotNetObjectReference.Create<VirtualizeBase<TItem>>(this);
                await JSRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.init", _selfReference, _spacerBefore, _spacerAfter);
            }
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var itemsAfter = Math.Max(0, ItemCount - ItemsVisible - ItemsBefore);

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", GetSpacerStyle(ItemsBefore));
            builder.AddElementReferenceCapture(2, elementReference => _spacerBefore = elementReference);
            builder.CloseElement();

            builder.OpenRegion(3);

            RenderItems(builder);

            builder.CloseRegion();

            builder.OpenElement(4, "div");
            builder.AddAttribute(5, "style", GetSpacerStyle(itemsAfter));
            builder.AddElementReferenceCapture(6, elementReference => _spacerAfter = elementReference);

            builder.CloseElement();
        }

        /// <summary>
        /// Called when the top spacer becomes visible.
        /// This method is intended to be invoked only from JavaScript.
        /// </summary>
        /// <param name="spacerSize">The new top spacer size.</param>
        /// <param name="containerSize">The top spacer's container size.</param>
        [JSInvokable]
        public void OnTopSpacerVisible(float spacerSize, float containerSize)
        {
            ItemsBefore = CalcualteItemDistribution(spacerSize, containerSize);

            StateHasChanged();
        }

        /// <summary>
        /// Called when the bottom spacer becomes visible.
        /// This method is intended to be invoked only from JavaScript.
        /// </summary>
        /// <param name="spacerSize">The new bottom spacer size.</param>
        /// <param name="containerSize">The bottom spacer's container size.</param>
        [JSInvokable]
        public void OnBottomSpacerVisible(float spacerSize, float containerSize)
        {
            var itemsAfter = CalcualteItemDistribution(spacerSize, containerSize);

            ItemsBefore = Math.Max(0, ItemCount - itemsAfter - ItemsVisible);

            StateHasChanged();
        }

        private int CalcualteItemDistribution(float spacerSize, float containerSize)
        {
            VisibleItemCapacity = (int)Math.Ceiling(containerSize / ItemSize) + 2;
            ItemsVisible = Math.Clamp(ItemCount, 0, VisibleItemCapacity);

            return Math.Max(0, (int)Math.Floor(spacerSize / ItemSize) - 1);
        }

        private string GetSpacerStyle(int itemsInSpacer)
            => $"height: {itemsInSpacer * ItemSize}px;";

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_selfReference != null)
            {
                await JSRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.dispose", _selfReference);
            }
        }
    }
}
