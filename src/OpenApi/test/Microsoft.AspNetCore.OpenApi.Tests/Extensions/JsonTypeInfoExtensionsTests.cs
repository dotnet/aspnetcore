// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;

public class JsonTypeInfoExtensionsTests
{
    private delegate void TestDelegate(int x, int y);

    private class Container
    {
        internal delegate void ContainedTestDelegate(int x, int y);
    }

    /// <remarks>
    /// https://github.com/dotnet/aspnetcore/issues/59092
    /// </remarks>
    public static class Foo<T>
    {
        public static class Bar<TT>
        {
            public class Baz
            {
                public required T One { get; set; }
                public required TT Two { get; set; }
            }
        }
    }

    /// <summary>
    /// This data is used to test the <see cref="TypeExtensions.GetSchemaReferenceId"/> method
    /// which is used to generate reference IDs for OpenAPI schemas in the OpenAPI document.
    /// <remarks>
    /// Some things of note:
    /// - For generic types, we generate the reference ID by appending the type arguments to the type name.
    /// Our implementation currently supports versions of OpenAPI up to v3.0 which do not include support for
    /// generic types in schemas. This means that generic types must be resolved to their concrete types before
    /// being encoded in teh OpenAPI document.
    /// - Array-like types (List, IEnumerable, etc.) are represented as "ArrayOf" followed by the type name of the
    /// element type.
    /// - Dictionary-list types are represented as "DictionaryOf" followed by the key type and the value type.
    /// - Supported primitive types are mapped to their corresponding names (string, char, Uri, etc.).
    /// </remarks>
    /// </summary>
    public static IEnumerable<object[]> GetSchemaReferenceId_Data =>
    [
        [typeof(Todo), "Todo"],
        [typeof(IEnumerable<Todo>), null],
        [typeof(List<Todo>), null],
        [typeof(TodoWithDueDate), "TodoWithDueDate"],
        [typeof(IEnumerable<TodoWithDueDate>), null],
        [(new { Id = 1 }).GetType(), "AnonymousTypeOfint"],
        [(new { Id = 1, Name = "Todo" }).GetType(), "AnonymousTypeOfintAndstring"],
        [typeof(IFormFile), "IFormFile"],
        [typeof(IFormFileCollection), "IFormFileCollection"],
        [typeof(Stream), "Stream"],
        [typeof(PipeReader), "PipeReader"],
        [typeof(Results<Ok<TodoWithDueDate>, Ok<Todo>>), "ResultsOfOkOfTodoWithDueDateAndOkOfTodo"],
        [typeof(Ok<Todo>), "OkOfTodo"],
        [typeof(NotFound<TodoWithDueDate>), "NotFoundOfTodoWithDueDate"],
        [typeof(TestDelegate), "TestDelegate"],
        [typeof(Container.ContainedTestDelegate), "ContainedTestDelegate"],
        [typeof(List<int>), null],
        [typeof(List<List<int>>), null],
        [typeof(int[]), null],
        [typeof(ValidationProblemDetails), "ValidationProblemDetails"],
        [typeof(ProblemDetails), "ProblemDetails"],
        [typeof(Dictionary<string, string[]>), null],
        [typeof(Dictionary<string, List<string[]>>), null],
        [typeof(Dictionary<string, IEnumerable<string[]>>), null],
        [typeof(Foo<int>.Bar<string>.Baz), "BazOfintAndstring"],
    ];

    [Theory]
    [MemberData(nameof(GetSchemaReferenceId_Data))]
    public void GetSchemaReferenceId_Works(Type type, string referenceId)
    {
        var jsonTypeInfo = JsonSerializerOptions.Default.GetTypeInfo(type);
        Assert.Equal(referenceId, jsonTypeInfo.GetSchemaReferenceId());
    }
}
