// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using SignalRSamples.ConnectionHandlers;
using SignalRSamples.Hubs;

namespace SignalRSamples;

public class Startup
{
    private readonly JsonWriterOptions _jsonWriterOptions = new JsonWriterOptions { Indented = true };

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddConnections();

        services.AddSignalR(o => o.MaximumParallelInvocationsPerClient = 2)
        .AddMessagePackProtocol()
        .AddStackExchangeRedis();

        services.AddSingleton<Game>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseFileServer();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/start", (Game game, string connection1, string connection2) =>
            {
                _ = game.GameLoop(connection1, connection2);
            });

            endpoints.MapHub<DynamicChat>("/dynamic");
            endpoints.MapHub<Chat>("/default");
            endpoints.MapHub<Streaming>("/streaming");
            endpoints.MapHub<UploadHub>("/uploading");
            endpoints.MapHub<HubTChat>("/hubT");
            endpoints.MapHub<GameHub>("/game");

            endpoints.MapConnectionHandler<MessagesConnectionHandler>("/chat");

            endpoints.MapGet("/deployment", async context =>
            {
                var attributes = Assembly.GetAssembly(typeof(Startup)).GetCustomAttributes<AssemblyMetadataAttribute>();

                context.Response.ContentType = "application/json";
                await using (var writer = new Utf8JsonWriter(context.Response.BodyWriter, _jsonWriterOptions))
                {
                    writer.WriteStartObject();
                    var commitHash = string.Empty;

                    foreach (var attribute in attributes)
                    {
                        writer.WriteString(attribute.Key, attribute.Value);

                        if (string.Equals(attribute.Key, "CommitHash"))
                        {
                            commitHash = attribute.Value;
                        }
                    }

                    if (!string.IsNullOrEmpty(commitHash))
                    {
                        writer.WriteString("GitHubUrl", $"https://github.com/aspnet/SignalR/commit/{commitHash}");
                    }

                    writer.WriteEndObject();
                    await writer.FlushAsync();
                }
            });
        });
    }
}

// Nurdle
public class Game
{
    public string Player1Id { get; set; }
    public string Player2Id { get; set; }

    private readonly IHubContext<GameHub> _hubContext;

    public Game(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public void AddPlayer(string Id)
    {
        if (string.IsNullOrEmpty(Player1Id))
        {
            Player1Id = Id;
        }
        else
        {
            Player2Id = Id;
        }
    }

    public async Task GameLoop(string connection1, string connection2)
    {
        var randomAnswer = Random.Shared.Next(2, 10);
        var res = 0;

        do
        {
            await Task.Delay(1000);
            var task1 = _hubContext.Clients.Single(connection1).InvokeAsync<int>("GetNumber");
            var task2 = _hubContext.Clients.Single(connection2).InvokeAsync<int>("GetNumber");
            res = (await task1) + (await task2);

            if (res < randomAnswer)
            {
                await _hubContext.Clients.Clients(connection1, connection2).SendAsync("Result", $"Guessed {res} which is too low");
            }
            else if (res > randomAnswer)
            {
                await _hubContext.Clients.Clients(connection1, connection2).SendAsync("Result", $"Guessed {res} which is too high");
            }
        }
        while (res != randomAnswer);

        await _hubContext.Clients.Clients(connection1, connection2).SendAsync("Result", $"Guessed {res} which is correct!");
    }
}
