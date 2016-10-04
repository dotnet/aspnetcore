
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Google.Protobuf;

namespace SocketsSample.Protobuf
{
    public class ProtobufInvocationDescriptorBuilder : InvocationDescriptorBuilder
    {
        public Task<InvocationDescriptor> CreateInvocationDescriptor(Stream stream, Func<string, Type[]> getParams)
        {
            var invocationDescriptor = new InvocationDescriptor();
            var inputStream = new CodedInputStream(stream, leaveOpen: true);
            var invocationHeader = new RpcInvocationHeader();
            inputStream.ReadMessage(invocationHeader);
            var argumentTypes = getParams(invocationHeader.Name);

            invocationDescriptor.Method = invocationHeader.Name;
            invocationDescriptor.Id = invocationHeader.Id.ToString();
            invocationDescriptor.Arguments = new object[argumentTypes.Length];

            var primitiveValueParser = PrimitiveValue.Parser;
            for (var i = 0; i < argumentTypes.Length; i++)
            {
                if (argumentTypes[i] == typeof(int))
                {
                    invocationDescriptor.Arguments[i] = primitiveValueParser.ParseFrom(inputStream).Int32Value;
                }
                else if (argumentTypes[i] == typeof(int))
                {
                    invocationDescriptor.Arguments[i] = primitiveValueParser.ParseFrom(inputStream).StringValue;
                }
                else if (typeof(IMessage).IsAssignableFrom(argumentTypes[i]))
                {
                    throw new NotImplementedException();
                }
            }

            return Task.FromResult(invocationDescriptor);
        }

        public async Task WriteResult(Stream stream, InvocationResultDescriptor result)
        {
            throw new NotImplementedException();
        }
    }
}
