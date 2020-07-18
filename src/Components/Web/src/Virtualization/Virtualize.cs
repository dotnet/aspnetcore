using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Virtualization
{
    public class Virtualize<TItem> : VirtualizeBase<TItem>, IDisposable
    {
        private DotNetObjectReference<Virtualize<TItem>>? _selfReference;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _selfReference = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("Blazor._internal.Virtualize.init", _selfReference, TopSpacer, BottomSpacer);
            }
        }

        [JSInvokable]
        public void OnTopSpacerVisible(float spacerSize, float containerSize)
        {
            UpdateTopSpacer(spacerSize, containerSize);
        }

        [JSInvokable]
        public void OnBottomSpacerVisible(float spacerSize, float containerSize)
        {
            UpdateBottomSpacer(spacerSize, containerSize);
        }

        public void Dispose()
        {
            _selfReference?.Dispose();
        }
    }
}
