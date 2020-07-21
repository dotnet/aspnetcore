using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    public abstract class VirtualizeBase<TItem> : ComponentBase
    {
        private readonly ConcurrentQueue<TItem> _loadedItems = new ConcurrentQueue<TItem>();

        private readonly SemaphoreSlim _fetchSemaphore = new SemaphoreSlim(1);

        private int _itemsAbove;

        private int _itemsVisible;

        private int _itemsBelow;

        private IEnumerable<TItem> LoadedItems => Items ?? (IEnumerable<TItem>)_loadedItems;

        private int ItemCount => Items?.Count ?? _loadedItems.Count;

        protected ElementReference TopSpacer { get; private set; }

        protected ElementReference BottomSpacer { get; private set; }

        [Parameter]
        public ICollection<TItem> Items { get; set; } = default!;

        [Parameter]
        public Func<Range, Task<IEnumerable<TItem>>> ItemsProvider { get; set; } = default!;

        [Parameter]
        public float ItemSize { get; set; }

        [Parameter]
        public int InitialItemsCount { get; set; }

        [Parameter]
        public RenderFragment<TItem>? ChildContent { get; set; }

        protected override void OnParametersSet()
        {
            if (ItemSize <= 0f)
            {
                throw new InvalidOperationException(
                    $"Parameter '{nameof(ItemSize)}' must be specified and greater than zero.");
            }

            if (Items != null)
            {
                if (ItemsProvider != null)
                {
                    throw new InvalidOperationException(
                        $"{GetType()} cannot have both '{nameof(Items)}' and '{nameof(ItemsProvider)}' parameters.");
                }

                _itemsBelow = Items.Count;
            }
            else if (ItemsProvider != null)
            {
                _itemsBelow = 0;
            }
            else
            {
                throw new InvalidOperationException(
                    $"{GetType()} requires either the '{nameof(Items)}' or '{nameof(ItemsProvider)}' parameter to " +
                    $"be specified and non-null.");
            }
        }

        protected void UpdateTopSpacer(float spacerSize, float containerSize)
        {
            CalculateSpacerItemDistribution(spacerSize, containerSize, out _itemsAbove, out _itemsBelow);
            Console.WriteLine($"Above: {_itemsAbove}, Visible: {_itemsVisible}");
        }

        protected void UpdateBottomSpacer(float spacerSize, float containerSize)
        {
            CalculateSpacerItemDistribution(spacerSize, containerSize, out _itemsBelow, out _itemsAbove);
            Console.WriteLine($"Above: {_itemsAbove}, Visible: {_itemsVisible}");

            if (ItemsProvider != null && _itemsAbove + _itemsVisible >= _loadedItems.Count)
            {
                FetchItems(_itemsAbove + _itemsVisible + InitialItemsCount);
            }
        }

        private void CalculateSpacerItemDistribution(float spacerSize, float containerSize, out int itemsInThisSpacer, out int itemsInOtherSpacer)
        {
            _itemsVisible = Math.Max(0, (int)Math.Ceiling(containerSize / ItemSize) + 2);
            itemsInThisSpacer = Math.Max(0, (int)Math.Floor(spacerSize / ItemSize) - 1);
            itemsInOtherSpacer = Math.Max(0, ItemCount - itemsInThisSpacer - _itemsVisible);

            StateHasChanged();
        }

        private string GetSpacerStyle(int itemsInSpacer)
        {
            return $"height: {itemsInSpacer * ItemSize}px;";
        }

        private void FetchItems(int newItemCount)
        {
            var currentScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            _fetchSemaphore.WaitAsync().ContinueWith(t =>
            {
                if (_loadedItems.Count >= newItemCount)
                {
                    _fetchSemaphore.Release();
                    return;
                }

                ItemsProvider(_loadedItems.Count..newItemCount).ContinueWith(t =>
                {
                    foreach (var item in t.Result)
                    {
                        _loadedItems.Enqueue(item);
                    }

                    StateHasChanged();

                    _fetchSemaphore.Release();
                }, currentScheduler);
            });
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            _itemsBelow = Math.Max(1, ItemCount - (_itemsVisible + _itemsAbove));

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "key", "top-spacer");
            builder.AddAttribute(2, "style", GetSpacerStyle(_itemsAbove));
            builder.AddElementReferenceCapture(3, elementReference => TopSpacer = elementReference);
            builder.CloseElement();

            builder.OpenRegion(4);

            if (ChildContent != null)
            {
                foreach (var item in LoadedItems.Skip(_itemsAbove).Take(_itemsVisible))
                {
                    ChildContent(item)?.Invoke(builder);
                }
            }

            builder.CloseRegion();

            builder.OpenElement(5, "div");
            builder.AddAttribute(6, "key", "bottom-spacer");
            builder.AddAttribute(7, "style", GetSpacerStyle(_itemsBelow));
            builder.AddElementReferenceCapture(8, elementReference => BottomSpacer = elementReference);
            builder.CloseElement();
        }
    }
}
