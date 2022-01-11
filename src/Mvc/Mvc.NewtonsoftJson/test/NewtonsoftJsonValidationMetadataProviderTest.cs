// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson;

public class NewtonsoftJsonValidationMetadataProviderTest
{
    [Fact]
    public void CreateValidationMetadata_SetValidationPropertyName_WithJsonPropertyNameAttribute()
    {
        var metadataProvider = new NewtonsoftJsonValidationMetadataProvider();
        var propertyName = "sample-data";

        var key = ModelMetadataIdentity.ForProperty(typeof(SampleTestClass).GetProperty(nameof(SampleTestClass.NoAttributesProperty)), typeof(int), typeof(SampleTestClass));
        var modelAttributes = new ModelAttributes(Array.Empty<object>(), new[] { new JsonPropertyAttribute() { PropertyName = propertyName } }, Array.Empty<object>());
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
        var metadataProvider = new NewtonsoftJsonValidationMetadataProvider();
        var propertyName = nameof(SampleTestClass.NoAttributesProperty);

        var key = ModelMetadataIdentity.ForProperty(typeof(SampleTestClass).GetProperty(propertyName), typeof(int), typeof(SampleTestClass));
        var modelAttributes = new ModelAttributes(Array.Empty<object>(), Array.Empty<object>(), Array.Empty<object>());
        var context = new ValidationMetadataProviderContext(key, modelAttributes);

        // Act
        metadataProvider.CreateValidationMetadata(context);

        // Assert
        Assert.NotNull(context.ValidationMetadata.ValidationModelName);
        Assert.Equal(new CamelCaseNamingStrategy().GetPropertyName(propertyName, false), context.ValidationMetadata.ValidationModelName);
    }

    [Theory]
    [MemberData(nameof(NamingPolicies))]
    public void CreateValidationMetadata_SetValidationPropertyName_WithJsonNamingPolicy(NamingStrategy namingStrategy)
    {
        var metadataProvider = new NewtonsoftJsonValidationMetadataProvider(namingStrategy);
        var propertyName = nameof(SampleTestClass.NoAttributesProperty);

        var key = ModelMetadataIdentity.ForProperty(typeof(SampleTestClass).GetProperty(propertyName), typeof(int), typeof(SampleTestClass));
        var modelAttributes = new ModelAttributes(Array.Empty<object>(), Array.Empty<object>(), Array.Empty<object>());
        var context = new ValidationMetadataProviderContext(key, modelAttributes);

        // Act
        metadataProvider.CreateValidationMetadata(context);

        // Assert
        Assert.NotNull(context.ValidationMetadata.ValidationModelName);
        Assert.Equal(namingStrategy.GetPropertyName(propertyName, false), context.ValidationMetadata.ValidationModelName);
    }

    public static TheoryData<NamingStrategy> NamingPolicies
    {
        get
        {
            return new TheoryData<NamingStrategy>
                {
                    new UpperCaseJsonNamingPolicy(),
                    new CamelCaseNamingStrategy()
                };
        }
    }

    public class UpperCaseJsonNamingPolicy : NamingStrategy
    {
        protected override string ResolvePropertyName(string name) => name?.ToUpperInvariant();
    }

    public class SampleTestClass
    {
        public int NoAttributesProperty { get; set; }
    }
}
