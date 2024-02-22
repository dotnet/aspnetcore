using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApiDocument();

app.MapPost("/polymoprhism", (Shape shape) => { });
app.MapPost("/inheritance", (TodoFromInterface todo) => { });
app.MapGet("/rout-constraints/{age:min(18)}/{ssn:regex(^\\d{{3}}-\\d{{2}}-\\d{{4}}$)}/{username:minlength(4)}/{filename:length(12)}", (int age, string ssn, string username, string filename) => { });
app.MapPost("/validations", ([Range(1, 10)] int number, [RegularExpression(@"^\d{3}-\d{2}-\d{4}$")] string ssn, ValidatedTodo todo) => { });

app.Run();

public interface ITodo
{
    int Id { get; }
}

public enum TodoStatus
{
    NotStarted,
    InProgress,
    Completed
}

public record TodoFromInterface(int Id, string Title, bool Completed, DateTime CreatedAt) : ITodo;

public record Todo(int Id, string Title, bool Completed, DateTime CreatedAt);
public record TodoWithDueDate(int Id, string Title, bool Completed, DateTime CreatedAt, DateTime DueDate) : Todo(Id, Title, Completed, CreatedAt);

[JsonDerivedType(typeof(Triangle), typeDiscriminator: "triangle")]
[JsonDerivedType(typeof(Square), typeDiscriminator: "square")]
public class Shape
{
    public string Color { get; set; } = "blue";
    public int Sides { get; set; }
}

public class Triangle : Shape { }
public class Square : Shape { }

public class ValidatedTodo
{
    [Range(1, 10)]
    public int Id { get; set; }

    [MaxLength(40)]
    public required string Title { get; set; }

    [MinLength(10)]
    public required string Description { get; set; }

    [RegularExpression(@"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+")]
    public required string Email { get; set; }
}

