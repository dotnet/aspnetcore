#if (NativeAot)
using System.Text.Json.Serialization;
#endif
using Company.ApiApplication1;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();

#if (NativeAot)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.AddContext<AppJsonSerializerContext>();
});

#endif
var app = builder.Build();

var sampleTodos = TodoGenerator.GenerateTodos().ToArray();

var api = app.MapGroup("/todos");
api.MapGet("/", () => sampleTodos);
api.MapGet("/{id}", (int id) =>
    sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

app.Run();

#if (NativeAot)
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
