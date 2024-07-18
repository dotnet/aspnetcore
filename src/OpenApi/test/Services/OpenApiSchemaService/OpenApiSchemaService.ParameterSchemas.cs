// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

public partial class OpenApiSchemaServiceTests : OpenApiDocumentServiceTestBase
{
#nullable enable
    public static object?[][] RouteParametersWithPrimitiveTypes =>
    [
        [(int id) => {}, "integer", "int32", false],
        [(long id) => {}, "integer", "int64", false],
        [(float id) => {}, "number", "float", false],
        [(double id) => {}, "number", "double", false],
        [(decimal id) => {}, "number", "double", false],
        [(bool id) => {}, "boolean", null, false],
        [(string id) => {}, "string", null, false],
        [(char id) => {}, "string", "char", false],
        [(byte id) => {}, "integer", "uint8", false],
        [(byte[] id) => {}, "string", "byte", false],
        [(short id) => {}, "integer", "int16", false],
        [(ushort id) => {}, "integer", "uint16", false],
        [(uint id) => {}, "integer", "uint32", false],
        [(ulong id) => {}, "integer", "uint64", false],
        [(Uri id) => {}, "string", "uri", false],
        [(TimeOnly id) => {}, "string", "time", false],
        [(DateOnly id) => {}, "string", "date", false],
        [(int? id) => {}, "integer", "int32", true],
        [(long? id) => {}, "integer", "int64", true],
        [(float? id) => {}, "number", "float", true],
        [(double? id) => {}, "number", "double", true],
        [(decimal? id) => {}, "number", "double", true],
        [(bool? id) => {}, "boolean", null, true],
        [(string? id) => {}, "string", null, true],
        [(char? id) => {}, "string", "char", true],
        [(byte? id) => {}, "integer", "uint8", true],
        [(byte[]? id) => {}, "string", "byte", true],
        [(short? id) => {}, "integer", "int16", true],
        [(ushort? id) => {}, "integer", "uint16", true],
        [(uint? id) => {}, "integer", "uint32", true],
        [(ulong? id) => {}, "integer", "uint64", true],
        [(Uri? id) => {}, "string", "uri", true],
        [(TimeOnly? id) => {}, "string", "time", true],
        [(DateOnly? id) => {}, "string", "date", true]
    ];
#nullable restore

    [Theory]
    [MemberData(nameof(RouteParametersWithPrimitiveTypes))]
    public async Task GetOpenApiParameters_HandlesRouteParameterWithPrimitiveType(Delegate requestHandler, string schemaType, string schemaFormat, bool isNullable)
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
            Assert.Equal(isNullable, parameter.Schema.Nullable);
        });
    }

    public static object[][] RouteParametersWithParsableTypes =>
    [
        [(Guid id) => {}, "string", "uuid"],
        [(DateTime id) => {}, "string", "date-time"],
        [(DateTimeOffset id) => {}, "string", "date-time"],
        [(Uri id) => {}, "string", "uri"]
    ];

    [Theory]
    [MemberData(nameof(RouteParametersWithParsableTypes))]
    public async Task GetOpenApiParameters_HandlesRouteParameterWithParsableType(Delegate requestHandler, string schemaType, string schemaFormat)
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
    [InlineData("/api/{id:int}", "integer", "int32", null, null, null, null, null)]
    [InlineData("/api/{id:bool}", "boolean", null, null, null, null, null, null)]
    [InlineData("/api/{id:datetime}", "string", "date-time", null, null, null, null, null)]
    [InlineData("/api/{id:decimal}", "number", "double", null, null, null, null, null)]
    [InlineData("/api/{id:double}", "number", "double", null, null, null, null, null)]
    [InlineData("/api/{id:float}", "number", "float", null, null, null, null, null)]
    [InlineData("/api/{id:guid}", "string", "uuid", null, null, null, null, null)]
    [InlineData("/api/{id:long}", "integer", "int64", null, null, null, null, null)]
    [InlineData("/api/{id:minLength(4)}", "integer", "int32", null, null, 4, null, null)]
    [InlineData("/api/{id:maxLength(8)}", "integer", "int32", null, null, null, 8, null)]
    [InlineData("/api/{id:length(4, 8)}", "integer", "int32", null, null, 4, 8, null)]
    [InlineData("/api/{id:min(4)}", "integer", "int32", 4, null, null, null, null)]
    [InlineData("/api/{id:max(8)}", "integer", "int32", null, 8, null, null, null)]
    [InlineData("/api/{id:range(4, 8)}", "integer", "int32", 4, 8, null, null, null)]
    [InlineData("/api/{id:alpha}", "string", null, null, null, null, null, "^[A-Za-z]*$")]
    [InlineData("/api/{id:regex(^\\d{{3}}-\\d{{2}}-\\d{{4}}$)}", "string", null, null, null, null, null, "^\\d{3}-\\d{2}-\\d{4}$")]
    // First route constraint wins
    [InlineData("/api/{id:min(2):range(4, 8)}", "integer", "int32", 2, 8, null, null, null)]
    [InlineData("/api/{id::double:float}", "number", "double", null, null, null, null, null)]
    [InlineData("/api/{id::long:int}", "integer", "int64", null, null, null, null, null)]
    // Compatible route constraints are combined
    [InlineData("/api/{id:max(8):min(4)}", "integer", "int32", 4, 8, null, null, null)]
    [InlineData("/api/{id:maxLength(8):minLength(4)}", "integer", "int32", null, null, 4, 8, null)]
    public async Task GetOpenApiParameters_HandlesRouteParameterWithRouteConstraint(string routeTemplate, string type, string format, int? minimum, int? maximum, int? minLength, int? maxLength, string pattern)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet(routeTemplate, (int id) => {});

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
        [(int id = 2) => { }, (IOpenApiAny defaultValue) => Assert.Equal(2, ((OpenApiInteger)defaultValue).Value)],
        [(float id = 3f) => { }, (IOpenApiAny defaultValue) => Assert.Equal(3, ((OpenApiInteger)defaultValue).Value)],
        [(string id = "test") => { }, (IOpenApiAny defaultValue) => Assert.Equal("test", ((OpenApiString)defaultValue).Value)],
        [(bool id = true) => { }, (IOpenApiAny defaultValue) => Assert.True(((OpenApiBoolean)defaultValue).Value)],
        [(TaskStatus status = TaskStatus.Canceled) => { }, (IOpenApiAny defaultValue) => Assert.Equal(6, ((OpenApiInteger)defaultValue).Value)],
        // Default value for enums is serialized as string when a converter is registered.
        [(Status status = Status.Pending) => { }, (IOpenApiAny defaultValue) => Assert.Equal("Pending", ((OpenApiString)defaultValue).Value)],
        [([DefaultValue(2)] int id) => { }, (IOpenApiAny defaultValue) => Assert.Equal(2, ((OpenApiInteger)defaultValue).Value)],
        [([DefaultValue(3f)] float id) => { }, (IOpenApiAny defaultValue) => Assert.Equal(3, ((OpenApiInteger)defaultValue).Value)],
        [([DefaultValue("test")] string id) => { }, (IOpenApiAny defaultValue) => Assert.Equal("test", ((OpenApiString)defaultValue).Value)],
        [([DefaultValue(true)] bool id) => { }, (IOpenApiAny defaultValue) => Assert.True(((OpenApiBoolean)defaultValue).Value)],
        [([DefaultValue(TaskStatus.Canceled)] TaskStatus status) => { }, (IOpenApiAny defaultValue) => Assert.Equal(6, ((OpenApiInteger)defaultValue).Value)],
        [([DefaultValue(Status.Pending)] Status status) => { }, (IOpenApiAny defaultValue) => Assert.Equal("Pending", ((OpenApiString)defaultValue).Value)],
        [([DefaultValue(null)] int? id) => { }, (IOpenApiAny defaultValue) => Assert.True(defaultValue is OpenApiNull)],
        [([DefaultValue(2)] int? id) => { }, (IOpenApiAny defaultValue) => Assert.Equal(2, ((OpenApiInteger)defaultValue).Value)],
        [([DefaultValue(null)] string? id) => { }, (IOpenApiAny defaultValue) => Assert.True(defaultValue is OpenApiNull)],
        [([DefaultValue("foo")] string? id) => { }, (IOpenApiAny defaultValue) => Assert.Equal("foo", ((OpenApiString)defaultValue).Value)],
        [([DefaultValue(null)] TaskStatus? status) => { }, (IOpenApiAny defaultValue) => Assert.True(defaultValue is OpenApiNull)],
        [([DefaultValue(TaskStatus.Canceled)] TaskStatus? status) => { }, (IOpenApiAny defaultValue) => Assert.Equal(6, ((OpenApiInteger)defaultValue).Value)],
    ];

    [Theory]
    [MemberData(nameof(RouteParametersWithDefaultValues))]
    public async Task GetOpenApiParameters_HandlesRouteParametersWithDefaultValue(Delegate requestHandler, Action<IOpenApiAny> assert)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api/{id}", requestHandler);

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/{id}"].Operations[OperationType.Post];
            var parameter = Assert.Single(operation.Parameters);
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
            Assert.Equal("integer", parameter.Schema.Type);
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
                var openApiString = Assert.IsType<OpenApiString>(value);
                Assert.Equal("Pending", openApiString.Value);
            },
            value =>
            {
                var openApiString = Assert.IsType<OpenApiString>(value);
                Assert.Equal("Approved", openApiString.Value);
            },
            value =>
            {
                var openApiString = Assert.IsType<OpenApiString>(value);
                Assert.Equal("Rejected", openApiString.Value);
            });
        });
    }

    [Fact]
    public async Task GetOpenApiParameters_HandlesRouteParameterFromAsParameters()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapGet("/api/{id}/{date}", ([AsParameters] RouteParamsContainer routeParams) => {});

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api/{id}/{date}"].Operations[OperationType.Get];
            Assert.Collection(operation.Parameters, parameter =>
            {
                Assert.Equal("id", parameter.Name);
                Assert.Equal("string", parameter.Schema.Type);
                Assert.Equal("uuid", parameter.Schema.Format);
            },
            parameter =>
            {
                Assert.Equal("date", parameter.Name);
                Assert.Equal("string", parameter.Schema.Type);
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
                Assert.Equal("string", parameter.Schema.Type);
                Assert.Equal("uuid", parameter.Schema.Format);
            },
            parameter =>
            {
                Assert.Equal("Date", parameter.Name);
                Assert.Equal("string", parameter.Schema.Type);
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
                Assert.Equal("string", parameter.Schema.Type);
                Assert.Equal("uuid", parameter.Schema.Format);
            },
            parameter =>
            {
                Assert.Equal("Name", parameter.Name);
                Assert.Equal("string", parameter.Schema.Type);
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
        [([StringLength(10)] string name) => {}, (OpenApiSchema schema) => { Assert.Equal(10, schema.MaxLength); Assert.Equal(0, schema.MinLength); }],
        [([StringLength(10, MinimumLength = 5)] string name) => {}, (OpenApiSchema schema) => { Assert.Equal(10, schema.MaxLength); Assert.Equal(5, schema.MinLength); }],
        [([Url] string url) => {}, (OpenApiSchema schema) => { Assert.Equal("string", schema.Type); Assert.Equal("uri", schema.Format); }],
        // Check that multiple attributes get applied correctly
        [([Url][StringLength(10)] string url) => {}, (OpenApiSchema schema) => { Assert.Equal("string", schema.Type); Assert.Equal("uri", schema.Format); Assert.Equal(10, schema.MaxLength); }],
        [([Base64String] string base64string) => {}, (OpenApiSchema schema) => { Assert.Equal("string", schema.Type); Assert.Equal("byte", schema.Format); }],
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
        [(int[] id) => { }, "integer", false],
        [(int?[] id) => { }, "integer", true],
        [(Guid[] id) => { }, "string", false],
        [(Guid?[] id) => { }, "string", true],
        [(DateTime[] id) => { }, "string", false],
        [(DateTime?[] id) => { }, "string", true],
        [(DateTimeOffset[] id) => { }, "string", false],
        [(DateTimeOffset?[] id) => { }, "string", true],
        [(Uri[] id) => { }, "string", false],
    ];

    [Theory]
    [MemberData(nameof(ArrayBasedQueryParameters))]
    public async Task GetOpenApiParameters_HandlesArrayBasedTypes(Delegate requestHandler, string innerSchemaType, bool isNullable)
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
            Assert.Equal("array", parameter.Schema.Type);
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
}
