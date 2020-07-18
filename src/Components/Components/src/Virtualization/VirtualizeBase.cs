using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    public abstract class VirtualizeBase<TItem> : ComponentBase
    {
        private int _itemsAbove;

        private int _itemsVisible;

        private int _itemsBelow;

        protected ElementReference TopSpacer { get; private set; }

        protected ElementReference BottomSpacer { get; private set; }

        [Parameter]
        public ICollection<TItem> Items { get; set; } = default!;

        [Parameter]
        public float ItemSize { get; set; }

        [Parameter]
        public RenderFragment<TItem>? ChildContent { get; set; }

        protected override void OnParametersSet()
        {
            if (Items == null)
            {
                throw new InvalidOperationException(
                    $"Parameter '{nameof(Items)}' must be specified and non-null.");
            }

            if (ItemSize <= 0f)
            {
                throw new InvalidOperationException(
                    $"Parameter '{nameof(ItemSize)}' must be specified and greater than zero.");
            }

            _itemsBelow = Items.Count;
        }

        protected void UpdateTopSpacer(float spacerSize, float containerSize)
            => CalculateSpacerItemDistribution(spacerSize, containerSize, out _itemsAbove, out _itemsBelow);

        protected void UpdateBottomSpacer(float spacerSize, float containerSize)
            => CalculateSpacerItemDistribution(spacerSize, containerSize, out _itemsBelow, out _itemsAbove);

        private void CalculateSpacerItemDistribution(float spacerSize, float containerSize, out int itemsInThisSpacer, out int itemsInOtherSpacer)
        {
            _itemsVisible = (int)(containerSize / ItemSize) + 1; // TODO: Custom number of "padding" elements?
            itemsInThisSpacer = (int)(spacerSize / ItemSize);
            itemsInOtherSpacer = Items.Count - itemsInThisSpacer - _itemsVisible;

            StateHasChanged();
        }

        private string GetSpacerStyle(int itemsInSpacer)
        {
            return $"height: {itemsInSpacer * ItemSize}px;";
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "key", "top-spacer");
            builder.AddAttribute(2, "style", GetSpacerStyle(_itemsAbove));
            builder.AddElementReferenceCapture(3, elementReference => TopSpacer = elementReference);
            builder.CloseElement();

            if (ChildContent != null)
            {
                builder.AddContent(4, new RenderFragment(builder =>
                {
                    foreach (var item in Items.Skip(_itemsAbove).Take(_itemsVisible))
                    {
                        ChildContent(item)?.Invoke(builder);
                    }
                }));
            }

            builder.OpenElement(5, "div");
            builder.AddAttribute(6, "key", "bottom-spacer");
            builder.AddAttribute(7, "style", GetSpacerStyle(_itemsBelow));
            builder.AddElementReferenceCapture(8, elementReference =>
                BottomSpacer = elementReference);
            builder.CloseElement();
        }
    }
}
