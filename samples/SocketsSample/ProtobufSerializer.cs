using System;
using Google.Protobuf;
using SocketsSample.Hubs;

namespace SocketsSample
{
    public class ProtobufSerializer
    {
        public object GetValue(CodedInputStream inputStream, Type type)
        {
            if (type == typeof(Person))
            {
                var value = new PersonMessage();
                inputStream.ReadMessage(value);

                return new Person { Name = value.Name, Age = value.Age };
            }

            throw new InvalidOperationException("(Deserialize) Unknown type.");
        }

        public IMessage GetMessage(object value)
        {
            Person person = value as Person;
            if (person != null)
            {
                return new PersonMessage { Name = person.Name, Age = person.Age };
            }

            throw new InvalidOperationException("(Serialize) Unknown type.");
        }
    }
}
