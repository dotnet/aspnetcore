// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    private async Task RunConnection(HttpTransportType transportType)
    {
        var userId = "C#" + transportType;

        var hubConnection = new HubConnectionBuilder()
            .WithUrl(ServerUrl + "/broadcast", options =>
            {
                options.Transports = transportType;
                options.AccessTokenProvider = () => GetJwtToken(userId);
            })
            .WithAuthenticationRefresh(o =>
            {
                o.RefreshBeforeExpiration = TimeSpan.FromSeconds(10);
            })
            .Build();

        var closedTcs = new TaskCompletionSource();
        hubConnection.Closed += e =>
        {
            Console.WriteLine(e);
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

                if (ticks % nextMsgAt == 0)
                {
                    await hubConnection.SendAsync("Broadcast", userId, $"Hello at {DateTime.Now}");
                    nextMsgAt = Random.Shared.Next(20, 50);
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
