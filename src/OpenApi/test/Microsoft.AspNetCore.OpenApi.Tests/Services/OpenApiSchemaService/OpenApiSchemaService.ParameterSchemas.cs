// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

public partial class OpenApiSchemaServiceTests : OpenApiDocumentServiceTestBase
{
#nullable enable
    public static object?[][] RouteParametersWithPrimitiveTypes =>
    [
        [(int id) => {}, JsonSchemaType.Integer, "int32"],
        [(long id) => {}, JsonSchemaType.Integer, "int64"],
        [(float id) => {}, JsonSchemaType.Number, "float"],
        [(double id) => {}, JsonSchemaType.Number, "double"],
        [(decimal id) => {}, JsonSchemaType.Number, "double"],
        [(bool id) => {}, JsonSchemaType.Boolean, null],
        [(string id) => {}, JsonSchemaType.String, null],
        [(char id) => {}, JsonSchemaType.String, "char"],
        [(byte id) => {}, JsonSchemaType.Integer, "uint8"],
        [(byte[] id) => {}, JsonSchemaType.String, "byte"],
        [(short id) => {}, JsonSchemaType.Integer, "int16"],
        [(ushort id) => {}, JsonSchemaType.Integer, "uint16"],
        [(uint id) => {}, JsonSchemaType.Integer, "uint32"],
        [(ulong id) => {}, JsonSchemaType.Integer, "uint64"],
        [(Uri id) => {}, JsonSchemaType.String, "uri"],
        [(TimeOnly id) => {}, JsonSchemaType.String, "time"],
        [(DateOnly id) => {}, JsonSchemaType.String, "date"],
        [(int? id) => {}, JsonSchemaType.Integer, "int32"],
        [(long? id) => {}, JsonSchemaType.Integer, "int64"],
        [(float? id) => {}, JsonSchemaType.Number, "float"],
        [(double? id) => {}, JsonSchemaType.Number, "double"],
        [(decimal? id) => {}, JsonSchemaType.Number, "double"],
        [(bool? id) => {}, JsonSchemaType.Boolean, null],
        [(string? id) => {}, JsonSchemaType.String, null],
        [(char? id) => {}, JsonSchemaType.String, "char"],
        [(byte? id) => {}, JsonSchemaType.Integer, "uint8"],
        [(byte[]? id) => {}, JsonSchemaType.String, "byte"],
        [(short? id) => {}, JsonSchemaType.Integer, "int16"],
        [(ushort? id) => {}, JsonSchemaType.Integer, "uint16"],
        [(uint? id) => {}, JsonSchemaType.Integer, "uint32"],
        [(ulong? id) => {}, JsonSchemaType.Integer, "uint64"],
        [(Uri? id) => {}, JsonSchemaType.String, "uri"],
        [(TimeOnly? id) => {}, JsonSchemaType.String, "time"],
        [(DateOnly? id) => {}, JsonSchemaType.String, "date"]
    ];
#nullable restore

    [Theory]
    [MemberData(nameof(RouteParametersWithPrimitiveTypes))]
    public async Task GetOpenApiParameters_HandlesRouteParameterWithPrimitiveType(Delegate requestHandler, JsonSchemaType schemaType, string schemaFormat)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/{id}", requestHandler);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/{id}"].Operations[OperationType.Get];
            var parameter = Assert.Single(operation.Parameters);
            Assert.Equal(schemaType, parameter.Schema.Type);
            Assert.Equal(schemaFormat, parameter.Schema.Format);
            Assert.False(parameter.Schema.Nullable);
        });
    }

    public static object[][] RouteParametersWithParsableTypes =>
    [
        [(Guid id) => {}, JsonSchemaType.String, "uuid"],
        [(DateTime id) => {}, JsonSchemaType.String, "date-time"],
        [(DateTimeOffset id) => {}, JsonSchemaType.String, "date-time"],
        [(Uri id) => {}, JsonSchemaType.String, "uri"]
    ];

    [Theory]
    [MemberData(nameof(RouteParametersWithParsableTypes))]
    public async Task GetOpenApiParameters_HandlesRouteParameterWithParsableType(Delegate requestHandler, JsonSchemaType schemaType, string schemaFormat)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/{id}", requestHandler);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/{id}"].Operations[OperationType.Get];
            var parameter = Assert.Single(operation.Parameters);
            Assert.Equal(schemaType, parameter.Schema.Type);
            Assert.Equal(schemaFormat, parameter.Schema.Format);
        });
    }

    [Theory]
    [InlineData("/api/{id:int}", JsonSchemaType.Integer, "int32", null, null, null, null, null)]
    [InlineData("/api/{id:bool}", JsonSchemaType.Boolean, null, null, null, null, null, null)]
    [InlineData("/api/{id:datetime}", JsonSchemaType.String, "date-time", null, null, null, null, null)]
    [InlineData("/api/{id:decimal}", JsonSchemaType.Number, "double", null, null, null, null, null)]
    [InlineData("/api/{id:double}", JsonSchemaType.Number, "double", null, null, null, null, null)]
    [InlineData("/api/{id:float}", JsonSchemaType.Number, "float", null, null, null, null, null)]
    [InlineData("/api/{id:guid}", JsonSchemaType.String, "uuid", null, null, null, null, null)]
    [InlineData("/api/{id:long}", JsonSchemaType.Integer, "int64", null, null, null, null, null)]
    [InlineData("/api/{id:minLength(4)}", JsonSchemaType.Integer, "int32", null, null, 4, null, null)]
    [InlineData("/api/{id:maxLength(8)}", JsonSchemaType.Integer, "int32", null, null, null, 8, null)]
    [InlineData("/api/{id:length(4, 8)}", JsonSchemaType.Integer, "int32", null, null, 4, 8, null)]
    [InlineData("/api/{id:min(4)}", JsonSchemaType.Integer, "int32", 4, null, null, null, null)]
    [InlineData("/api/{id:max(8)}", JsonSchemaType.Integer, "int32", null, 8, null, null, null)]
    [InlineData("/api/{id:range(4, 8)}", JsonSchemaType.Integer, "int32", 4, 8, null, null, null)]
    [InlineData("/api/{id:alpha}", JsonSchemaType.String, null, null, null, null, null, "^[A-Za-z]*$")]
    [InlineData("/api/{id:regex(^\\d{{3}}-\\d{{2}}-\\d{{4}}$)}", JsonSchemaType.String, null, null, null, null, null, "^\\d{3}-\\d{2}-\\d{4}$")]
    // First route constraint wins
    [InlineData("/api/{id:min(2):range(4, 8)}", JsonSchemaType.Integer, "int32", 2, 8, null, null, null)]
    [InlineData("/api/{id::double:float}", JsonSchemaType.Number, "double", null, null, null, null, null)]
    [InlineData("/api/{id::long:int}", JsonSchemaType.Integer, "int64", null, null, null, null, null)]
    // Compatible route constraints are combined
    [InlineData("/api/{id:max(8):min(4)}", JsonSchemaType.Integer, "int32", 4, 8, null, null, null)]
    [InlineData("/api/{id:maxLength(8):minLength(4)}", JsonSchemaType.Integer, "int32", null, null, 4, 8, null)]
    public async Task GetOpenApiParameters_HandlesRouteParameterWithRouteConstraint(string routeTemplate, JsonSchemaType type, string format, int? minimum, int? maximum, int? minLength, int? maxLength, string pattern)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet(routeTemplate, (int id) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var path = Assert.Single(document.Paths);
            var operation = path.Value.Operations[OperationType.Get];
            var parameter = Assert.Single(operation.Parameters);
            Assert.Equal(type, parameter.Schema.Type);
            Assert.Equal(format, parameter.Schema.Format);
            Assert.Equal(minimum, parameter.Schema.Minimum);
            Assert.Equal(maximum, parameter.Schema.Maximum);
            Assert.Equal(minLength, parameter.Schema.MinLength);
            Assert.Equal(maxLength, parameter.Schema.MaxLength);
            Assert.Equal(pattern, parameter.Schema.Pattern);
        });
    }

#nullable enable
    public static object[][] RouteParametersWithDefaultValues =>
    [
        [(int id = 2) => { }, (JsonNode defaultValue) => Assert.Equal(2, defaultValue.GetValue<int>())],
        [(float id = 3f) => { }, (JsonNode defaultValue) => Assert.Equal(3, defaultValue.GetValue<int>())],
        [(string id = "test") => { }, (JsonNode defaultValue) => Assert.Equal("test", defaultValue.GetValue<string>())],
        [(bool id = true) => { }, (JsonNode defaultValue) => Assert.True(defaultValue.GetValue<bool>())],
        [(TaskStatus status = TaskStatus.Canceled) => { }, (JsonNode defaultValue) => Assert.Equal(6, defaultValue.GetValue<int>())],
        // Default value for enums is serialized as string when a converter is registered.
        [(Status status = Status.Pending) => { }, (JsonNode defaultValue) => Assert.Equal("Pending", defaultValue.GetValue<string>())],
        [([DefaultValue(2)] int id) => { }, (JsonNode defaultValue) => Assert.Equal(2, defaultValue.GetValue<int>())],
        [([DefaultValue(3f)] float id) => { }, (JsonNode defaultValue) => Assert.Equal(3, defaultValue.GetValue<int>())],
        [([DefaultValue("test")] string id) => { }, (JsonNode defaultValue) => Assert.Equal("test", defaultValue.GetValue<string>())],
        [([DefaultValue(true)] bool id) => { }, (JsonNode defaultValue) => Assert.True(defaultValue.GetValue<bool>())],
        [([DefaultValue(TaskStatus.Canceled)] TaskStatus status) => { }, (JsonNode defaultValue) => Assert.Equal(6, defaultValue.GetValue<int>())],
        [([DefaultValue(Status.Pending)] Status status) => { }, (JsonNode defaultValue) => Assert.Equal("Pending", defaultValue.GetValue<string>())],
        [([DefaultValue(null)] int? id) => { }, (JsonNode defaultValue) => Assert.True(defaultValue is null)],
        [([DefaultValue(2)] int? id) => { }, (JsonNode defaultValue) => Assert.Equal(2, defaultValue.GetValue<int>())],
        [([DefaultValue(null)] string? id) => { }, (JsonNode defaultValue) => Assert.True(defaultValue is null)],
        [([DefaultValue("foo")] string? id) => { }, (JsonNode defaultValue) => Assert.Equal("foo", defaultValue.GetValue<string>())],
        [([DefaultValue(null)] TaskStatus? status) => { }, (JsonNode defaultValue) => Assert.True(defaultValue is null)],
        [([DefaultValue(TaskStatus.Canceled)] TaskStatus? status) => { }, (JsonNode defaultValue) => Assert.Equal(6, defaultValue.GetValue<int>())],
    ];

    [Theory]
    [MemberData(nameof(RouteParametersWithDefaultValues))]
    public async Task GetOpenApiParameters_HandlesRouteParametersWithDefaultValue(Delegate requestHandler, Action<JsonNode> assert)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api/{id}", requestHandler);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/{id}"].Operations[OperationType.Post];
            var parameter = Assert.Single(operation.Parameters ?? []);
            var openApiDefault = parameter.Schema.Default;
            assert(openApiDefault);
        });
    }
#nullable restore

    [Fact]
    public async Task GetOpenApiParameters_HandlesEnumParameterWithoutConverter()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api", (TaskStatus status) => { });

        // Assert -- that enums without a converter registered are
        // consumed as integer
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Get];
            var parameter = Assert.Single(operation.Parameters);
            Assert.Equal(JsonSchemaType.Integer, parameter.Schema.Type);
            Assert.Empty(parameter.Schema.Enum);
        });
    }

    [Fact]
    public async Task GetOpenApiParameters_HandlesEnumParameterWithConverter()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api", (Status status) => { });

        // Assert -- that enums with a converter registered
        // are serialized with the `enum` value in the schema.
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Get];
            var parameter = Assert.Single(operation.Parameters);
            Assert.Null(parameter.Schema.Type);
            Assert.Collection(parameter.Schema.Enum,
            value =>
            {
                Assert.Equal("Pending", value.GetValue<string>());
            },
            value =>
            {
                Assert.Equal("Approved", value.GetValue<string>());
            },
            value =>
            {
                Assert.Equal("Rejected", value.GetValue<string>());
            });
        });
    }

    [Fact]
    public async Task GetOpenApiParameters_HandlesRouteParameterFromAsParameters()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/{id}/{date}", ([AsParameters] RouteParamsContainer routeParams) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/{id}/{date}"].Operations[OperationType.Get];
            Assert.Collection(operation.Parameters, parameter =>
            {
                Assert.Equal("id", parameter.Name);
                Assert.Equal(JsonSchemaType.String, parameter.Schema.Type);
                Assert.Equal("uuid", parameter.Schema.Format);
            },
            parameter =>
            {
                Assert.Equal("date", parameter.Name);
                Assert.Equal(JsonSchemaType.String, parameter.Schema.Type);
                Assert.Equal("date-time", parameter.Schema.Format);
            });
        });
    }

    [Fact]
    public async Task GetOpenApiParameters_HandlesRouteParametersWithMvcModelBinding()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsParametersInModel));

        // Assert
        await VerifyOpenApiDocument(action, document =>
        {
            var operation = document.Paths["/api/{id}/{date}"].Operations[OperationType.Get];
            Assert.Collection(operation.Parameters, parameter =>
            {
                Assert.Equal("Id", parameter.Name);
                Assert.Equal(JsonSchemaType.String, parameter.Schema.Type);
                Assert.Equal("uuid", parameter.Schema.Format);
            },
            parameter =>
            {
                Assert.Equal("Date", parameter.Name);
                Assert.Equal(JsonSchemaType.String, parameter.Schema.Type);
                Assert.Equal("date-time", parameter.Schema.Format);
            });
        });
    }

    [Fact]
    public async Task GetOpenApiParameters_HandlesRouteParametersWithValidationsInMvcModelBinding()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsValidatableParametersInModel));

        // Assert
        await VerifyOpenApiDocument(action, document =>
        {
            var operation = document.Paths["/api/{id}/{name}"].Operations[OperationType.Get];
            Assert.Collection(operation.Parameters, parameter =>
            {
                Assert.Equal("Id", parameter.Name);
                Assert.Equal(JsonSchemaType.String, parameter.Schema.Type);
                Assert.Equal("uuid", parameter.Schema.Format);
            },
            parameter =>
            {
                Assert.Equal("Name", parameter.Name);
                Assert.Equal(JsonSchemaType.String, parameter.Schema.Type);
                Assert.Equal(5, parameter.Schema.MaxLength);
            });
        });
    }

    public static object[][] RouteParametersWithValidationAttributes =>
    [
        [([MaxLength(5)] string id) => {}, (OpenApiSchema schema) => Assert.Equal(5, schema.MaxLength)],
        [([MinLength(2)] string id) => {}, (OpenApiSchema schema) => Assert.Equal(2, schema.MinLength)],
        [([MaxLength(5)] int[] ids) => {}, (OpenApiSchema schema) => Assert.Equal(5, schema.MaxItems)],
        [([MinLength(2)] int[] id) => {}, (OpenApiSchema schema) => Assert.Equal(2, schema.MinItems)],
        [([Length(4, 8)] int[] id) => {}, (OpenApiSchema schema) => { Assert.Equal(4, schema.MinItems); Assert.Equal(8, schema.MaxItems); }],
        [([Range(4, 8)]int id) => {}, (OpenApiSchema schema) => { Assert.Equal(4, schema.Minimum); Assert.Equal(8, schema.Maximum); }],
        [([Range(typeof(DateTime), "2024-02-01", "2024-02-031")] DateTime id) => {}, (OpenApiSchema schema) => { Assert.Null(schema.Minimum); Assert.Null(schema.Maximum); }],
        [([StringLength(10)] string name) => {}, (OpenApiSchema schema) => { Assert.Equal(10, schema.MaxLength); Assert.Equal(0, schema.MinLength); }],
        [([StringLength(10, MinimumLength = 5)] string name) => {}, (OpenApiSchema schema) => { Assert.Equal(10, schema.MaxLength); Assert.Equal(5, schema.MinLength); }],
        [([Url] string url) => {}, (OpenApiSchema schema) => { Assert.Equal(JsonSchemaType.String, schema.Type); Assert.Equal("uri", schema.Format); }],
        // Check that multiple attributes get applied correctly
        [([Url][StringLength(10)] string url) => {}, (OpenApiSchema schema) => { Assert.Equal(JsonSchemaType.String, schema.Type); Assert.Equal("uri", schema.Format); Assert.Equal(10, schema.MaxLength); }],
        [([Base64String] string base64string) => {}, (OpenApiSchema schema) => { Assert.Equal(JsonSchemaType.String, schema.Type); Assert.Equal("byte", schema.Format); }],
    ];

    [Theory]
    [MemberData(nameof(RouteParametersWithValidationAttributes))]
    public async Task GetOpenApiParameters_HandlesRouteParameterWithValidationAttributes(Delegate requestHandler, Action<OpenApiSchema> verifySchema)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/{id}", requestHandler);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/{id}"].Operations[OperationType.Get];
            var parameter = Assert.Single(operation.Parameters);
            verifySchema(parameter.Schema);
        });
    }

    public static object[][] RouteParametersWithRangeAttributes =>
    [
        [([Range(4, 8)] int id) => {}, (OpenApiSchema schema) => { Assert.Equal(4, schema.Minimum); Assert.Equal(8, schema.Maximum); }],
        [([Range(int.MinValue, int.MaxValue)] int id) => {}, (OpenApiSchema schema) => { Assert.Equal(int.MinValue, schema.Minimum); Assert.Equal(int.MaxValue, schema.Maximum); }],
        [([Range(0, double.MaxValue)] double id) => {}, (OpenApiSchema schema) => { Assert.Equal(0, schema.Minimum); Assert.Null(schema.Maximum); }],
        [([Range(typeof(double), "0", "1.79769313486232E+308")] double id) => {}, (OpenApiSchema schema) => { Assert.Equal(0, schema.Minimum); Assert.Null(schema.Maximum); }],
        [([Range(typeof(long), "-9223372036854775808", "9223372036854775807")] long id) => {}, (OpenApiSchema schema) => { Assert.Equal(long.MinValue, schema.Minimum); Assert.Equal(long.MaxValue, schema.Maximum); }],
        [([Range(typeof(DateTime), "2024-02-01", "2024-02-031")] DateTime id) => {}, (OpenApiSchema schema) => { Assert.Null(schema.Minimum); Assert.Null(schema.Maximum); }],
    ];

    [Theory]
    [MemberData(nameof(RouteParametersWithRangeAttributes))]
    public async Task GetOpenApiParameters_HandlesRouteParametersWithRangeAttributes(Delegate requestHandler, Action<OpenApiSchema> verifySchema)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/{id}", requestHandler);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/{id}"].Operations[OperationType.Get];
            var parameter = Assert.Single(operation.Parameters);
            verifySchema(parameter.Schema);
        });
    }

    public static object[][] RouteParametersWithRangeAttributes_CultureInfo =>
    [
        [([Range(typeof(DateTime), "2024-02-01", "2024-02-031")] DateTime id) => {}, (OpenApiSchema schema) => { Assert.Null(schema.Minimum); Assert.Null(schema.Maximum); }],
        [([Range(typeof(decimal), "1,99", "3,99")] decimal id) => {}, (OpenApiSchema schema) => { Assert.Equal(1.99m, schema.Minimum); Assert.Equal(3.99m, schema.Maximum); }],
        [([Range(typeof(decimal), "1,99", "3,99", ParseLimitsInInvariantCulture = true)] decimal id) => {}, (OpenApiSchema schema) => { Assert.Equal(199, schema.Minimum); Assert.Equal(399, schema.Maximum); }],
        [([Range(1000, 2000)] int id) => {}, (OpenApiSchema schema) => { Assert.Equal(1000, schema.Minimum); Assert.Equal(2000, schema.Maximum); }]
    ];

    [Theory]
    [MemberData(nameof(RouteParametersWithRangeAttributes_CultureInfo))]
    [UseCulture("fr-FR")]
    public async Task GetOpenApiParameters_HandlesRouteParametersWithRangeAttributes_CultureInfo(Delegate requestHandler, Action<OpenApiSchema> verifySchema)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/{id}", requestHandler);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/{id}"].Operations[OperationType.Get];
            var parameter = Assert.Single(operation.Parameters);
            verifySchema(parameter.Schema);
        });
    }

    [Fact]
    public async Task GetOpenApiParameters_HandlesParametersWithRequiredAttribute()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act -- route parameters are always required so we test other
        // parameter sources here.
        builder.MapGet("/api-1", ([Required] string id) => { });
        builder.MapGet("/api-2", ([Required] int? age) => { });
        builder.MapGet("/api-3", ([Required] Guid guid) => { });
        builder.MapGet("/api-4", ([Required][FromHeader] DateTime date) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            foreach (var path in document.Paths.Values)
            {
                var operation = path.Operations[OperationType.Get];
                var parameter = Assert.Single(operation.Parameters);
                Assert.True(parameter.Required);
            }
        });
    }

    public static object[][] ArrayBasedQueryParameters =>
    [
        [(int[] id) => { }, JsonSchemaType.Integer, false],
        [(int?[] id) => { }, JsonSchemaType.Integer, true],
        [(Guid[] id) => { }, JsonSchemaType.String, false],
        [(Guid?[] id) => { }, JsonSchemaType.String, true],
        [(DateTime[] id) => { }, JsonSchemaType.String, false],
        [(DateTime?[] id) => { }, JsonSchemaType.String, true],
        [(DateTimeOffset[] id) => { }, JsonSchemaType.String, false],
        [(DateTimeOffset?[] id) => { }, JsonSchemaType.String, true],
        [(Uri[] id) => { }, JsonSchemaType.String, false],
    ];

    [Theory]
    [MemberData(nameof(ArrayBasedQueryParameters))]
    public async Task GetOpenApiParameters_HandlesArrayBasedTypes(Delegate requestHandler, JsonSchemaType innerSchemaType, bool isNullable)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/", requestHandler);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Get];
            var parameter = Assert.Single(operation.Parameters);
            // Array items can be serialized to nullable values when the element
            // type is nullable. For example, array-of-ints?ints=1&ints=2&ints=&ints=4
            // will produce [1, 2, null, 4] when the parameter is int?[] ints.
            // When the element type is not nullable (int[] ints), the binding
            // will produce [1, 2, 0, 4]
            Assert.Equal(JsonSchemaType.Array, parameter.Schema.Type);
            Assert.Equal(JsonSchemaType.Array, parameter.Schema.Type);
            Assert.Equal(innerSchemaType, parameter.Schema.Items.Type);
            Assert.Equal(isNullable, parameter.Schema.Items.Nullable);
        });
    }

    [Fact]
    public async Task GetOpenApiParameters_HandlesParametersWithDescriptionAttribute()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api", ([Description("The ID of the entity")] int id) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Get];
            var parameter = Assert.Single(operation.Parameters);
            Assert.Equal("The ID of the entity", parameter.Description);
        });
    }

    [Route("/api/{id}/{date}")]
    private void AcceptsParametersInModel(RouteParamsContainer model) { }

    [Route("/api/{id}/{name}")]
    private void AcceptsValidatableParametersInModel(RouteParamsWithValidationsContainer model) { }

    private class RouteParamsContainer
    {
        [FromRoute]
        public Guid Id { get; set; }

        [FromRoute]
        public DateTime Date { get; set; }
    }

    private class RouteParamsWithValidationsContainer
    {
        [FromRoute]
        public Guid Id { get; set; }

        [FromRoute]
        [MaxLength(5)]
        public string Name { get; set; }
    }

    [Fact]
    public async Task SupportsParametersWithTypeConverter()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new CustomTypeConverter());
        });
        var builder = CreateBuilder(serviceCollection);

        // Act
        builder.MapPost("/api", (CustomType id) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            Assert.NotNull(operation.RequestBody.Content["application/json"]);
            Assert.NotNull(operation.RequestBody.Content["application/json"].Schema);
            // Type is null, it's up to the user to configure this via a custom schema
            // transformer for types with a converter.
            Assert.Null(operation.RequestBody.Content["application/json"].Schema.Type);
        });
    }

    public struct CustomType { }

    public class CustomTypeConverter : JsonConverter<CustomType>
    {
        public override CustomType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new CustomType();
        }

        public override void Write(Utf8JsonWriter writer, CustomType value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public async Task SupportsParameterWithDynamicType()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (dynamic id) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            Assert.NotNull(operation.RequestBody.Content);
            Assert.NotNull(operation.RequestBody.Content["application/json"]);
            Assert.NotNull(operation.RequestBody.Content["application/json"].Schema);
            // Type is null, it's up to the user to configure this via a custom schema
            // transformer for types with a converter.
            Assert.Null(operation.RequestBody.Content["application/json"].Schema.Type);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SupportsParameterWithEnumType(bool useAction)
    {
        // Arrange
        if (!useAction)
        {
            var builder = CreateBuilder();
            builder.MapGet("/api/with-enum", (Status status) => status);
            await VerifyOpenApiDocument(builder, AssertOpenApiDocument);
        }
        else
        {
            var action = CreateActionDescriptor(nameof(GetItemStatus));
            await VerifyOpenApiDocument(action, AssertOpenApiDocument);
        }

        static void AssertOpenApiDocument(OpenApiDocument document)
        {
            var operation = document.Paths["/api/with-enum"].Operations[OperationType.Get];
            var parameter = Assert.Single(operation.Parameters);
            var response = Assert.Single(operation.Responses).Value.Content["application/json"].Schema;
            Assert.NotNull(parameter.Schema.Reference);
            Assert.Equal(parameter.Schema.Reference.Id, response.Reference.Id);
            var schema = parameter.Schema;
            Assert.Collection(schema.Enum,
            value =>
            {
                Assert.Equal("Pending", value.GetValue<string>());
            },
            value =>
            {
                Assert.Equal("Approved", value.GetValue<string>());
            },
            value =>
            {
                Assert.Equal("Rejected", value.GetValue<string>());
            });
        }
    }

    [Route("/api/with-enum")]
    private Status GetItemStatus([FromQuery] Status status) => status;

    [Fact]
    public async Task SupportsMvcActionWithAmbientRouteParameter()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AmbientRouteParameter));

        // Assert
        await VerifyOpenApiDocument(action, document =>
        {
            var operation = document.Paths["/api/with-ambient-route-param/{versionId}"].Operations[OperationType.Get];
            var parameter = Assert.Single(operation.Parameters);
            Assert.Equal(JsonSchemaType.String, parameter.Schema.Type);
        });
    }

    [Route("/api/with-ambient-route-param/{versionId}")]
    private void AmbientRouteParameter() { }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SupportsRouteParameterWithCustomTryParse(bool useAction)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        if (!useAction)
        {
            builder.MapGet("/api/{student}", (Student student) => student);
            await VerifyOpenApiDocument(builder, AssertOpenApiDocument);
        }
        else
        {
            var action = CreateActionDescriptor(nameof(GetStudent));
            await VerifyOpenApiDocument(action, AssertOpenApiDocument);
        }

        // Assert
        static void AssertOpenApiDocument(OpenApiDocument document)
        {
            // Parameter is a plain-old string when it comes from the route or query
            var operation = document.Paths["/api/{student}"].Operations[OperationType.Get];
            var parameter = Assert.Single(operation.Parameters);
            Assert.Equal(JsonSchemaType.String, parameter.Schema.Type);

            // Type is fully serialized in the response
            var response = Assert.Single(operation.Responses).Value;
            Assert.True(response.Content.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema;
            Assert.Equal(JsonSchemaType.Object, schema.Type);
            Assert.Collection(schema.Properties, property =>
            {
                Assert.Equal("name", property.Key);
                Assert.Equal(JsonSchemaType.String, property.Value.Type);
            });
        }
    }

    [Route("/api/{student}")]
    private Student GetStudent(Student student) => student;

    public record Student(string Name)
    {
        public static bool TryParse(string value, out Student result)
        {
            if (value is null)
            {
                result = null;
                return false;
            }

            result = new Student(value);
            return true;
        }
    }
}
