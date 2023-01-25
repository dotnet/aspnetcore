using System.Text.Json.Serialization;
#if NativeAot
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
#endif
namespace Company.ApiApplication1;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);
        builder.Logging.AddConsole();

        #if (NativeAot)
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.AddContext<AppJsonSerializerContext>();
        });

        #endif
        var app = builder.Build();

        var sampleTodos = TodoGenerator.GenerateTodos().ToArray();

        var todosApi = app.MapGroup("/todos");
        todosApi.MapGet("/", () => sampleTodos);
        todosApi.MapGet("/{id}", (int id) =>
            sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
                ? Results.Ok(todo)
                : Results.NotFound());

        app.Run();
    }
}

#if (NativeAot)
[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
