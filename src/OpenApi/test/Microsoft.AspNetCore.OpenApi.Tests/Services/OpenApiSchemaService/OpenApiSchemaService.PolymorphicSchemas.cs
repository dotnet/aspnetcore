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
            var schema = mediaType.Schema;
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
            Assert.Equal("triangle", triangleSchema.Properties[schema.Discriminator.PropertyName].Enum.First().GetValue<string>());
            Assert.True(document.Components.Schemas.TryGetValue("ShapeSquare", out var squareSchema));
            Assert.Equal("square", squareSchema.Properties[schema.Discriminator.PropertyName].Enum.First().GetValue<string>());
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
            var schema = mediaType.Schema;
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
            Assert.Equal(0, citySchema.Properties[schema.Discriminator.PropertyName].Enum.First().GetValue<int>());
            // Assert schema with discriminator = 1 has been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("WeatherForecastBaseWeatherForecastWithTimeSeries", out var timeSeriesSchema));
            Assert.Contains(schema.Discriminator.PropertyName, timeSeriesSchema.Properties.Keys);
            Assert.Equal(1, timeSeriesSchema.Properties[schema.Discriminator.PropertyName].Enum.First().GetValue<int>());
            // Assert schema with discriminator = 2 has been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("WeatherForecastBaseWeatherForecastWithLocalNews", out var newsSchema));
            Assert.Contains(schema.Discriminator.PropertyName, newsSchema.Properties.Keys);
            Assert.Equal(2, newsSchema.Properties[schema.Discriminator.PropertyName].Enum.First().GetValue<int>());
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
            var schema = mediaType.Schema;
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
            Assert.Equal("student", citySchema.Properties[schema.Discriminator.PropertyName].Enum.First().GetValue<string>());
            // Assert schema with discriminator = 1 has been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("PersonTeacher", out var timeSeriesSchema));
            Assert.Contains(schema.Discriminator.PropertyName, timeSeriesSchema.Properties.Keys);
            Assert.Equal("teacher", timeSeriesSchema.Properties[schema.Discriminator.PropertyName].Enum.First().GetValue<string>());
        });
    }

    [Fact]
    public async Task HandlesPolymorphicTypesWithNonAbstractBaseClassWithNoDiscriminator()
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
            var schema = mediaType.Schema;
            // Assert discriminator mappings are not configured for this type since we
            // can't meet OpenAPI's restrictions that derived types _always_ have a discriminator
            // property associated with them.
            Assert.Null(schema.Discriminator);
            Assert.Collection(schema.AnyOf,
                schema => Assert.Equal("ColorPaintColor", schema.Reference.Id),
                schema => Assert.Equal("ColorFabricColor", schema.Reference.Id),
                schema => Assert.Equal("ColorBase", schema.Reference.Id));
            // Assert schema with discriminator = "paint" has been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("ColorPaintColor", out var paintSchema));
            Assert.Contains("$type", paintSchema.Properties.Keys);
            Assert.Equal("paint", paintSchema.Properties["$type"].Enum.First().GetValue<string>());
            // Assert schema with discriminator = "fabric" has been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("ColorFabricColor", out var fabricSchema));
            Assert.Contains("$type", fabricSchema.Properties.Keys);
            Assert.Equal("fabric", fabricSchema.Properties["$type"].Enum.First().GetValue<string>());
            // Assert that schema for `Color` has been inserted into the components without a discriminator
            Assert.True(document.Components.Schemas.TryGetValue("ColorBase", out var colorSchema));
            Assert.DoesNotContain("$type", colorSchema.Properties.Keys);
        });
    }

    [Fact]
    public async Task HandlesPolymorphicTypesWithNonAbstractBaseClassAndDiscriminator()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (Pet pet) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            var requestBody = operation.RequestBody.Content;
            Assert.True(requestBody.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema;
            // Assert discriminator mappings have been configured correctly
            Assert.Equal("$type", schema.Discriminator.PropertyName);
            Assert.Collection(schema.Discriminator.Mapping,
                item => Assert.Equal("cat", item.Key),
                item => Assert.Equal("dog", item.Key),
                item => Assert.Equal("pet", item.Key)
            );
            Assert.Collection(schema.Discriminator.Mapping,
                item => Assert.Equal("#/components/schemas/PetCat", item.Value),
                item => Assert.Equal("#/components/schemas/PetDog", item.Value),
                item => Assert.Equal("#/components/schemas/PetPet", item.Value)
            );
            // OpenAPI requires that derived types in a polymorphic schema _always_ have a discriminator
            // property associated with them. STJ permits the discriminator to be omitted from the
            // if the base type is a non-abstract class and falls back to serializing to this base
            // type. In this scenario, we check that the base class is not included in the `anyOf`
            // schema.
            Assert.Collection(schema.AnyOf,
                schema => Assert.Equal("PetCat", schema.Reference.Id),
                schema => Assert.Equal("PetDog", schema.Reference.Id),
                schema => Assert.Equal("PetPet", schema.Reference.Id));
            // Assert schema with discriminator = "dog" has been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("PetDog", out var dogSchema));
            Assert.Contains(schema.Discriminator.PropertyName, dogSchema.Properties.Keys);
            Assert.Equal("dog", dogSchema.Properties[schema.Discriminator.PropertyName].Enum.First().GetValue<string>());
            // Assert schema with discriminator = "cat" has been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("PetCat", out var catSchema));
            Assert.Contains(schema.Discriminator.PropertyName, catSchema.Properties.Keys);
            Assert.Equal("cat", catSchema.Properties[schema.Discriminator.PropertyName].Enum.First().GetValue<string>());
            // Assert schema with discriminator = "cat" has been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("PetPet", out var petSchema));
            Assert.Contains(schema.Discriminator.PropertyName, petSchema.Properties.Keys);
            Assert.Equal("pet", petSchema.Properties[schema.Discriminator.PropertyName].Enum.First().GetValue<string>());
        });
    }

    [Fact]
    public async Task HandlesPolymorphicTypesWithNoExplicitDiscriminators()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (Organism color) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            var requestBody = operation.RequestBody.Content;
            Assert.True(requestBody.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema;
            // Assert discriminator mappings are not configured for this type since we
            // can't meet OpenAPI's restrictions that derived types _always_ have a discriminator
            // property associated with them.
            Assert.Null(schema.Discriminator);
            Assert.Collection(schema.AnyOf,
                schema => Assert.Equal("OrganismAnimal", schema.Reference.Id),
                schema => Assert.Equal("OrganismPlant", schema.Reference.Id),
                schema => Assert.Equal("OrganismBase", schema.Reference.Id));
            // Assert that schemas without discriminators have been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("OrganismAnimal", out var animalSchema));
            Assert.DoesNotContain("$type", animalSchema.Properties.Keys);
            Assert.True(document.Components.Schemas.TryGetValue("OrganismPlant", out var plantSchema));
            Assert.DoesNotContain("$type", plantSchema.Properties.Keys);
            Assert.True(document.Components.Schemas.TryGetValue("OrganismBase", out var baseSchema));
            Assert.DoesNotContain("$type", baseSchema.Properties.Keys);
        });
    }

    [Fact]
    public async Task HandlesPolymorphicTypesWithSelfReference()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (Employee color) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            var requestBody = operation.RequestBody.Content;
            Assert.True(requestBody.TryGetValue("application/json", out var mediaType));
            Assert.Equal("Employee", mediaType.Schema.Reference.Id);
            var schema = mediaType.Schema;
            // Assert that discriminator mappings are configured correctly for type.
            Assert.Equal("$type", schema.Discriminator.PropertyName);
            Assert.Collection(schema.Discriminator.Mapping,
                item => Assert.Equal("manager", item.Key),
                item => Assert.Equal("employee", item.Key)
            );
            Assert.Collection(schema.Discriminator.Mapping,
                item => Assert.Equal("#/components/schemas/EmployeeManager", item.Value),
                item => Assert.Equal("#/components/schemas/EmployeeEmployee", item.Value)
            );
            // Assert that anyOf schemas use the correct reference IDs.
            Assert.Collection(schema.AnyOf,
                schema => Assert.Equal("EmployeeManager", schema.Reference.Id),
                schema => Assert.Equal("EmployeeEmployee", schema.Reference.Id));
            // Assert that schemas without discriminators have been inserted into the components
            Assert.True(document.Components.Schemas.TryGetValue("EmployeeManager", out var managerSchema));
            Assert.Equal("manager", managerSchema.Properties[schema.Discriminator.PropertyName].Enum.First().GetValue<string>());
            Assert.True(document.Components.Schemas.TryGetValue("EmployeeEmployee", out var employeeSchema));
            Assert.Equal("employee", employeeSchema.Properties[schema.Discriminator.PropertyName].Enum.First().GetValue<string>());
            // Assert that the schema has a correct self-reference to the base-type. This points to the schema that contains the discriminator.
            Assert.Equal("Employee", employeeSchema.Properties["manager"].Reference.Id);
        });
    }
}
