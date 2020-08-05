using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal class InputFileJsCallbacksRelay : IDisposable
    {
        private readonly IInputFileJsCallbacks _callbacks;

        private readonly IDisposable _selfReference;

        public InputFileJsCallbacksRelay(IInputFileJsCallbacks callbacks)
        {
            _callbacks = callbacks;
            _selfReference = DotNetObjectReference.Create(this);
        }

        [JSInvokable]
        public Task NotifyChange(FileListEntry[] files)
            => _callbacks.NotifyChange(files);

        public void Dispose()
        {
            _selfReference.Dispose();
        }
    }
}
