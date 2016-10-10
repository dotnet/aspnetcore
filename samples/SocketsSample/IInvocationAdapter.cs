using System;
using System.IO;
using System.Threading.Tasks;

namespace SocketsSample
{
    public interface IInvocationAdapter
    {
        Task<InvocationDescriptor> CreateInvocationDescriptor(Stream stream, Func<string, Type[]> getParams);

        Task WriteInvocationResult(Stream stream, InvocationResultDescriptor resultDescriptor);

        Task InvokeClientMethod(Stream stream, InvocationDescriptor invocationDescriptor);
    }
}
