using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Greet;
using Grpc.Core;

namespace Company.ClientApplication
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var channel = new Channel("localhost:50051", ChannelCredentials.Insecure);
            var client = new Greeter.GreeterClient(channel);

            var reply = await client.SayHelloAsync(new HelloRequest { Name = "GreeterClient" });
            Console.WriteLine("Greeting: " + reply.Message);

            await channel.ShutdownAsync();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
