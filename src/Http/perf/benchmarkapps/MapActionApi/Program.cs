using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using var host = Host.CreateDefaultBuilder(args)
    // REVIEW: do we want to measure default logging overhead?
    .ConfigureLogging(logBuilder => logBuilder.ClearProviders())
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.Configure(app =>
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                Todo EchoTodo([FromBody] Todo todo) => todo;
                endpoints.MapPost("/EchoTodo", (Func<Todo, Todo>)EchoTodo);
            });

        });
    })
    .Build();

await host.StartAsync();

Console.WriteLine("Application started.");

await host.WaitForShutdownAsync();

record Todo(int Id, string Name, bool IsComplete);
