using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public interface ITodo
{
    int Id { get; }
}

public record Todo(int Id, string Title, bool Completed, DateTime CreatedAt);

public record TodoFromInterface(int Id, string Title, bool Completed, DateTime CreatedAt) : ITodo;
public record TodoWithDueDate(int Id, string Title, bool Completed, DateTime CreatedAt, DateTime DueDate) : Todo(Id, Title, Completed, CreatedAt);

[JsonDerivedType(typeof(Triangle))]
[JsonDerivedType(typeof(Square))]
public class Shape
{
    public string Color { get; set; }
    public int Sides { get; set; }
}

public class Triangle : Shape { }
public class Square : Shape { }

public enum TodoStatus
{
    NotStarted,
    InProgress,
    Completed
}

public class ValidatedTodo
{
    [Range(1, 10)]
    public int Id { get; set; }

    [MaxLength(40)]
    public string Title { get; set; }

    [MinLength(10)]
    public string Description { get; set; }

    [RegularExpression(@"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+")]
    public string Email { get; set; }
}
