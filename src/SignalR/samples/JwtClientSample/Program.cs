// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Net.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace JwtClientSample;

class Program
{
    static async Task Main(string[] args)
    {
        var app = new Program();
        await Task.WhenAll(
            app.RunConnection(HttpTransportType.WebSockets),
            app.RunConnection(HttpTransportType.ServerSentEvents),
            app.RunConnection(HttpTransportType.LongPolling));
    }

    private const string ServerUrl = "http://localhost:54543";

    private readonly ConcurrentDictionary<string, Task<string>> _tokens = new ConcurrentDictionary<string, Task<string>>(StringComparer.Ordinal);

    private async Task RunConnection(HttpTransportType transportType)
    {
        var userId = "C#" + transportType;
        _tokens[userId] = GetJwtToken(userId);

        var hubConnection = new HubConnectionBuilder()
            .WithUrl(ServerUrl + "/broadcast", options =>
            {
                options.Transports = transportType;
                options.AccessTokenProvider = () => _tokens[userId];
            })
            .Build();

        var closedTcs = new TaskCompletionSource();
        hubConnection.Closed += e =>
        {
            closedTcs.SetResult();
            return Task.CompletedTask;
        };

        hubConnection.On<string, string>("Message", (sender, message) => Console.WriteLine($"[{userId}] {sender}: {message}"));
        await hubConnection.StartAsync();
        Console.WriteLine($"[{userId}] Connection Started");

        var ticks = 0;
        var nextMsgAt = 3;

        try
        {
            while (!closedTcs.Task.IsCompleted)
            {
                await Task.Delay(1000);
                ticks++;
                if (ticks % 15 == 0)
                {
                    // no need to refresh the token for websockets
                    if (transportType != HttpTransportType.WebSockets)
                    {
                        _tokens[userId] = GetJwtToken(userId);
                        Console.WriteLine($"[{userId}] Token refreshed");
                    }
                }

                if (ticks % nextMsgAt == 0)
                {
                    await hubConnection.SendAsync("Broadcast", userId, $"Hello at {DateTime.Now}");
                    nextMsgAt = Random.Shared.Next(2, 5);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{userId}] Connection terminated with error: {ex}");
        }
    }

    private static async Task<string> GetJwtToken(string userId)
    {
        var httpResponse = await new HttpClient().GetAsync(ServerUrl + $"/generatetoken?user={userId}");
        httpResponse.EnsureSuccessStatusCode();
        return await httpResponse.Content.ReadAsStringAsync();
    }
}
