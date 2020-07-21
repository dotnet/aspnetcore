// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    /// <summary>
    /// Provides common functionality for virtualized lists.
    /// </summary>
    /// <typeparam name="TItem">The <c>context</c> type for the items being rendered.</typeparam>
    public abstract class VirtualizeBase<TItem> : ComponentBase, IAsyncDisposable
    {
        private int _itemsAbove;

        private int _itemsVisible;

        private ElementReference _topSpacer;

        private ElementReference _bottomSpacer;

        private IVirtualizationHelper? _virtualizationHelper;

        [Inject]
        private IVirtualizationService VirtualizationService { get; set; } = default!;

        /// <summary>
        /// Gets the total number of items in the collection.
        /// </summary>
        protected abstract int ItemCount { get; }

        /// <summary>
        /// Gets the size (height) of each item in pixels.
        /// </summary>
        [Parameter]
        public float ItemSize { get; set; }

        /// <summary>
        /// Gets the <see cref="RenderFragment"/>s representing a range of items.
        /// </summary>
        /// <param name="start">The start index of the range of items.</param>
        /// <param name="count">The number of items in the range.</param>
        /// <returns>The <see cref="RenderFragment"/>s representing the given range.</returns>
        protected abstract IEnumerable<RenderFragment> GetItems(int start, int count);

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            if (ItemSize <= 0f)
            {
                throw new InvalidOperationException($"Parameter '{nameof(ItemSize)}' must be specified and greater than zero.");
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _virtualizationHelper = VirtualizationService.CreateVirtualizationHelper();
                _virtualizationHelper.TopSpacerVisible += OnTopSpacerVisible;
                _virtualizationHelper.BottomSpacerVisible += OnBottomSpacerVisible;

                await _virtualizationHelper.InitAsync(_topSpacer, _bottomSpacer);
            }
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var itemsBelow = Math.Max(0, ItemCount - _itemsVisible - _itemsAbove);

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "key", "top-spacer");
            builder.AddAttribute(2, "style", GetSpacerStyle(_itemsAbove));
            builder.AddElementReferenceCapture(3, elementReference => _topSpacer = elementReference);
            builder.CloseElement();

            builder.OpenRegion(4);

            foreach (var item in GetItems(_itemsAbove, _itemsVisible))
            {
                item.Invoke(builder);
            }

            builder.CloseRegion();

            builder.OpenElement(5, "div");
            builder.AddAttribute(6, "key", "bottom-spacer");
            builder.AddAttribute(7, "style", GetSpacerStyle(itemsBelow));
            builder.AddElementReferenceCapture(8, elementReference => _bottomSpacer = elementReference);
            builder.CloseElement();
        }

        private void OnTopSpacerVisible(object? sender, SpacerEventArgs e)
        {
            CalcualteItemDistribution(e, out _itemsVisible, out _itemsAbove);

            StateHasChanged();
        }

        private void OnBottomSpacerVisible(object? sender, SpacerEventArgs e)
        {
            CalcualteItemDistribution(e, out _itemsVisible, out var itemsBelow);

            _itemsAbove = Math.Max(0, ItemCount - itemsBelow - _itemsVisible);

            StateHasChanged();
        }

        private void CalcualteItemDistribution(SpacerEventArgs e, out int itemsVisible, out int itemsInSpacer)
        {
            itemsVisible = Math.Max(0, (int)Math.Ceiling(e.ContainerSize / ItemSize) + 2);
            itemsInSpacer = Math.Max(0, (int)Math.Floor(e.SpacerSize / ItemSize) - 1);
        }

        private string GetSpacerStyle(int itemsInSpacer)
        {
            return $"height: {itemsInSpacer * ItemSize}px;";
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_virtualizationHelper != null)
            {
                await _virtualizationHelper.DisposeAsync();
            }
        }
    }
}
