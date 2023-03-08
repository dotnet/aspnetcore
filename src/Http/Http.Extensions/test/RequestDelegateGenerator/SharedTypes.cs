// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

#nullable  enable

public class TestService
{
    public string TestServiceMethod() => "Produced from service!";
}

public class Todo
{
    public int Id { get; set; }
    public string? Name { get; set; } = "Todo";
    public bool IsComplete { get; set; }
}

public class TryParseTodo : Todo
{
    public static bool TryParse(string input, out TryParseTodo? result)
    {
        if (input == "1")
        {
            result = new TryParseTodo
            {
                Id = 1,
                Name = "Knit kitten mittens.",
                IsComplete = false
            };
            return true;
        }
        else
        {
            result = null;
            return false;
        }
    }
}

public class CustomFromBodyAttribute : Attribute, IFromBodyMetadata
{
    public bool AllowEmpty { get; set; }
}

public enum TodoStatus
{
    Trap, // A trap for Enum.TryParse<T>!
    Done,
    InProgress,
    NotDone
}

public interface ITodo
{
    public int Id { get; }
    public string? Name { get; }
    public bool IsComplete { get; }
    public TodoStatus Status { get; }
}

public class PrecedenceCheckTodo
{
    public PrecedenceCheckTodo(int magicValue)
    {
        MagicValue = magicValue;
    }
    public int MagicValue { get; }
    public static bool TryParse(string? input, IFormatProvider? provider, out PrecedenceCheckTodo result)
    {
        result = new PrecedenceCheckTodo(42);
        return true;
    }
    public static bool TryParse(string? input, out PrecedenceCheckTodo result)
    {
        result = new PrecedenceCheckTodo(24);
        return true;
    }
}

public enum MyEnum { ValueA, ValueB, }

public record MyTryParseRecord(Uri Uri)
{
    public static bool TryParse(string value, out MyTryParseRecord? result)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            result = null;
            return false;
        }

        result = new MyTryParseRecord(uri);
        return true;
    }
}

public class PrecedenceCheckTodoWithoutFormat
{
    public PrecedenceCheckTodoWithoutFormat(int magicValue)
    {
        MagicValue = magicValue;
    }
    public int MagicValue { get; }
    public static bool TryParse(string? input, out PrecedenceCheckTodoWithoutFormat result)
    {
        result = new PrecedenceCheckTodoWithoutFormat(24);
        return true;
    }
}

public class ParsableTodo : IParsable<ParsableTodo>
{
    public int Id { get; set; }
    public string? Name { get; set; } = "Todo";
    public bool IsComplete { get; set; }
    public static ParsableTodo Parse(string s, IFormatProvider? provider)
    {
        return new ParsableTodo();
    }
    public static bool TryParse(string? input, IFormatProvider? provider, out ParsableTodo result)
    {
        if (input == "1")
        {
            result = new ParsableTodo
            {
            Id = 1,
            Name = "Knit kitten mittens.",
            IsComplete = false
            };
            return true;
        }
        else
        {
        result = null!;
        return false;
        }
    }
}

public record MyBindAsyncRecord(Uri Uri)
{
    public static ValueTask<MyBindAsyncRecord?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        if (parameter.ParameterType != typeof(MyBindAsyncRecord))
        {
            throw new UnreachableException($"Unexpected parameter type: {parameter.ParameterType}");
        }
        if (parameter.Name != "myBindAsyncParam")
        {
            throw new UnreachableException("Unexpected parameter name");
        }

        if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
        {
            return new(result: null);
        }

        return new(result: new(uri));
    }

    // BindAsync(HttpContext, ParameterInfo) should be preferred over TryParse(string, ...) if there's
    // no [FromRoute] or [FromQuery] attributes.
    public static bool TryParse(string? value, out MyBindAsyncRecord? result) =>
        throw new NotImplementedException();
}

public record struct MyNullableBindAsyncStruct(Uri Uri)
{
    public static ValueTask<MyNullableBindAsyncStruct?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        if (parameter.ParameterType != typeof(MyNullableBindAsyncStruct) && parameter.ParameterType != typeof(MyNullableBindAsyncStruct?))
        {
            throw new UnreachableException("Unexpected parameter type");
        }
        if (parameter.Name != "myBindAsyncParam")
        {
            throw new UnreachableException("Unexpected parameter name");
        }

        if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
        {
            return new(result: null);
        }

        return new(result: new(uri));
    }

    public static bool TryParse(string? value, out MyNullableBindAsyncStruct? result) =>
        throw new NotImplementedException();
}

public record struct MyBindAsyncStruct(Uri Uri)
{
    public static ValueTask<MyBindAsyncStruct> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        if (parameter.ParameterType != typeof(MyBindAsyncStruct) && parameter.ParameterType != typeof(MyBindAsyncStruct?))
        {
            throw new UnreachableException("Unexpected parameter type");
        }
        if (parameter.Name != "myBindAsyncParam")
        {
            throw new UnreachableException("Unexpected parameter name");
        }

        if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
        {
            throw new BadHttpRequestException("The request is missing the required Referer header.");
        }

        return new(result: new(uri));
    }

    // BindAsync(HttpContext, ParameterInfo) should be preferred over TryParse(string, ...) if there's
    // no [FromRoute] or [FromQuery] attributes.
    public static bool TryParse(string? value, out MyBindAsyncStruct result) =>
        throw new NotImplementedException();
}

public record struct MyBothBindAsyncStruct(Uri Uri)
{
    public static ValueTask<MyBothBindAsyncStruct> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        if (parameter.ParameterType != typeof(MyBothBindAsyncStruct) && parameter.ParameterType != typeof(MyBothBindAsyncStruct?))
        {
            throw new UnreachableException("Unexpected parameter type");
        }
        if (parameter.Name != "myBindAsyncParam")
        {
            throw new UnreachableException("Unexpected parameter name");
        }

        if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
        {
            throw new BadHttpRequestException("The request is missing the required Referer header.");
        }

        return new(result: new(uri));
    }

    // BindAsync with ParameterInfo is preferred
    public static ValueTask<MyBothBindAsyncStruct> BindAsync(HttpContext context) =>
        throw new NotImplementedException();
}

public record struct MySimpleBindAsyncStruct(Uri Uri)
{
    public static ValueTask<MySimpleBindAsyncStruct> BindAsync(HttpContext context)
    {
        if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
        {
            throw new BadHttpRequestException("The request is missing the required Referer header.");
        }

        return new(result: new(uri));
    }

    public static bool TryParse(string? value, out MySimpleBindAsyncStruct result) =>
        throw new NotImplementedException();
}

public record MySimpleBindAsyncRecord(Uri Uri)
{
    public static ValueTask<MySimpleBindAsyncRecord?> BindAsync(HttpContext context)
    {
        if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
        {
            return new(result: null);
        }

        return new(result: new(uri));
    }

    public static bool TryParse(string? value, out MySimpleBindAsyncRecord? result) =>
        throw new NotImplementedException();
}

public interface IBindAsync<T>
{
    static ValueTask<T?> BindAsync(HttpContext context)
    {
        if (typeof(T) != typeof(MyBindAsyncFromInterfaceRecord))
        {
            throw new UnreachableException("Unexpected parameter type");
        }

        if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
        {
            return new(default(T));
        }

        return new(result: (T)(object)new MyBindAsyncFromInterfaceRecord(uri));
    }
}

public class BindAsyncWrongType
{
    public static ValueTask<MyBindAsyncRecord?> BindAsync(HttpContext context, ParameterInfo parameter) =>
        throw new UnreachableException("We shouldn't bind from the wrong type!");
}

public record MyBindAsyncFromInterfaceRecord(Uri Uri) : IBindAsync<MyBindAsyncFromInterfaceRecord>
{
}

public interface IHaveUri
{
    Uri? Uri { get; set; }
}

public class BaseBindAsync<T> where T : IHaveUri, new()
{
    public static ValueTask<T?> BindAsync(HttpContext context)
    {
        if (typeof(T) != typeof(InheritBindAsync))
        {
            throw new UnreachableException("Unexpected parameter type");
        }

        if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
        {
            return new(default(T));
        }

        return new(result: new() { Uri = uri });
    }
}

public class InheritBindAsync : BaseBindAsync<InheritBindAsync>, IHaveUri
{
    public Uri? Uri { get; set; }
}

// Using wrong T on purpose
public class InheritBindAsyncWrongType : BaseBindAsync<InheritBindAsync>
{
}

public class BindAsyncFromImplicitStaticAbstractInterface : IBindableFromHttpContext<BindAsyncFromImplicitStaticAbstractInterface>
{
    public Uri? Uri { get; init; }

    public static ValueTask<BindAsyncFromImplicitStaticAbstractInterface?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        if (parameter.ParameterType != typeof(BindAsyncFromImplicitStaticAbstractInterface))
        {
            throw new UnreachableException("Unexpected parameter type");
        }
        if (parameter.Name != "myBindAsyncParam")
        {
            throw new UnreachableException("Unexpected parameter name");
        }

        if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
        {
            return new(result: null);
        }

        return new(result: new() { Uri = uri });
    }
}

public class BindAsyncFromExplicitStaticAbstractInterface : IBindableFromHttpContext<BindAsyncFromExplicitStaticAbstractInterface>
{
    public Uri? Uri { get; init; }

    static ValueTask<BindAsyncFromExplicitStaticAbstractInterface?> IBindableFromHttpContext<BindAsyncFromExplicitStaticAbstractInterface>.BindAsync(HttpContext context, ParameterInfo parameter)
    {
        if (parameter.ParameterType != typeof(BindAsyncFromExplicitStaticAbstractInterface))
        {
            throw new UnreachableException("Unexpected parameter type");
        }
        if (parameter.Name != "myBindAsyncParam")
        {
            throw new UnreachableException("Unexpected parameter name");
        }

        if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
        {
            return new(result: null);
        }

        return new(result: new() { Uri = uri });
    }
}

public class BindAsyncFromStaticAbstractInterfaceWrongType : IBindableFromHttpContext<BindAsyncFromImplicitStaticAbstractInterface>
{
    public static ValueTask<BindAsyncFromImplicitStaticAbstractInterface?> BindAsync(HttpContext context, ParameterInfo parameter) =>
        throw new UnreachableException("We shouldn't bind from the wrong interface type!");
}

public class MyBindAsyncTypeThatThrows
{
    public static ValueTask<MyBindAsyncTypeThatThrows?> BindAsync(HttpContext context, ParameterInfo parameter) =>
        throw new InvalidOperationException("BindAsync failed");
}

public struct BodyStruct
{
    public int Id { get; set; }
}

#nullable restore
