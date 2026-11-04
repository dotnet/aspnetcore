// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Mono.TextTemplating;

namespace Microsoft.AspNetCore.Http.Abstractions.Tests;

public class EndpointFilterInvocationContextOfTTests
{
    [Fact]
    public void ProhibitsActionsThatModifyListSize()
    {
        var context = new EndpointFilterInvocationContext<string, int, bool>(new DefaultHttpContext(), "This is a test", 42, false);
        Assert.Throws<NotSupportedException>(() => context.Add("string"));
        Assert.Throws<NotSupportedException>(() => context.Insert(0, "string"));
        Assert.Throws<NotSupportedException>(() => context.RemoveAt(0));
        Assert.Throws<NotSupportedException>(() => context.Remove("string"));
        Assert.Throws<NotSupportedException>(() => context.Clear());
    }

    [Fact]
    public void ThrowsExceptionForInvalidCastOnGetArgument()
    {
        var context = new EndpointFilterInvocationContext<string, int, bool, Todo>(new DefaultHttpContext(), "This is a test", 42, false, new Todo());
        Assert.Throws<InvalidCastException>(() => context.GetArgument<string>(1));
        Assert.Throws<InvalidCastException>(() => context.GetArgument<int>(0));
        Assert.Throws<InvalidCastException>(() => context.GetArgument<string>(3));
        var todo = context.GetArgument<ITodo>(3);
        Assert.IsType<Todo>(todo);
    }

    [Fact]
    public void SetterAllowsInPlaceModificationOfParameters()
    {
        var context = new EndpointFilterInvocationContext<string, int, bool, Todo>(new DefaultHttpContext(), "This is a test", 42, false, new Todo());
        context[0] = "Foo";
        Assert.Equal("Foo", context.GetArgument<string>(0));
    }

    [Fact]
    public void SetterDoesNotAllowModificationOfParameterType()
    {
        var context = new EndpointFilterInvocationContext<string, int, bool, Todo>(new DefaultHttpContext(), "This is a test", 42, false, new Todo());
        Assert.Throws<InvalidCastException>(() => context[0] = 4);
    }

    [Fact]
    public void AllowsEnumerationOfParameters()
    {
        var context = new EndpointFilterInvocationContext<string, int, bool, Todo>(new DefaultHttpContext(), "This is a test", 42, false, new Todo());
        var enumeratedCount = 0;
        foreach (var parameter in context)
        {
            Assert.NotNull(parameter);
            enumeratedCount++;
        }
        Assert.Equal(4, enumeratedCount);
    }

    [Fact]
    public void HandlesIListReadOperations()
    {
        var context = new EndpointFilterInvocationContext<int?, string, int, bool>(new DefaultHttpContext(), (int?)null, "This is a test", 42, false);
#pragma warning disable xUnit2017 // Do not use Contains() to check if a value exists in a collection
        Assert.True(context.Contains("This is a test"));
        Assert.False(context.Contains("This does not exist"));
#pragma warning restore xUnit2017 // Do not use Contains() to check if a value exists in a collection
        Assert.Equal(2, context.IndexOf(42));
        Assert.Equal(-1, context.IndexOf(21));
    }

    // Test for https://github.com/dotnet/aspnetcore/issues/41489
    [Fact]
    public void HandlesMismatchedNullabilityOnTypeParams()
    {
        var context = new EndpointFilterInvocationContext<string?, int?, bool?, Todo?>(new DefaultHttpContext(), null, null, null, null);
        // Mismatched reference types will resolve as null
        Assert.Null(context.GetArgument<string>(0));
        Assert.Null(context.GetArgument<Todo>(3));
        // Mismatched value types will throw
        Assert.Throws<NullReferenceException>(() => context.GetArgument<int>(1));
        Assert.Throws<NullReferenceException>(() => context.GetArgument<bool>(2));
    }

    [Fact]
    public void GeneratedCodeIsUpToDate()
    {
        var currentContentPath = Path.Combine(AppContext.BaseDirectory, "Shared", "GeneratedContent", "EndpointFilterInvocationContextOfT.Generated.cs");
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Shared", "GeneratedContent", "EndpointFilterInvocationContextOfT.Generated.tt");

        var generator = new TemplateGenerator();
        var compiledTemplate = generator.CompileTemplate(File.ReadAllText(templatePath));

        var generatedContent = compiledTemplate.Process();
        var currentContent = File.ReadAllText(currentContentPath);

        Assert.Equal(currentContent, generatedContent);
    }

    interface ITodo { }
    class Todo : ITodo { }
}
