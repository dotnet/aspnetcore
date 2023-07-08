#if NativeAot
using System.Text.Json.Serialization;

#endif
namespace Company.ApiApplication1;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        #if (NativeAot)
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });

        #endif
        var app = builder.Build();

        Todo[] sampleTodos = [
            new(1, "Walk the dog"),
            new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
            new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
            new(4, "Clean the bathroom"),
            new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
        ];

        var todosApi = app.MapGroup("/todos");
        todosApi.MapGet("/", () => sampleTodos);
        todosApi.MapGet("/{id}", (int id) =>
            sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
                ? Results.Ok(todo)
                : Results.NotFound());

        app.Run();
    }
}

public class Todo(int id, string? title, DateOnly? dueBy = null, bool isComplete = false)
{
    public int Id { get; set; } = id;

    public string? Title { get; set; } = title;

    public DateOnly? DueBy { get; set; } = dueBy;

    public bool IsComplete { get; set; } = isComplete;
}

#if (NativeAot)
[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
