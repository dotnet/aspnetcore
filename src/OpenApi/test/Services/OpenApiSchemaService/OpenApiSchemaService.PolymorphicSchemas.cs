// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

public partial class OpenApiSchemaServiceTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task HandlesPolymorphicTypeWithMappingsAndStringDiscriminator()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (Shape shape) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            var requestBody = operation.RequestBody.Content;
            Assert.True(requestBody.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema.GetEffective(document);
            // Assert discriminator mappings have been configured correctly
            Assert.Equal("$type", schema.Discriminator.PropertyName);
            Assert.Contains(schema.Discriminator.PropertyName, schema.Required);
            Assert.Collection(schema.Discriminator.Mapping,
                item => Assert.Equal("triangle", item.Key),
                item => Assert.Equal("square", item.Key)
            );
            Assert.Collection(schema.Discriminator.Mapping,
                item => Assert.Equal("#/components/schemas/ShapeTriangle", item.Value),
                item => Assert.Equal("#/components/schemas/ShapeSquare", item.Value)
            );
            // Assert the schemas with the discriminator have been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("ShapeTriangle", out var triangleSchema));
            Assert.Contains(schema.Discriminator.PropertyName, triangleSchema.Properties.Keys);
            Assert.Equal("triangle", ((OpenApiString)triangleSchema.Properties[schema.Discriminator.PropertyName].Enum.First()).Value);
            Assert.True(document.Components.Schemas.TryGetValue("ShapeSquare", out var squareSchema));
            Assert.Equal("square", ((OpenApiString)squareSchema.Properties[schema.Discriminator.PropertyName].Enum.First()).Value);
        });
    }

    [Fact]
    public async Task HandlesPolymorphicTypeWithMappingsAndIntegerDiscriminator()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (WeatherForecastBase forecast) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            var requestBody = operation.RequestBody.Content;
            Assert.True(requestBody.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema.GetEffective(document);
            // Assert discriminator mappings have been configured correctly
            Assert.Equal("$type", schema.Discriminator.PropertyName);
            Assert.Contains(schema.Discriminator.PropertyName, schema.Required);
            Assert.Collection(schema.Discriminator.Mapping,
                item => Assert.Equal("0", item.Key),
                item => Assert.Equal("1", item.Key),
                item => Assert.Equal("2", item.Key)
            );
            Assert.Collection(schema.Discriminator.Mapping,
                item => Assert.Equal("#/components/schemas/WeatherForecastBaseWeatherForecastWithCity", item.Value),
                item => Assert.Equal("#/components/schemas/WeatherForecastBaseWeatherForecastWithTimeSeries", item.Value),
                item => Assert.Equal("#/components/schemas/WeatherForecastBaseWeatherForecastWithLocalNews", item.Value)
            );
            // Assert schema with discriminator = 0 has been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("WeatherForecastBaseWeatherForecastWithCity", out var citySchema));
            Assert.Contains(schema.Discriminator.PropertyName, citySchema.Properties.Keys);
            Assert.Equal(0, ((OpenApiInteger)citySchema.Properties[schema.Discriminator.PropertyName].Enum.First()).Value);
            // Assert schema with discriminator = 1 has been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("WeatherForecastBaseWeatherForecastWithTimeSeries", out var timeSeriesSchema));
            Assert.Contains(schema.Discriminator.PropertyName, timeSeriesSchema.Properties.Keys);
            Assert.Equal(1, ((OpenApiInteger)timeSeriesSchema.Properties[schema.Discriminator.PropertyName].Enum.First()).Value);
            // Assert schema with discriminator = 2 has been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("WeatherForecastBaseWeatherForecastWithLocalNews", out var newsSchema));
            Assert.Contains(schema.Discriminator.PropertyName, newsSchema.Properties.Keys);
            Assert.Equal(2, ((OpenApiInteger)newsSchema.Properties[schema.Discriminator.PropertyName].Enum.First()).Value);
        });
    }

    [Fact]
    public async Task HandlesPolymorphicTypesWithCustomPropertyName()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (Person person) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            var requestBody = operation.RequestBody.Content;
            Assert.True(requestBody.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema.GetEffective(document);
            // Assert discriminator mappings have been configured correctly
            Assert.Equal("discriminator", schema.Discriminator.PropertyName);
            Assert.Contains(schema.Discriminator.PropertyName, schema.Required);
            Assert.Collection(schema.Discriminator.Mapping,
                item => Assert.Equal("student", item.Key),
                item => Assert.Equal("teacher", item.Key)
            );
            Assert.Collection(schema.Discriminator.Mapping,
                item => Assert.Equal("#/components/schemas/PersonStudent", item.Value),
                item => Assert.Equal("#/components/schemas/PersonTeacher", item.Value)
            );
            // Assert schema with discriminator = 0 has been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("PersonStudent", out var citySchema));
            Assert.Contains(schema.Discriminator.PropertyName, citySchema.Properties.Keys);
            Assert.Equal("student", ((OpenApiString)citySchema.Properties[schema.Discriminator.PropertyName].Enum.First()).Value);
            // Assert schema with discriminator = 1 has been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("PersonTeacher", out var timeSeriesSchema));
            Assert.Contains(schema.Discriminator.PropertyName, timeSeriesSchema.Properties.Keys);
            Assert.Equal("teacher", ((OpenApiString)timeSeriesSchema.Properties[schema.Discriminator.PropertyName].Enum.First()).Value);
        });
    }

    [Fact]
    public async Task HandlesPolymorphicTypesWithNonAbstractBaseClass()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (Color color) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            var requestBody = operation.RequestBody.Content;
            Assert.True(requestBody.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema.GetEffective(document);
            // Assert discriminator mappings have been configured correctly
            Assert.Equal("$type", schema.Discriminator.PropertyName);
            Assert.Collection(schema.Discriminator.Mapping,
                item => Assert.Equal("paint", item.Key),
                item => Assert.Equal("fabric", item.Key)
            );
            Assert.Collection(schema.Discriminator.Mapping,
                item => Assert.Equal("#/components/schemas/ColorPaintColor", item.Value),
                item => Assert.Equal("#/components/schemas/ColorFabricColor", item.Value)
            );
            // Note that our implementation diverges from the OpenAPI specification here. OpenAPI
            // requires that derived types in a polymorphic schema _always_ have a discriminator
            // property associated with them. STJ permits the discriminator to be omitted from the
            // if the base type is a non-abstract class and falls back to serializing to this base
            // type. This is a known limitation of the current implementation.
            Assert.Collection(schema.AnyOf,
                schema => Assert.Equal("ColorPaintColor", schema.Reference.Id),
                schema => Assert.Equal("ColorFabricColor", schema.Reference.Id),
                schema => Assert.Equal("ColorColor", schema.Reference.Id));
            // Assert schema with discriminator = "paint" has been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("ColorPaintColor", out var paintSchema));
            Assert.Contains(schema.Discriminator.PropertyName, paintSchema.Properties.Keys);
            Assert.Equal("paint", ((OpenApiString)paintSchema.Properties[schema.Discriminator.PropertyName].Enum.First()).Value);
            // Assert schema with discriminator = "fabric" has been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("ColorFabricColor", out var fabricSchema));
            Assert.Contains(schema.Discriminator.PropertyName, fabricSchema.Properties.Keys);
            Assert.Equal("fabric", ((OpenApiString)fabricSchema.Properties[schema.Discriminator.PropertyName].Enum.First()).Value);
        });
    }
}
