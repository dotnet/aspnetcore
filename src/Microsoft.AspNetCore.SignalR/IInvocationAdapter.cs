using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IInvocationAdapter
    {
        Task<InvocationDescriptor> ReadInvocationDescriptorAsync(Stream stream, Func<string, Type[]> getParams);

        Task WriteInvocationResultAsync(InvocationResultDescriptor resultDescriptor, Stream stream);

        Task WriteInvocationDescriptorAsync(InvocationDescriptor invocationDescriptor, Stream stream);
    }
}
