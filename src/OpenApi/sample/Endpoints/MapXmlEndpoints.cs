// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public static class XmlEndpointExtensions
{
    public static IEndpointRouteBuilder MapXmlEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var group = endpointRouteBuilder.MapGroup("/xml")
            .WithGroupName("xml");

        group.MapGet("/type-with-examples", (TypeWithExamples typeWithExamples) =>
            TypedResults.Ok(typeWithExamples));

        group.MapPost("/todo", (TodoFomInterface todo) => { });
        group.MapPost("/project", (Project project) => { });
        group.MapPost("/board", (ProjectBoard.BoardItem boardItem) => { });

        group.MapPost("/project-record", (ProjectRecord projectRecord) => { });

        group.MapPost("/todo-with-description", (TodoWithDescription todo) => { });

        return endpointRouteBuilder;
    }

    public class TypeWithExamples
    {
        /// <example>true</example>
        public bool BooleanType { get; set; }
        /// <example>42</example>
        public int IntegerType { get; set; }
        /// <example>1234567890123456789</example>
        public long LongType { get; set; }
        /// <example>3.14</example>
        public double DoubleType { get; set; }
        /// <example>3.14</example>
        public float FloatType { get; set; }
        /// <example>2022-01-01T00:00:00Z</example>
        public DateTime DateTimeType { get; set; }
        /// <example>2022-01-01</example>
        public DateOnly DateOnlyType { get; set; }
    }

    public interface ITodo
    {
        /// <summary>
        /// The identifier of the todo.
        /// </summary>
        int Id { get; set; }

        /// <value>
        /// The name of the todo.
        /// </value>
        string Name { get; set; }
    }

    /// <summary>
    /// This is a todo item.
    /// </summary>
    public class TodoFomInterface : ITodo
    {
        /// <inheritdoc />
        public int Id { get; set; }

        /// <inheritdoc />
        public required string Name { get; set; }

        /// <summary>
        /// A description of the todo.
        /// </summary>
        public required string Description { get; set; }
    }

    /// <summary>
    /// The project that contains <see cref="Todo"/> items.
    /// </summary>
    public record Project(string Name, string Description);

    public class ProjectBoard
    {
        /// <summary>
        /// An item on the board.
        /// </summary>
        public class BoardItem
        {
            public required string Name { get; set; }
        }
    }

    /// <summary>
    /// The project that contains <see cref="Todo"/> items.
    /// </summary>
    /// <param name="Name">The name of the project.</param>
    /// <param name="Description">The description of the project.</param>
    public record ProjectRecord(string Name, string Description);

    public class TodoWithDescription : ITodo
    {
        /// <summary>
        /// The identifier of the todo, overridden.
        /// </summary>
        public int Id { get; set; }

        /// <value>
        /// The name of the todo, overridden.
        /// </value>
        public required string Name { get; set; }

        /// <summary>
        /// A description of the the todo.
        /// </summary>
        /// <value>Another description of the todo.</value>
        public required string Description { get; set; }
    }
}
