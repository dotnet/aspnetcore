// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class CachedDataAnnotationsModelMetadataProviderTest
    {
        [Bind(Include = nameof(IncludedAndExcludedExplicitly1) + "," + nameof(IncludedExplicitly1),
              Exclude = nameof(IncludedAndExcludedExplicitly1) + "," + nameof(ExcludedExplicitly1),
            Prefix = "TypePrefix")]
        private class TypeWithExludedAndIncludedPropertiesUsingBindAttribute
        {
            public int ExcludedExplicitly1 { get; set; }

            public int IncludedAndExcludedExplicitly1 { get; set; }

            public int IncludedExplicitly1 { get; set; }

            public int NotIncludedOrExcluded { get; set; }

            public void ActionWithBindAttribute(
                          [Bind(Include = "Property1, Property2,IncludedAndExcludedExplicitly1",
                                Exclude ="Property3, Property4, IncludedAndExcludedExplicitly1",
                                Prefix = "ParameterPrefix")]
                TypeWithExludedAndIncludedPropertiesUsingBindAttribute param)
            {
            }
        }

        [Fact]
        public void DataAnnotationsModelMetadataProvider_ReadsIncludedAndExcludedProperties_ForTypes()
        {
            // Arrange
            var type = typeof(TypeWithExludedAndIncludedPropertiesUsingBindAttribute);
            var provider = new DataAnnotationsModelMetadataProvider();
            var expectedIncludedPropertyNames = new[] { "IncludedAndExcludedExplicitly1", "IncludedExplicitly1" };
            var expectedExcludedPropertyNames = new[] { "IncludedAndExcludedExplicitly1", "ExcludedExplicitly1" };

            // Act 
            var metadata = provider.GetMetadataForType(null, type);

            // Assert
            Assert.Equal(expectedIncludedPropertyNames.ToList(), metadata.IncludedProperties);
            Assert.Equal(expectedExcludedPropertyNames.ToList(), metadata.ExcludedProperties);
        }

        [Fact]
        public void ModelMetadataProvider_ReadsIncludedAndExcludedProperties_OnlyAtParameterLevel_ForParameters()
        {
            // Arrange
            var type = typeof(TypeWithExludedAndIncludedPropertiesUsingBindAttribute);
            var methodInfo = type.GetMethod("ActionWithBindAttribute");
            var provider = new DataAnnotationsModelMetadataProvider();

            // Note it does an intersection for included and a union for excluded.
            var expectedIncludedPropertyNames = new[] { "Property1", "Property2", "IncludedAndExcludedExplicitly1" };
            var expectedExcludedPropertyNames = new[] {
                "Property3", "Property4", "IncludedAndExcludedExplicitly1" };
           
            // Act 
            var metadata = provider.GetMetadataForParameter(
                modelAccessor: null,
                methodInfo: methodInfo,
                parameterName: "param",
                binderMetadata: null);

            // Assert
            Assert.Equal(expectedIncludedPropertyNames.ToList(), metadata.IncludedProperties);
            Assert.Equal(expectedExcludedPropertyNames.ToList(), metadata.ExcludedProperties);
        }

        [Fact]
        public void ModelMetadataProvider_ReadsPrefixProperty_OnlyAtParameterLevel_ForParameters()
        {
            // Arrange
            var type = typeof(TypeWithExludedAndIncludedPropertiesUsingBindAttribute);
            var methodInfo = type.GetMethod("ActionWithBindAttribute");
            var provider = new DataAnnotationsModelMetadataProvider();

            // Act 
            var metadata = provider.GetMetadataForParameter(
                modelAccessor: null, 
                methodInfo: methodInfo, 
                parameterName: "param",
                binderMetadata: null);

            // Assert
            Assert.Equal("ParameterPrefix", metadata.ModelName);
        }

        [Fact]
        public void DataAnnotationsModelMetadataProvider_ReadsModelNameProperty_ForTypes()
        {
            // Arrange
            var type = typeof(TypeWithExludedAndIncludedPropertiesUsingBindAttribute);
            var provider = new DataAnnotationsModelMetadataProvider();

            // Act 
            var metadata = provider.GetMetadataForType(null, type);

            // Assert
            Assert.Equal("TypePrefix", metadata.ModelName);
        }

        [Fact]
        public void DataAnnotationsModelMetadataProvider_ReadsModelNameProperty_ForParameters()
        {
            // Arrange
            var type = typeof(TypeWithExludedAndIncludedPropertiesUsingBindAttribute);
            var methodInfo = type.GetMethod("ActionWithBindAttribute");
            var provider = new DataAnnotationsModelMetadataProvider();

            // Act 
            var metadata = provider.GetMetadataForParameter(
                modelAccessor: null, 
                methodInfo: methodInfo, 
                parameterName: "param",
                binderMetadata: null);

            // Assert
            Assert.Equal("ParameterPrefix", metadata.ModelName);
        }

        [Fact]
        public void DataAnnotationsModelMetadataProvider_ReadsScaffoldColumnAttribute_ForShowForDisplay()
        {
            // Arrange
            var type = typeof(ScaffoldColumnModel);
            var provider = new DataAnnotationsModelMetadataProvider();

            // Act & Assert
            Assert.True(provider.GetMetadataForProperty(null, type, "NoAttribute").ShowForDisplay);
            Assert.True(provider.GetMetadataForProperty(null, type, "ScaffoldColumnTrue").ShowForDisplay);
            Assert.False(provider.GetMetadataForProperty(null, type, "ScaffoldColumnFalse").ShowForDisplay);
        }

        [Fact]
        public void DataAnnotationsModelMetadataProvider_ReadsScaffoldColumnAttribute_ForShowForEdit()
        {
            // Arrange
            var type = typeof(ScaffoldColumnModel);
            var provider = new DataAnnotationsModelMetadataProvider();

            // Act & Assert
            Assert.True(provider.GetMetadataForProperty(null, type, "NoAttribute").ShowForEdit);
            Assert.True(provider.GetMetadataForProperty(null, type, "ScaffoldColumnTrue").ShowForEdit);
            Assert.False(provider.GetMetadataForProperty(null, type, "ScaffoldColumnFalse").ShowForEdit);
        }

        [Fact]
        public void HiddenInputWorksOnProperty()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = provider.GetMetadataForType(modelAccessor: null, modelType: typeof(ClassWithHiddenProperties));
            var property = metadata.Properties.First(m => string.Equals("DirectlyHidden", m.PropertyName));

            // Act
            var result = property.HideSurroundingHtml;

            // Assert
            Assert.True(result);
        }

        // TODO #1000; enable test once we detect attributes on the property's type
        public void HiddenInputWorksOnPropertyType()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = provider.GetMetadataForType(modelAccessor: null, modelType: typeof(ClassWithHiddenProperties));
            var property = metadata.Properties.First(m => string.Equals("OfHiddenType", m.PropertyName));

            // Act
            var result = property.HideSurroundingHtml;

            // Assert
            Assert.True(result);
        }

        private class ScaffoldColumnModel
        {
            public int NoAttribute { get; set; }

            [ScaffoldColumn(scaffold: true)]
            public int ScaffoldColumnTrue { get; set; }

            [ScaffoldColumn(scaffold: false)]
            public int ScaffoldColumnFalse { get; set; }
        }

        [HiddenInput(DisplayValue = false)]
        private class HiddenClass
        {
            public string Property { get; set; }
        }

        private class ClassWithHiddenProperties
        {
            [HiddenInput(DisplayValue = false)]
            public string DirectlyHidden { get; set; }

            public HiddenClass OfHiddenType { get; set; }
        }
    }
}