// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Abstractions.Tests;

public class RouteHandlerInvocationContextOfTTests
{
    [Fact]
    public void ProhibitsActionsThatModifyListSize()
    {
        var context = new RouteHandlerInvocationContext<string, int, bool>(new DefaultHttpContext(), "This is a test", 42, false);
        Assert.Throws<NotSupportedException>(() => context.Add("string"));
        Assert.Throws<NotSupportedException>(() => context.Insert(0, "string"));
        Assert.Throws<NotSupportedException>(() => context.RemoveAt(0));
        Assert.Throws<NotSupportedException>(() => context.Remove("string"));
        Assert.Throws<NotSupportedException>(() => context.Clear());
    }

    [Fact]
    public void ThrowsExceptionForInvalidCastOnGetParameter()
    {
        var context = new RouteHandlerInvocationContext<string, int, bool, Todo>(new DefaultHttpContext(), "This is a test", 42, false, new Todo());
        Assert.Throws<InvalidCastException>(() => context.GetParameter<string>(1));
        Assert.Throws<InvalidCastException>(() => context.GetParameter<int>(0));
        Assert.Throws<InvalidCastException>(() => context.GetParameter<string>(3));
        var todo = context.GetParameter<ITodo>(3);
        Assert.IsType<Todo>(todo);
    }

    [Fact]
    public void SetterAllowsInPlaceModificationOfParameters()
    {
        var context = new RouteHandlerInvocationContext<string, int, bool, Todo>(new DefaultHttpContext(), "This is a test", 42, false, new Todo());
        context[0] = "Foo";
        Assert.Equal("Foo", context.GetParameter<string>(0));
    }

    [Fact]
    public void SetterDoesNotAllowModificationOfParameterType()
    {
        var context = new RouteHandlerInvocationContext<string, int, bool, Todo>(new DefaultHttpContext(), "This is a test", 42, false, new Todo());
        Assert.Throws<InvalidCastException>(() => context[0] = 4);
    }

    [Fact]
    public void AllowsEnumerationOfParameters()
    {
        var context = new RouteHandlerInvocationContext<string, int, bool, Todo>(new DefaultHttpContext(), "This is a test", 42, false, new Todo());
        var enumeratedCount = 0;
        foreach (var parameter in context)
        {
            Assert.NotNull(parameter);
            enumeratedCount++;
        }
        Assert.Equal(4, enumeratedCount);
    }

    interface ITodo { }
    class Todo : ITodo { }
}
