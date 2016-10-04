using System;
using System.IO;
using System.Threading.Tasks;

namespace SocketsSample
{
    interface InvocationDescriptorBuilder
    {
        Task<InvocationDescriptor> CreateInvocationDescriptor(Stream stream, Func<string, Type[]> getParams);
    }
}
