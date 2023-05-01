// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

public class SystemTextJsonValidationMetadataProviderTest
{
    [Fact]
    public void CreateValidationMetadata_SetValidationPropertyName_WithJsonPropertyNameAttribute()
    {
        var metadataProvider = new SystemTextJsonValidationMetadataProvider();
        var propertyName = "sample-data";

        var key = ModelMetadataIdentity.ForProperty(typeof(SampleTestClass).GetProperty(nameof(SampleTestClass.NoAttributesProperty)), typeof(int), typeof(SampleTestClass));
        var modelAttributes = new ModelAttributes(Array.Empty<object>(), new[] { new JsonPropertyNameAttribute(propertyName) }, Array.Empty<object>());
        var context = new ValidationMetadataProviderContext(key, modelAttributes);

        // Act
        metadataProvider.CreateValidationMetadata(context);

        // Assert
        Assert.NotNull(context.ValidationMetadata.ValidationModelName);
        Assert.Equal(propertyName, context.ValidationMetadata.ValidationModelName);
    }

    [Fact]
    public void CreateValidationMetadata_SetValidationPropertyName_CamelCaseWithDefaultNamingPolicy()
    {
        var metadataProvider = new SystemTextJsonValidationMetadataProvider();
        var propertyName = nameof(SampleTestClass.NoAttributesProperty);

        var key = ModelMetadataIdentity.ForProperty(typeof(SampleTestClass).GetProperty(propertyName), typeof(int), typeof(SampleTestClass));
        var modelAttributes = new ModelAttributes(Array.Empty<object>(), Array.Empty<object>(), Array.Empty<object>());
        var context = new ValidationMetadataProviderContext(key, modelAttributes);

        // Act
        metadataProvider.CreateValidationMetadata(context);

        // Assert
        Assert.NotNull(context.ValidationMetadata.ValidationModelName);
        Assert.Equal(JsonNamingPolicy.CamelCase.ConvertName(propertyName), context.ValidationMetadata.ValidationModelName);
    }

    [Fact]
    // Test for https://github.com/dotnet/aspnetcore/issues/47835
    public void CreateValidationMetadata_SetValidationPropertyName_WithNullKeyName()
    {
        var metadataProvider = new SystemTextJsonValidationMetadataProvider(JsonNamingPolicy.SnakeCaseLower);
        var key = ModelMetadataIdentity.ForType(typeof(SampleTestClass));
        var modelAttributes = new ModelAttributes(Array.Empty<object>(), Array.Empty<object>(), Array.Empty<object>());
        var context = new ValidationMetadataProviderContext(key, modelAttributes);

        // Act
        metadataProvider.CreateValidationMetadata(context);

        // Assert
        Assert.Null(context.ValidationMetadata.ValidationModelName);
    }

    [Theory]
    [MemberData(nameof(NamingPolicies))]
    public void CreateValidationMetadata_SetValidationPropertyName_WithJsonNamingPolicy(JsonNamingPolicy namingPolicy)
    {
        var metadataProvider = new SystemTextJsonValidationMetadataProvider(namingPolicy);
        var propertyName = nameof(SampleTestClass.NoAttributesProperty);

        var key = ModelMetadataIdentity.ForProperty(typeof(SampleTestClass).GetProperty(propertyName), typeof(int), typeof(SampleTestClass));
        var modelAttributes = new ModelAttributes(Array.Empty<object>(), Array.Empty<object>(), Array.Empty<object>());
        var context = new ValidationMetadataProviderContext(key, modelAttributes);

        // Act
        metadataProvider.CreateValidationMetadata(context);

        // Assert
        Assert.NotNull(context.ValidationMetadata.ValidationModelName);
        Assert.Equal(namingPolicy.ConvertName(propertyName), context.ValidationMetadata.ValidationModelName);
    }

    public static TheoryData<JsonNamingPolicy> NamingPolicies
    {
        get
        {
            return new TheoryData<JsonNamingPolicy>
                {
                    UpperCaseJsonNamingPolicy.Instance,
                    JsonNamingPolicy.CamelCase
                };
        }
    }

    public class UpperCaseJsonNamingPolicy : System.Text.Json.JsonNamingPolicy
    {
        public static JsonNamingPolicy Instance = new UpperCaseJsonNamingPolicy();

        public override string ConvertName(string name)
        {
            return name?.ToUpperInvariant();
        }
    }

    public class SampleTestClass
    {
        public int NoAttributesProperty { get; set; }
    }
}
