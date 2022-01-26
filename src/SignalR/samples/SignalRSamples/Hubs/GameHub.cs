// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR;

namespace SignalRSamples.Hubs;

public class GameHub : Hub
{
    private readonly Game _game;

    public GameHub(Game game)
    {
        _game = game;
    }

    public Task AddPlayer()
    {
        //_ = await Clients.Caller.InvokeAsync<int>("GetNumber");
        //Clients.Caller.InvokeClientAsync();
        _game.AddPlayer(Context.ConnectionId);
        return Task.CompletedTask;
    }
}
