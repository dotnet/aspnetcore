// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class CachedDataAnnotationsModelMetadataProviderTest
    {
        [Fact]
        public void DataAnnotationsModelMetadataProvider_UsesPredicateOnType()
        {
            // Arrange
            var type = typeof(User);

            var provider = new DataAnnotationsModelMetadataProvider();
            var context = new ModelBindingContext();

            var expected = new[] { "IsAdmin", "UserName" };

            // Act
            var metadata = provider.GetMetadataForType(type);

            // Assert
            var predicate = metadata.PropertyBindingPredicateProvider.PropertyFilter;

            var matched = new HashSet<string>();
            foreach (var property in metadata.Properties)
            {
                if (predicate(context, property.PropertyName))
                {
                    matched.Add(property.PropertyName);
                }
            }

            Assert.Equal<string>(expected, matched);
        }

        [Fact]
        public void DataAnnotationsModelMetadataProvider_UsesPredicateOnParameter()
        {
            // Arrange
            var type = GetType();
            var methodInfo = type.GetMethod(
                "ActionWithoutBindAttribute",
                BindingFlags.Instance | BindingFlags.NonPublic);

            var provider = new DataAnnotationsModelMetadataProvider();
            var context = new ModelBindingContext();

            // Note it does an intersection for included -- only properties that
            // pass both predicates will be bound.
            var expected = new[] { "IsAdmin", "UserName" };

            // Act
            var metadata = provider.GetMetadataForParameter(
                methodInfo: methodInfo,
                parameterName: "param");

            // Assert
            var predicate = metadata.PropertyBindingPredicateProvider.PropertyFilter;
            Assert.NotNull(predicate);

            var matched = new HashSet<string>();
            foreach (var property in metadata.Properties)
            {
                if (predicate(context, property.PropertyName))
                {
                    matched.Add(property.PropertyName);
                }
            }

            Assert.Equal<string>(expected, matched);
        }

        [Fact]
        public void DataAnnotationsModelMetadataProvider_UsesPredicateOnParameter_Merge()
        {
            // Arrange
            var type = GetType();
            var methodInfo = type.GetMethod(
                "ActionWithBindAttribute",
                BindingFlags.Instance | BindingFlags.NonPublic);

            var provider = new DataAnnotationsModelMetadataProvider();
            var context = new ModelBindingContext();

            // Note it does an intersection for included -- only properties that
            // pass both predicates will be bound.
            var expected = new[] { "IsAdmin" };

            // Act
            var metadata = provider.GetMetadataForParameter(
                methodInfo: methodInfo,
                parameterName: "param");

            // Assert
            var predicate = metadata.PropertyBindingPredicateProvider.PropertyFilter;
            Assert.NotNull(predicate);

            var matched = new HashSet<string>();
            foreach (var property in metadata.Properties)
            {
                if (predicate(context, property.PropertyName))
                {
                    matched.Add(property.PropertyName);
                }
            }

            Assert.Equal<string>(expected, matched);
        }

        [Fact]
        public void DataAnnotationsModelMetadataProvider_ReadsModelNameProperty_ForParameters()
        {
            // Arrange
            var type = GetType();
            var methodInfo = type.GetMethod(
                "ActionWithBindAttribute",
                BindingFlags.Instance | BindingFlags.NonPublic);

            var provider = new DataAnnotationsModelMetadataProvider();

            // Act
            var metadata = provider.GetMetadataForParameter(
                methodInfo: methodInfo,
                parameterName: "param");

            // Assert
            Assert.Equal("ParameterPrefix", metadata.BinderModelName);
        }

        [Fact]
        public void DataAnnotationsModelMetadataProvider_ReadsModelNameProperty_ForTypes()
        {
            // Arrange
            var type = typeof(User);
            var provider = new DataAnnotationsModelMetadataProvider();

            // Act
            var metadata = provider.GetMetadataForType(type);

            // Assert
            Assert.Equal("TypePrefix", metadata.BinderModelName);
        }


        [Fact]
        public void DataAnnotationsModelMetadataProvider_ReadsScaffoldColumnAttribute_ForShowForDisplay()
        {
            // Arrange
            var type = typeof(ScaffoldColumnModel);
            var provider = new DataAnnotationsModelMetadataProvider();

            // Act & Assert
            Assert.True(provider.GetMetadataForProperty(type, "NoAttribute").ShowForDisplay);
            Assert.True(provider.GetMetadataForProperty(type, "ScaffoldColumnTrue").ShowForDisplay);
            Assert.False(provider.GetMetadataForProperty(type, "ScaffoldColumnFalse").ShowForDisplay);
        }

        [Fact]
        public void DataAnnotationsModelMetadataProvider_ReadsScaffoldColumnAttribute_ForShowForEdit()
        {
            // Arrange
            var type = typeof(ScaffoldColumnModel);
            var provider = new DataAnnotationsModelMetadataProvider();

            // Act & Assert
            Assert.True(provider.GetMetadataForProperty(type, "NoAttribute").ShowForEdit);
            Assert.True(provider.GetMetadataForProperty(type, "ScaffoldColumnTrue").ShowForEdit);
            Assert.False(provider.GetMetadataForProperty(type, "ScaffoldColumnFalse").ShowForEdit);
        }

        [Fact]
        public void HiddenInputWorksOnProperty_ForHideSurroundingHtml()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = provider.GetMetadataForType(modelType: typeof(ClassWithHiddenProperties));
            var property = metadata.Properties["DirectlyHidden"];

            // Act
            var result = property.HideSurroundingHtml;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HiddenInputWorksOnPropertyType_ForHideSurroundingHtml()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(ClassWithHiddenProperties));
            var property = metadata.Properties["OfHiddenType"];

            // Act
            var result = property.HideSurroundingHtml;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HiddenInputWorksOnProperty_ForTemplateHint()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(ClassWithHiddenProperties));
            var property = metadata.Properties["DirectlyHidden"];

            // Act
            var result = property.TemplateHint;

            // Assert
            Assert.Equal("HiddenInput", result);
        }

        [Fact]
        public void HiddenInputWorksOnPropertyType_ForTemplateHint()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();
            var metadata = provider.GetMetadataForType(typeof(ClassWithHiddenProperties));
            var property = metadata.Properties["OfHiddenType"];

            // Act
            var result = property.TemplateHint;

            // Assert
            Assert.Equal("HiddenInput", result);
        }

        [Fact]
        public void GetMetadataForProperty_WithNoBinderMetadata_GetsItFromType()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();

            // Act
            var propertyMetadata = provider.GetMetadataForProperty(typeof(Person), nameof(Person.Parent));

            // Assert
            Assert.NotNull(propertyMetadata.BinderMetadata);
            var attribute = Assert.IsType<TypeBasedBinderAttribute>(propertyMetadata.BinderMetadata);
            Assert.Equal("PersonType", propertyMetadata.BinderModelName);
        }

        [Fact]
        public void GetMetadataForProperty_WithBinderMetadataOnPropertyAndType_GetsMetadataFromProperty()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();

            // Act
            var propertyMetadata = provider.GetMetadataForProperty(typeof(Person), nameof(Person.GrandParent));

            // Assert
            Assert.NotNull(propertyMetadata.BinderMetadata);
            var attribute = Assert.IsType<NonTypeBasedBinderAttribute>(propertyMetadata.BinderMetadata);
            Assert.Equal("GrandParentProperty", propertyMetadata.BinderModelName);
        }

        [Fact]
        public void GetMetadataForParameter_WithNoBinderMetadata_GetsItFromType()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();

            // Act
            var parameterMetadata = provider.GetMetadataForParameter(
                typeof(Person).GetMethod("Update"),
                "person");

            // Assert
            Assert.NotNull(parameterMetadata.BinderMetadata);
            var attribute = Assert.IsType<TypeBasedBinderAttribute>(parameterMetadata.BinderMetadata);
            Assert.Equal("PersonType", parameterMetadata.BinderModelName);
        }

        [Fact]
        public void GetMetadataForParameter_WithBinderDataOnParameterAndType_GetsMetadataFromParameter()
        {
            // Arrange
            var provider = new DataAnnotationsModelMetadataProvider();

            // Act
            var parameterMetadata = provider.GetMetadataForParameter(
                typeof(Person).GetMethod("Save"),
                "person");

            // Assert
            Assert.NotNull(parameterMetadata.BinderMetadata);
            var attribute = Assert.IsType<NonTypeBasedBinderAttribute>(parameterMetadata.BinderMetadata);
            Assert.Equal("PersonParameter", parameterMetadata.BinderModelName);
        }

        private void ActionWithoutBindAttribute(User param)
        {
        }

        private void ActionWithBindAttribute([Bind(new string[] { "IsAdmin" }, Prefix = "ParameterPrefix")] User param)
        {
        }

        public class TypeBasedBinderAttribute : Attribute, IBinderMetadata, IModelNameProvider
        {
            public string Name { get; set; }
        }

        public class NonTypeBasedBinderAttribute : Attribute, IBinderMetadata, IModelNameProvider
        {
            public string Name { get; set; }
        }

        [TypeBasedBinder(Name = "PersonType")]
        public class Person
        {
            public Person Parent { get; set; }

            [NonTypeBasedBinder(Name = "GrandParentProperty")]
            public Person GrandParent { get; set; }

            public void Update(Person person)
            {
            }

            public void Save([NonTypeBasedBinder(Name = "PersonParameter")] Person person)
            {
            }
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

        [Bind(new[] { nameof(IsAdmin), nameof(UserName) }, Prefix = "TypePrefix")]
        private class User
        {
            public int Id { get; set; }

            public bool IsAdmin { get; set; }

            public int UserName { get; set; }

            public int NotIncludedOrExcluded { get; set; }
        }
    }
}