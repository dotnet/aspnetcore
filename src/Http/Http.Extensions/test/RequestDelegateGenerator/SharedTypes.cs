// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

#nullable  enable

public class TestService
{
    public string TestServiceMethod() => "Produced from service!";
}

public class CustomMetadata
{
    public int? Value { get; set; }
}

public class CustomMetadataEmitter : IEndpointMetadataProvider, IEndpointParameterMetadataProvider
{
    public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
    {
        builder.Metadata.Add(new CustomMetadata()
        {
            Value = 42
        });
    }

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        builder.Metadata.Add(new CustomMetadata()
        {
            Value = 24
        });
    }
}

public interface ITodo
{
    public int Id { get; }
    public string? Name { get; }
    public bool IsComplete { get; }
}

public class Todo : ITodo
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

[JsonPolymorphic]
[JsonDerivedType(typeof(JsonTodoChild), nameof(JsonTodoChild))]
public class JsonTodo : Todo
{
    public static async ValueTask<JsonTodo?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        // manually call deserialize so we don't check content type
        var body = await JsonSerializer.DeserializeAsync<JsonTodo>(context.Request.Body);
        context.Request.Body.Position = 0;
        return body;
    }
}

public class JsonTodoChild : JsonTodo
{
    public string? Child { get; set; }
}

[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(IAsyncEnumerable<JsonTodo>))]
public partial class SharedTestJsonContext : JsonSerializerContext
{ }

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

public class CustomTodo : Todo
{
    public static async ValueTask<CustomTodo?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        Assert.Equal(typeof(CustomTodo), parameter.ParameterType);
        Assert.Equal("customTodo", parameter.Name);

        var body = await context.Request.ReadFromJsonAsync<CustomTodo>();
        context.Request.Body.Position = 0;
        return body;
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
        if (parameter.Name?.StartsWith("myBindAsyncParam", StringComparison.OrdinalIgnoreCase) == false)
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

public class ExceptionThrowingRequestBodyStream : Stream
{
    public readonly Exception _exceptionToThrow;

    public ExceptionThrowingRequestBodyStream(Exception exceptionToThrow)
    {
        _exceptionToThrow = exceptionToThrow;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotImplementedException();

    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw _exceptionToThrow;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }
}

public readonly struct TraceIdentifier
{
    public TraceIdentifier(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public static implicit operator string(TraceIdentifier value) => value.Id;

    public static ValueTask<TraceIdentifier> BindAsync(HttpContext context)
    {
        return ValueTask.FromResult(new TraceIdentifier(context.TraceIdentifier));
    }
}

public class TlsConnectionFeature : ITlsConnectionFeature
{
    public TlsConnectionFeature(X509Certificate2 clientCertificate)
    {
        ClientCertificate = clientCertificate;
    }

    public X509Certificate2 ClientCertificate { get; set; }

    public Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

public class AddsCustomParameterMetadataBindable : IEndpointParameterMetadataProvider, IEndpointMetadataProvider
{
    public static ValueTask<AddsCustomParameterMetadataBindable> BindAsync(HttpContext context, ParameterInfo parameter) => default;

    public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
    {
        builder.Metadata.Add(new ParameterNameMetadata { Name = parameter.Name });
    }

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        builder.Metadata.Add(new CustomEndpointMetadata { Source = MetadataSource.Parameter });
    }
}

public class CustomEndpointMetadata
{
    public string Data { get; init; }

    public MetadataSource Source { get; init; }
}

public enum MetadataSource
{
    Caller,
    Parameter,
    ReturnType,
    Property
}

public class ParameterNameMetadata
{
    public string Name { get; init; }
}

public class AddsCustomParameterMetadata : IEndpointParameterMetadataProvider, IEndpointMetadataProvider
{
    public AddsCustomParameterMetadataAsProperty Data { get; set; }

    public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
    {
        builder.Metadata.Add(new ParameterNameMetadata { Name = parameter.Name });
    }

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        builder.Metadata.Add(new CustomEndpointMetadata { Source = MetadataSource.Parameter });
    }
}

public class AddsCustomParameterMetadataAsProperty : IEndpointParameterMetadataProvider, IEndpointMetadataProvider
{
    public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
    {
        builder.Metadata.Add(new ParameterNameMetadata { Name = parameter.Name });
    }

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        builder.Metadata.Add(new CustomEndpointMetadata { Source = MetadataSource.Property });
    }
}
public class AddsCustomEndpointMetadataResult : IEndpointMetadataProvider, IResult
{
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        builder.Metadata.Add(new CustomEndpointMetadata { Source = MetadataSource.ReturnType });
    }

    public Task ExecuteAsync(HttpContext httpContext) => throw new NotImplementedException();
}

public class AccessesServicesMetadataResult : IResult, IEndpointMetadataProvider
{
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        if (builder.ApplicationServices.GetRequiredService<MetadataService>() is { } metadataService)
        {
            builder.Metadata.Add(metadataService);
        }
    }

    public Task ExecuteAsync(HttpContext httpContext) => Task.CompletedTask;
}

public class RemovesAcceptsParameterMetadata : IEndpointParameterMetadataProvider
{
    public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
    {
        if (builder.Metadata is not null)
        {
            for (int i = builder.Metadata.Count - 1; i >= 0; i--)
            {
                var metadata = builder.Metadata[i];
                if (metadata is IAcceptsMetadata)
                {
                    builder.Metadata.RemoveAt(i);
                }
            }
        }
    }
}

public class RemovesAcceptsMetadataResult : IEndpointMetadataProvider, IResult
{
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        if (builder.Metadata is not null)
        {
            for (int i = builder.Metadata.Count - 1; i >= 0; i--)
            {
                var metadata = builder.Metadata[i];
                if (metadata is IAcceptsMetadata)
                {
                    builder.Metadata.RemoveAt(i);
                }
            }
        }
    }

    public Task ExecuteAsync(HttpContext httpContext) => throw new NotImplementedException();
}

public class AccessesServicesMetadataBinder : IEndpointMetadataProvider
{
    public static ValueTask<AccessesServicesMetadataBinder> BindAsync(HttpContext context, ParameterInfo parameter) =>
        new(new AccessesServicesMetadataBinder());

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        if (builder.ApplicationServices.GetRequiredService<MetadataService>() is { } metadataService)
        {
            builder.Metadata.Add(metadataService);
        }
    }
}

public record MetadataService;
public record ParameterListFromQuery(HttpContext HttpContext,
    [FromQuery] int Value,
    [FromQuery(Name = "customQuery")] int CustomValue,
    [property: FromQuery(Name = "anotherCustomQuery")] int? AnotherCustomValue = null);
public record ParameterListFromRoute(HttpContext HttpContext, int Value);
public record ParameterListFromHeader(HttpContext HttpContext, [FromHeader(Name = "X-Custom-Header")] int Value);

public record ParameterListFromHeaderWithProperties
{
    public HttpContext HttpContext { get; set; }
    [FromHeader(Name = "X-Custom-Header")]
    public int Value { get; set; }
}

public record ParametersListWithImplicitFromBody(HttpContext HttpContext, TodoStruct Todo);
public record struct TodoStruct(int Id, string Name, bool IsComplete, TodoStatus Status) : ITodo;
public record ParametersListWithExplicitFromBody(HttpContext HttpContext, [FromBody] Todo Todo);
public record ParametersListWithHttpContext(
    HttpContext HttpContext,
    ClaimsPrincipal User,
    HttpRequest Request,
    HttpResponse Response);

public record struct ParameterListRecordStruct(HttpContext HttpContext, [FromRoute] int Value);

public record ParameterListRecordClass(HttpContext HttpContext, [FromRoute] int Value);
public record struct ParameterRecordStructWithJsonBodyOrService(TodoStruct Todo, TestService Service);

#nullable enable
public record ParameterListRecordWithoutPositionalParameters
{
    public HttpContext? HttpContext { get; set; }

    [FromRoute]
    public required int Value { get; set; }
}
#nullable restore

public struct ParameterListStruct
{
    public HttpContext HttpContext { get; set; }

    [FromRoute]
    public int Value { get; set; }
}

public struct ParameterListMutableStruct
{
    public ParameterListMutableStruct()
    {
        Value = -1;
        HttpContext = default!;
    }

    public HttpContext HttpContext { get; set; }

    [FromRoute]
    public int Value { get; set; }
}

public class ParameterListStructWithParameterizedContructor
{
    public ParameterListStructWithParameterizedContructor(HttpContext httpContext)
    {
        HttpContext = httpContext;
        Value = 42;
    }

    public HttpContext HttpContext { get; set; }

    public int Value { get; set; }
}

public struct ParameterListStructWithMultipleParameterizedContructor
{
    public ParameterListStructWithMultipleParameterizedContructor(HttpContext httpContext)
    {
        HttpContext = httpContext;
        Value = 10;
    }

    public ParameterListStructWithMultipleParameterizedContructor(HttpContext httpContext, [FromHeader(Name = "Value")] int value)
    {
        HttpContext = httpContext;
        Value = value;
    }

    public HttpContext HttpContext { get; set; }

    [FromRoute]
    public int Value { get; set; }
}

#nullable enable
public class ParameterListClass
{
    public HttpContext? HttpContext { get; set; }

    [FromRoute]
    public int Value { get; set; }
}
#nullable restore

public class ParameterListClassWithParameterizedContructor
{
    public ParameterListClassWithParameterizedContructor(HttpContext httpContext)
    {
        HttpContext = httpContext;
        Value = 42;
    }

    public HttpContext HttpContext { get; set; }

    public int Value { get; set; }
}

public class ParameterListWitDefaultValue
{
    public ParameterListWitDefaultValue(HttpContext httpContext, [FromRoute] int value = 42)
    {
        HttpContext = httpContext;
        Value = value;
    }

    public HttpContext HttpContext { get; }
    public int Value { get; }
}

public class ParameterListWithReadOnlyProperties
{
    public ParameterListWithReadOnlyProperties()
    {
        ReadOnlyValue = 1;
        PrivateSetValue = 2;
    }

    public int Value { get; set; }

    public int ConstantValue => 1;

    public int ReadOnlyValue { get; }

    public int PrivateSetValue { get; private set;  }
}

public record struct SampleParameterList(int Foo);
public record struct AdditionalSampleParameterList(int Bar);

public record ParametersListWithBindAsyncType(
    HttpContext HttpContext,
    InheritBindAsync Value,
    MyBindAsyncRecord MyBindAsyncParam);

public record ParametersListWithMetadataType(
    HttpContext HttpContext,
    AddsCustomParameterMetadataAsProperty Value);

public class ParameterListRequiredStringFromDifferentSources
{
    public HttpContext HttpContext { get; set; }

    [FromRoute]
    public required string RequiredRouteParam { get; set; }

    [FromQuery]
    public required string RequiredQueryParam { get; set; }

    [FromHeader]
    public required string RequiredHeaderParam { get; set; }
}

public record BadArgumentListRecord(int Foo)
{
    public BadArgumentListRecord(int foo, int bar)
        : this(foo)
    {
    }

    public int Bar { get; set; }
}

public class BadNoPublicConstructorArgumentListClass
{
    private BadNoPublicConstructorArgumentListClass()
    { }

    public int Foo { get; set; }
}

public abstract class BadAbstractArgumentListClass
{
    public int Foo { get; set; }
}

public class BadArgumentListClass
{
    public BadArgumentListClass(int foo, string name)
    {
    }

    public int Foo { get; set; }
    public int Bar { get; set; }
}

public class BadArgumentListClassMultipleCtors
{
    public BadArgumentListClassMultipleCtors(int foo)
    {
    }

    public BadArgumentListClassMultipleCtors(int foo, int bar)
    {
    }

    public int Foo { get; set; }
    public int Bar { get; set; }
}

public record NestedArgumentListRecord([AsParameters] object NestedParameterList);

public class ClassWithParametersConstructor
{
    public ClassWithParametersConstructor([AsParameters] object nestedParameterList)
    {
        NestedParameterList = nestedParameterList;
    }

    public object NestedParameterList { get; set; }
}

public class CountsDefaultEndpointMetadataResult : IEndpointMetadataProvider, IResult
{
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        var currentMetadataCount = builder.Metadata.Count;
        builder.Metadata.Add(new MetadataCountMetadata { Count = currentMetadataCount });
    }

    public Task ExecuteAsync(HttpContext httpContext) => Task.CompletedTask;
}

public class MetadataCountMetadata
{
    public int Count { get; init; }
}
public class RoutePatternMetadata
{
    public string RoutePattern { get; init; } = String.Empty;
}

public class AddsRoutePatternMetadata : IEndpointMetadataProvider
{
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        if (builder is not RouteEndpointBuilder reb)
        {
            return;
        }

        builder.Metadata.Add(new RoutePatternMetadata { RoutePattern = reb.RoutePattern?.RawText ?? string.Empty });
    }
}

public class CountsDefaultEndpointMetadataPoco : IEndpointMetadataProvider
{
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        var currentMetadataCount = builder.Metadata.Count;
        builder.Metadata.Add(new MetadataCountMetadata { Count = currentMetadataCount });
    }
}

public class Attribute1 : Attribute
{
}

public class Attribute2 : Attribute
{
}

public class Status410Result : IResult
{
    Task IResult.ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status410Gone;
        httpContext.Response.WriteAsync("Already gone!");
        return Task.CompletedTask;
    }
}

public class TodoJsonConverter : JsonConverter<ITodo>
{
    public override ITodo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var todo = new Todo();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            var property = reader.GetString()!;
            reader.Read();

            switch (property.ToLowerInvariant())
            {
                case "id":
                    todo.Id = reader.GetInt32();
                    break;
                case "name":
                    todo.Name = reader.GetString();
                    break;
                case "iscomplete":
                    todo.IsComplete = reader.GetBoolean();
                    break;
                default:
                    break;
            }
        }

        return todo;
    }

    public override void Write(Utf8JsonWriter writer, ITodo value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

#nullable enable
public class TodoChild : Todo
{
    public string? Child { get; set; }
}
#nullable restore

public class TodoWithExplicitIParsable : IParsable<TodoWithExplicitIParsable>
{
    static TodoWithExplicitIParsable IParsable<TodoWithExplicitIParsable>.Parse(string s, IFormatProvider provider)
    {
        return new TodoWithExplicitIParsable();
    }

    static bool IParsable<TodoWithExplicitIParsable>.TryParse(string s, IFormatProvider provider, out TodoWithExplicitIParsable result)
    {
        result = new TodoWithExplicitIParsable();
        return true;
    }
}

#nullable enable
public class BindableWithMismatchedNullability<T>
{
    public BindableWithMismatchedNullability(T? value)
    {
        Value = value;
    }

    public T? Value { get; }

    public static async ValueTask<BindableWithMismatchedNullability<T?>> BindAsync(HttpContext httpContext, ParameterInfo parameter)
    {
        await Task.CompletedTask;
        return new BindableWithMismatchedNullability<T?>(default);
    }
}

public struct BindableStructWithMismatchedNullability<T>
{
    public BindableStructWithMismatchedNullability(T? value)
    {
        Value = value;
    }

    public T? Value { get; }

    public static async ValueTask<BindableStructWithMismatchedNullability<T?>> BindAsync(HttpContext httpContext, ParameterInfo parameter)
    {
        await Task.CompletedTask;
        return new BindableStructWithMismatchedNullability<T?>(default);
    }
}

public class BindableClassWithNullReturn
{
    public static async ValueTask<BindableClassWithNullReturn?> BindAsync(HttpContext httpContext, ParameterInfo parameter)
    {
        await Task.CompletedTask;
        return null;
    }
}

public struct BindableStructWithNullReturn
{
    public static async ValueTask<BindableStructWithNullReturn?> BindAsync(HttpContext httpContext, ParameterInfo parameter)
    {
        await Task.CompletedTask;
        return null;
    }
}

public struct BindableStruct
{
    public BindableStruct(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static async ValueTask<BindableStruct> BindAsync(HttpContext httpContext, ParameterInfo parameter)
    {
        await Task.CompletedTask;
        return new BindableStruct(httpContext.Request.Query["value"].ToString());
    }
}
