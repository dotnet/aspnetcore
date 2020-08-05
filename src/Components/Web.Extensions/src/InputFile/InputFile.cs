using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    public class InputFile : ComponentBase, IInputFileJsCallbacks, IDisposable
    {
        private const string JsFunctionsPrefix = "_blazorInputFile";

        private ElementReference _inputFileElement;

        private InputFileJsCallbacksRelay? _jsCallbacksRelay;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        [Parameter]
        public EventCallback<IFileListEntry[]> OnChange { get; set; }

        [Parameter]
        public int MaxMessageSize { get; set; } = 20 * 1024; // SignalR limit is 32K.

        [Parameter]
        public int MaxBufferSize { get; set; } = 1024 * 1024;

        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object>? AdditionalAttributes { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _jsCallbacksRelay = new InputFileJsCallbacksRelay(this);
                await JSRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.init", _jsCallbacksRelay, _inputFileElement);
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "input");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddElementReferenceCapture(2, elementReference => _inputFileElement = elementReference);
            builder.CloseElement();
        }

        internal Stream OpenFileStream(FileListEntry file)
            => RuntimeInformation.IsOSPlatform(OSPlatform.Browser) ?
                new MemoryStream() : // TODO: SharedMemoryFileListEntryStream
                new MemoryStream();  // TODO: RemoteFileListEntryStream

        Task IInputFileJsCallbacks.NotifyChange(FileListEntry[] files)
        {
            foreach (var file in files)
            {
                file.Owner = this;
            }

            return OnChange.InvokeAsync(files);
        }

        void IDisposable.Dispose()
        {
            _jsCallbacksRelay?.Dispose();
        }
    }
}
