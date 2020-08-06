using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal class RemoteFileListEntryStream : FileListEntryStream
    {
        public RemoteFileListEntryStream(IJSRuntime jsRuntime, ElementReference inputFileElement, int maxMessageSize, int maxBufferSize, FileListEntry file)
            : base(file)
        {
            // TODO
        }

        protected override Task<int> CopyFileDataIntoBuffer(long sourceOffset, byte[] destination, int destinationOffset, int maxBytes, CancellationToken cancellationToken)
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
