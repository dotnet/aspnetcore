// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Greet;
using Grpc.Net.Client;

namespace BasicClient;

public class Program
{
    public static async Task Main(string[] args)
    {
        var address = args[0];

        Console.WriteLine($"Address: {address}");
        Console.WriteLine("Application started.");

        var channel = GrpcChannel.ForAddress(address);
        var client = new Greeter.GreeterClient(channel);

        var reply = await client.SayHelloAsync(new HelloRequest { Name = "world " });

        Console.WriteLine($"From server: {reply}");
    }
}
