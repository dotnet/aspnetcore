// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DefaultBodyModelValidatorTests
    {
        private static Person LonelyPerson;

        static DefaultBodyModelValidatorTests()
        {
            LonelyPerson = new Person() { Name = "Reallllllllly Long Name" };
            LonelyPerson.Friend = LonelyPerson;
        }

        public static IEnumerable<object[]> ValidationErrors
        {
            get
            {
                // returns an array of model, type of model and expected errors.
                // Primitives
                yield return new object[] { null, typeof(Person), new Dictionary<string, string>() };
                yield return new object[] { 14, typeof(int), new Dictionary<string, string>() };
                yield return new object[] { "foo", typeof(string), new Dictionary<string, string>() };

                // Object Traversal : make sure we can traverse the object graph without throwing
                yield return new object[]
                {
                    new ValueType() { Reference = "ref", Value = 256 },
                    typeof(ValueType),
                    new Dictionary<string, string>()
                };
                yield return new object[]
                {
                    new ReferenceType() { Reference = "ref", Value = 256 },
                    typeof(ReferenceType),
                    new Dictionary<string, string>()
                };

                // Classes
                yield return new object[]
                {
                    new Person() { Name = "Rick", Profession = "Astronaut" },
                    typeof(Person),
                    new Dictionary<string, string>()
                };
                yield return new object[]
                {
                    new Person(),
                    typeof(Person),
                    new Dictionary<string, string>()
                    {
                        { "Name", ValidationAttributeUtil.GetRequiredErrorMessage("Name") },
                        { "Profession", ValidationAttributeUtil.GetRequiredErrorMessage("Profession") }
                    }
                };

                yield return new object[]
                {
                    new Person() { Name = "Rick", Friend = new Person() },
                    typeof(Person),
                    new Dictionary<string, string>()
                    {
                        { "Profession", ValidationAttributeUtil.GetRequiredErrorMessage("Profession") },
                        { "Friend.Name", ValidationAttributeUtil.GetRequiredErrorMessage("Name") },
                        { "Friend.Profession", ValidationAttributeUtil.GetRequiredErrorMessage("Profession") }
                    }
                };

                // Collections
                yield return new object[]
                {
                    new Person[] { new Person(), new Person() },
                    typeof(Person[]),
                    new Dictionary<string, string>()
                    {
                        { "[0].Name", ValidationAttributeUtil.GetRequiredErrorMessage("Name") },
                        { "[0].Profession", ValidationAttributeUtil.GetRequiredErrorMessage("Profession") },
                        { "[1].Name", ValidationAttributeUtil.GetRequiredErrorMessage("Name") },
                        { "[1].Profession", ValidationAttributeUtil.GetRequiredErrorMessage("Profession") }
                    }
                };

                yield return new object[]
                {
                    new List<Person> { new Person(), new Person() },
                    typeof(Person[]),
                    new Dictionary<string, string>()
                    {
                        { "[0].Name", ValidationAttributeUtil.GetRequiredErrorMessage("Name") },
                        { "[0].Profession", ValidationAttributeUtil.GetRequiredErrorMessage("Profession") },
                        { "[1].Name", ValidationAttributeUtil.GetRequiredErrorMessage("Name") },
                        { "[1].Profession", ValidationAttributeUtil.GetRequiredErrorMessage("Profession") }
                    }
                };

                if (!TestPlatformHelper.IsMono)
                {
                    // In Mono this throws a NullRef Exception.
                    // Should be investigated - https://github.com/aspnet/Mvc/issues/1261
                    yield return new object[]
                    {
                        new Dictionary<string, Person> { { "Joe", new Person() } , { "Mark", new Person() } },
                        typeof(Dictionary<string, Person>),
                        new Dictionary<string, string>()
                        {
                            { "[0].Value.Name", ValidationAttributeUtil.GetRequiredErrorMessage("Name") },
                            { "[0].Value.Profession", ValidationAttributeUtil.GetRequiredErrorMessage("Profession") },
                            { "[1].Value.Name", ValidationAttributeUtil.GetRequiredErrorMessage("Name") },
                            { "[1].Value.Profession", ValidationAttributeUtil.GetRequiredErrorMessage("Profession") }
                        }
                    };
                }

                // IValidatableObject's
                yield return new object[]
                {
                    new ValidatableModel(),
                    typeof(ValidatableModel),
                    new Dictionary<string, string>()
                    {
                        { "", "Error1" },
                        { "Property1", "Error2" },
                        { "Property2", "Error3" },
                        { "Property3", "Error3" }
                    }
                };

                yield return new object[]
                {
                    new[] { new ValidatableModel() },
                    typeof(ValidatableModel[]),
                    new Dictionary<string, string>()
                    {
                        { "[0]", "Error1" },
                        { "[0].Property1", "Error2" },
                        { "[0].Property2", "Error3" },
                        { "[0].Property3", "Error3" }
                    }
                };

                // Nested Objects
                yield return new object[]
                {
                    new Org()
                    {
                        Id = 1,
                        OrgName = "Org",
                        Dev = new Team
                        {
                            Id = 10,
                            TeamName = "HelloWorldTeam",
                            Lead = "SampleLeadDev",
                            TeamSize = 2
                        },
                        Test = new Team
                        {
                            Id = 11,
                            TeamName = "HWT",
                            Lead = "SampleTestLead",
                            TeamSize = 12
                        }
                    },
                    typeof(Org),
                    new Dictionary<string, string>()
                    {
                        { "OrgName", ValidationAttributeUtil.GetStringLengthErrorMessage(4, 20, "OrgName") },
                        { "Dev.Lead", ValidationAttributeUtil.GetMaxLengthErrorMessage(10, "Lead") },
                        { "Dev.TeamSize", ValidationAttributeUtil.GetRangeErrorMessage(3, 100, "TeamSize") },
                        { "Test.TeamName", ValidationAttributeUtil.GetStringLengthErrorMessage(4, 20, "TeamName") },
                        { "Test.Lead", ValidationAttributeUtil.GetMaxLengthErrorMessage(10, "Lead") }
                    }
                };

                // Testing we don't validate fields
                yield return new object[]
                {
                    new VariableTest() { test = 5 },
                    typeof(VariableTest),
                    new Dictionary<string, string>()
                };

                // Testing we don't blow up on cycles
                yield return new object[]
                {
                    LonelyPerson,
                    typeof(Person),
                    new Dictionary<string, string>()
                    {
                        { "Name", ValidationAttributeUtil.GetStringLengthErrorMessage(null, 10, "Name") },
                        { "Profession", ValidationAttributeUtil.GetRequiredErrorMessage("Profession") }
                    }
                };
            }
        }

        [Theory]
        [ReplaceCulture]
        [MemberData(nameof(ValidationErrors))]
        public void ExpectedValidationErrorsRaised(object model, Type type, Dictionary<string, string> expectedErrors)
        {
            // Arrange
            var validationContext = GetModelValidationContext(model, type);

            // Act
            Assert.DoesNotThrow(() =>
                new DefaultBodyModelValidator().Validate(validationContext, keyPrefix: string.Empty)
            );

            // Assert
            var actualErrors = new Dictionary<string, string>();
            foreach (var keyStatePair in validationContext.ModelState)
            {
                foreach (var error in keyStatePair.Value.Errors)
                {
                    actualErrors.Add(keyStatePair.Key, error.ErrorMessage);
                }
            }

            Assert.Equal(expectedErrors.Count, actualErrors.Count);
            foreach (var keyErrorPair in expectedErrors)
            {
                Assert.Contains(keyErrorPair.Key, actualErrors.Keys);
                Assert.Equal(keyErrorPair.Value, actualErrors[keyErrorPair.Key]);
            }
        }

        [Fact]
        [ReplaceCulture]
        public void BodyValidator_Throws_IfPropertyAccessorThrows()
        {
            // Arrange
            var validationContext = GetModelValidationContext(new Uri("/api/values", UriKind.Relative), typeof(Uri));

            // Act & Assert
            Assert.Throws(
                typeof(InvalidOperationException),
                () =>
                {
                    new DefaultBodyModelValidator().Validate(validationContext, keyPrefix: string.Empty);
                });
        }

        public static IEnumerable<object[]> ObjectsWithPropertiesWhichThrowOnGet
        {
            get
            {
                yield return new object[] {
                    new Uri("/api/values", UriKind.Relative),
                    typeof(Uri),
                    new List<Type>() { typeof(Uri) }
                };
                yield return new object[] {
                    new DerivedUri("/api/values", UriKind.Relative),
                    typeof(Uri),
                    new List<Type>() { typeof(Uri) }
                };
                yield return new object[] { new Dictionary<string, Uri> {
                    { "values",  new Uri("/api/values", UriKind.Relative) },
                    { "hello",  new Uri("/api/hello", UriKind.Relative) }
                }, typeof(Uri), new List<Type>() { typeof(Uri) } };
            }
        }

        [Theory]
        [MemberData(nameof(ObjectsWithPropertiesWhichThrowOnGet))]
        [ReplaceCulture]
        public void BodyValidator_DoesNotThrow_IfExcludedPropertyAccessorsThrow(
            object input, Type type, List<Type> excludedTypes)
        {
            // Arrange
            var validationContext = GetModelValidationContext(input, type, excludedTypes);

            // Act & Assert
            Assert.DoesNotThrow(
                () =>
                {
                    new DefaultBodyModelValidator().Validate(validationContext, keyPrefix: string.Empty);
                });
            Assert.True(validationContext.ModelState.IsValid);
        }

        [Fact]
        [ReplaceCulture]
        public void BodyValidator_Throws_IfPropertyGetterThrows()
        {
            // Arrange
            var validationContext = GetModelValidationContext(
                new Uri("/api/values", UriKind.Relative), typeof(Uri), new List<Type>());

            // Act & Assert
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    new DefaultBodyModelValidator().Validate(validationContext, keyPrefix: string.Empty);
                });
            Assert.True(validationContext.ModelState.IsValid);
        }

        [Fact]
        [ReplaceCulture]
        public void MultipleValidationErrorsOnSameMemberReported()
        {
            // Arrange
            var model = new Address() { Street = "Microsoft Way" };
            var validationContext = GetModelValidationContext(model, model.GetType());

            // Act
            Assert.DoesNotThrow(() =>
                new DefaultBodyModelValidator().Validate(validationContext, keyPrefix: string.Empty)
            );

            // Assert
            Assert.Contains("Street", validationContext.ModelState.Keys);
            var streetState = validationContext.ModelState["Street"];
            Assert.Equal(2, streetState.Errors.Count);
            var errorCollection = streetState.Errors.Select(e => e.ErrorMessage);
            Assert.Contains(ValidationAttributeUtil.GetStringLengthErrorMessage(null, 5, "Street"), errorCollection);
            Assert.Contains(ValidationAttributeUtil.GetRegExErrorMessage("hehehe", "Street"), errorCollection);
        }

        [Fact]
        public void Validate_DoesNotUseOverridden_GetHashCodeOrEquals()
        {
            // Arrange
            var instance = new[] {
                    new TypeThatOverridesEquals { Funny = "hehe" },
                    new TypeThatOverridesEquals { Funny = "hehe" }
                };
            var validationContext = GetModelValidationContext(instance, typeof(TypeThatOverridesEquals[]));

            // Act & Assert
            Assert.DoesNotThrow(
                () => new DefaultBodyModelValidator().Validate(validationContext, keyPrefix: string.Empty));
        }

        private ModelValidationContext GetModelValidationContext(
            object model, Type type, List<Type> excludedTypes = null)
        {
            var modelStateDictionary = new ModelStateDictionary();
            var provider = new Mock<IModelValidatorProviderProvider>();
            provider.SetupGet(p => p.ModelValidatorProviders)
                    .Returns(new IModelValidatorProvider[]
                    {
                       new DataAnnotationsModelValidatorProvider(),
                       new DataMemberModelValidatorProvider()
                    });
            var modelMetadataProvider = new EmptyModelMetadataProvider();
            List<ExcludeFromValidationDelegate> excludedValidationTypesPredicate =
                new List<ExcludeFromValidationDelegate>();
            if (excludedTypes != null)
            {
                excludedValidationTypesPredicate = new List<ExcludeFromValidationDelegate>()
                {
                    (excludedType) =>
                    {
                        return excludedTypes.Any(t => t.IsAssignableFrom(excludedType));
                    }
                };
            }

            return new ModelValidationContext(
                modelMetadataProvider,
                new CompositeModelValidatorProvider(provider.Object),
                modelStateDictionary,
                new ModelMetadata(
                    provider: modelMetadataProvider,
                    containerType: typeof(object),
                    modelAccessor: () => model,
                    modelType: type,
                    propertyName: null),
                containerMetadata: null,
                excludeFromValidationDelegate: excludedValidationTypesPredicate);
        }

        public class Person
        {
            [Required, StringLength(10)]
            public string Name { get; set; }

            [Required]
            public string Profession { get; set; }

            public Person Friend { get; set; }
        }

        public class Address
        {
            [StringLength(5)]
            [RegularExpression("hehehe")]
            public string Street { get; set; }
        }

        public struct ValueType
        {
            public int Value;
            public string Reference;
        }

        public class ReferenceType
        {
            public static string StaticProperty { get { return "static"; } }
            public int Value;
            public string Reference;
        }

        public class Pet
        {
            [Required]
            public Person Owner { get; set; }
        }

        public class ValidatableModel : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                yield return new ValidationResult("Error1", new string[] { });
                yield return new ValidationResult("Error2", new[] { "Property1" });
                yield return new ValidationResult("Error3", new[] { "Property2", "Property3" });
            }
        }

        public class TypeThatOverridesEquals
        {
            [StringLength(2)]
            public string Funny { get; set; }

            public override bool Equals(object obj)
            {
                throw new InvalidOperationException();
            }

            public override int GetHashCode()
            {
                throw new InvalidOperationException();
            }
        }

        public class VariableTest
        {
            [Range(15, 25)]
            public int test;
        }

        public class Team
        {
            [Required]
            public int Id { get; set; }

            [Required]
            [StringLength(20, MinimumLength = 4)]
            public string TeamName { get; set; }

            [MaxLength(10)]
            public string Lead { get; set; }

            [Range(3, 100)]
            public int TeamSize { get; set; }

            public string TeamDescription { get; set; }
        }

        public class Org
        {
            [Required]
            public int Id { get; set; }

            [StringLength(20, MinimumLength = 4)]
            public string OrgName { get; set; }

            [Required]
            public Team Dev { get; set; }

            public Team Test { get; set; }
        }

        public class DerivedUri : Uri
        {
            public DerivedUri(string uri, UriKind kind) :base(uri, kind)
            {
            }

            [Required]
            public string UriPurpose { get; set; }
        }
    }
}
#endif