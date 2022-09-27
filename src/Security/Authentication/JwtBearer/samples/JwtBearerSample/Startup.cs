// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace JwtBearerSample;

public class Startup
{
    public Startup(IConfiguration config)
    {
        Configuration = config;
    }

    public IConfiguration Configuration { get; set; }

    // Shared between users in memory
    public IList<Todo> Todos { get; } = new List<Todo>();

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                // You also need to update /wwwroot/app/scripts/app.js
                o.Authority = Configuration["oidc:authority"];
                o.Audience = Configuration["oidc:clientid"];
            });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseAuthentication();

        // [Authorize] would usually handle this
        app.Use(async (context, next) =>
        {
            // Use this if there are multiple authentication schemes
            var authResult = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            if (authResult.Succeeded && authResult.Principal.Identity.IsAuthenticated)
            {
                await next(context);
            }
            else if (authResult.Failure != null)
            {
                // Rethrow, let the exception page handle it.
                ExceptionDispatchInfo.Capture(authResult.Failure).Throw();
            }
            else
            {
                await context.ChallengeAsync();
            }
        });

        // MVC would usually handle this:
        app.Map("/api/TodoList", todoApp =>
        {
            todoApp.Run(async context =>
            {
                var response = context.Response;
                if (HttpMethods.IsPost(context.Request.Method))
                {
                    var reader = new StreamReader(context.Request.Body);
                    var body = await reader.ReadToEndAsync();
                    using (var json = JsonDocument.Parse(body))
                    {
                        var obj = json.RootElement;
                        var todo = new Todo() { Description = obj.GetProperty("Description").GetString(), Owner = context.User.Identity.Name };
                        Todos.Add(todo);
                    }
                }
                else
                {
                    response.ContentType = "application/json";
                    response.Headers.CacheControl = "no-cache";
                    await response.StartAsync();
                    Serialize(Todos, response.BodyWriter);
                    await response.BodyWriter.FlushAsync();
                }
            });
        });
    }

    private void Serialize(IList<Todo> todos, IBufferWriter<byte> output)
    {
        using var writer = new Utf8JsonWriter(output);
        writer.WriteStartArray();
        foreach (var todo in todos)
        {
            writer.WriteStartObject();
            writer.WriteString("Description", todo.Description);
            writer.WriteString("Owner", todo.Owner);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.Flush();
    }
}
