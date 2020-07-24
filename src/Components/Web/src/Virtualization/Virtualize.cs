// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    /// <summary>
    /// Provides functionality for rendering a virtualized, fixed-size list of items.
    /// </summary>
    /// <typeparam name="TItem">The <c>context</c> type for the items being rendered.</typeparam>
    public sealed class Virtualize<TItem> : VirtualizeBase<TItem>, IAsyncDisposable
    {
        private const string JsFunctionsPrefix = "Blazor._internal.Virtualize";

        private DotNetObjectReference<Virtualize<TItem>>? _selfReference;

        private int _itemsBefore;

        private int _itemsVisible;

        private ElementReference _spacerBefore;

        private ElementReference _spacerAfter;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _selfReference = DotNetObjectReference.Create<Virtualize<TItem>>(this);
                await JSRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.init", _selfReference, _spacerBefore, _spacerAfter);
            }
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var itemsAfter = Math.Max(0, ItemSource.Count() - _itemsVisible - _itemsBefore);

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", GetSpacerStyle(_itemsBefore));
            builder.AddElementReferenceCapture(2, elementReference => _spacerBefore = elementReference);
            builder.CloseElement();

            builder.OpenRegion(3);

            if (ItemTemplate != null)
            {
                var items = IsVirtualized ? ItemSource.Skip(_itemsBefore).Take(_itemsVisible) : ItemSource;

                foreach (var item in items)
                {
                    ItemTemplate(item)(builder);
                }
            }

            builder.CloseRegion();

            builder.OpenElement(4, "div");
            builder.AddAttribute(5, "style", GetSpacerStyle(itemsAfter));
            builder.AddElementReferenceCapture(6, elementReference => _spacerAfter = elementReference);

            builder.CloseElement();

            if (Footer != null)
            {
                builder.OpenRegion(7);

                Footer.Invoke(builder);

                builder.CloseRegion();
            }
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
            CalcualteItemDistribution(spacerSize, containerSize, out _itemsVisible, out _itemsBefore);

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
            CalcualteItemDistribution(spacerSize, containerSize, out _itemsVisible, out var itemsAfter);

            _itemsBefore = Math.Max(0, ItemSource.Count() - itemsAfter - _itemsVisible);

            if (itemsAfter == 0)
            {
                _ = FetchItemsAsync();
            }

            StateHasChanged();
        }

        private void CalcualteItemDistribution(float spacerSize, float containerSize, out int itemsVisible, out int itemsInSpacer)
        {
            if (IsVirtualized)
            {
                itemsVisible = Math.Clamp(ItemSource.Count(), 0, (int)Math.Ceiling(containerSize / ItemSize) + 2);
                itemsInSpacer = Math.Max(0, (int)Math.Floor(spacerSize / ItemSize) - 1);
            }
            else
            {
                itemsVisible = 0;
                itemsInSpacer = 0;
            }
        }

        private string GetSpacerStyle(int itemsInSpacer)
        {
            return $"height: {itemsInSpacer * ItemSize}px;";
        }

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
