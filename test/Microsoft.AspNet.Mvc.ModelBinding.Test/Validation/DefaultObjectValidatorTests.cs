// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DefaultObjectValidatorTests
    {
        private static Person LonelyPerson;

        static DefaultObjectValidatorTests()
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
            var context = GetModelValidationContext(model, type);

            var validator = new DefaultObjectValidator(context.ExcludeFiltersProvider, context.ModelMetadataProvider);

            // Act
            validator.Validate(context.ModelValidationContext);

            // Assert
            var actualErrors = new Dictionary<string, string>();
            foreach (var keyStatePair in context.ModelValidationContext.ModelState)
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
        public void Validator_Throws_IfPropertyAccessorThrows()
        {
            // Arrange
            var testValidationContext = GetModelValidationContext(new Uri("/api/values", UriKind.Relative), typeof(Uri));

            // Act & Assert
            Assert.Throws(
                typeof(InvalidOperationException),
                () =>
                {
                    new DefaultObjectValidator(
                        testValidationContext.ExcludeFiltersProvider,
                        testValidationContext.ModelMetadataProvider)
                        .Validate(testValidationContext.ModelValidationContext);
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
                yield return new object[] { new Dictionary<string, Uri> {
                    { "values",  new Uri("/api/values", UriKind.Relative) },
                    { "hello",  new Uri("/api/hello", UriKind.Relative) }
                }, typeof(Uri), new List<Type>() { typeof(Uri) } };
            }
        }

        [Theory]
        [MemberData(nameof(ObjectsWithPropertiesWhichThrowOnGet))]
        [ReplaceCulture]
        public void Validator_DoesNotThrow_IfExcludedPropertyAccessorsThrow(
            object input, Type type, List<Type> excludedTypes)
        {
            // Arrange
            var testValidationContext = GetModelValidationContext(input, type, string.Empty, excludedTypes);

            // Act & Assert (does not throw)
            new DefaultObjectValidator(
                testValidationContext.ExcludeFiltersProvider,
                testValidationContext.ModelMetadataProvider)
                .Validate(testValidationContext.ModelValidationContext);
            Assert.True(testValidationContext.ModelValidationContext.ModelState.IsValid);
        }

        [Fact]
        [ReplaceCulture]
        public void Validator_Throws_IfPropertyGetterThrows()
        {
            // Arrange
            var testValidationContext = GetModelValidationContext(
                new Uri("/api/values", UriKind.Relative), typeof(Uri));
            var validationContext = testValidationContext.ModelValidationContext;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    new DefaultObjectValidator(
                        testValidationContext.ExcludeFiltersProvider,
                        testValidationContext.ModelMetadataProvider)
                        .Validate(validationContext);
                });
            Assert.True(validationContext.ModelState.IsValid);
        }

        [Fact]
        [ReplaceCulture]
        public void MultipleValidationErrorsOnSameMemberReported()
        {
            // Arrange
            var model = new Address() { Street = "Microsoft Way" };
            var testValidationContext = GetModelValidationContext(model, model.GetType());
            var validationContext = testValidationContext.ModelValidationContext;

            // Act (does not throw)
            new DefaultObjectValidator(
                testValidationContext.ExcludeFiltersProvider,
                testValidationContext.ModelMetadataProvider)
                .Validate(validationContext);

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
            var testValidationContext = GetModelValidationContext(instance, typeof(TypeThatOverridesEquals[]));

            // Act & Assert (does not throw)
            new DefaultObjectValidator(
                testValidationContext.ExcludeFiltersProvider,
                testValidationContext.ModelMetadataProvider)
                .Validate(testValidationContext.ModelValidationContext);
        }

        [Fact]
        public void Validation_ShortCircuit_WhenMaxErrorCountIsSet()
        {
            // Arrange
            var user = new User()
            {
                Password = "password-val",
                ConfirmPassword = "not-password-val"
            };

            var testValidationContext = GetModelValidationContext(
                user,
                typeof(User),
                "user",
                new List<Type> { typeof(string) });

            var validationContext = testValidationContext.ModelValidationContext;
            validationContext.ModelState.MaxAllowedErrors = 2;
            validationContext.ModelState.AddModelError("key1", "error1");
            var validator = new DefaultObjectValidator(
                testValidationContext.ExcludeFiltersProvider,
                testValidationContext.ModelMetadataProvider);

            // Act
            validator.Validate(validationContext);

            // Assert
            Assert.Equal(new[] { "key1", "user.Password", "", "user.ConfirmPassword" },
                validationContext.ModelState.Keys.ToArray());
            var modelState = validationContext.ModelState["user.ConfirmPassword"];
            Assert.Empty(modelState.Errors);
            Assert.Equal(modelState.ValidationState, ModelValidationState.Skipped);
            
            var error = Assert.Single(validationContext.ModelState[""].Errors);
            Assert.IsType<TooManyModelErrorsException>(error.Exception);
        }

        [Fact]
        public void ForExcludedNonModelBoundType_Properties_NotMarkedAsSkiped()
        {
            // Arrange
            var user = new User()
            {
                Password = "password-val",
                ConfirmPassword = "not-password-val"
            };

            var testValidationContext = GetModelValidationContext(
                user,
                typeof(User),
                "user",
                new List<Type> { typeof(User) });
            var validationContext = testValidationContext.ModelValidationContext;
            var validator = new DefaultObjectValidator(
                testValidationContext.ExcludeFiltersProvider,
                testValidationContext.ModelMetadataProvider);

            // Act
            validator.Validate(validationContext);

            // Assert
            Assert.False(validationContext.ModelState.ContainsKey("user.Password"));
            Assert.False(validationContext.ModelState.ContainsKey("user.ConfirmPassword"));
            var modelState = validationContext.ModelState["user"];
            Assert.Empty(modelState.Errors);
            Assert.Equal(modelState.ValidationState, ModelValidationState.Valid);
        }

        [Fact]
        public void ForExcludedModelBoundTypes_Properties_MarkedAsSkipped()
        {
            // Arrange
            var user = new User()
            {
                Password = "password-val",
                ConfirmPassword = "not-password-val"
            };

            var testValidationContext = GetModelValidationContext(
                user,
                typeof(User),
                "user",
                new List<Type> { typeof(User) });
            var validationContext = testValidationContext.ModelValidationContext;

            // Set the value on model state as a model binder would.
            validationContext.ModelState.SetModelValue(
                "user.Password",
                Mock.Of<ValueProviderResult>());
            var validator = new DefaultObjectValidator(
                testValidationContext.ExcludeFiltersProvider,
                testValidationContext.ModelMetadataProvider);

            // Act
            validator.Validate(validationContext);

            // Assert
            var modelState = validationContext.ModelState["user.Password"];
            Assert.Empty(modelState.Errors);
            Assert.Equal(modelState.ValidationState, ModelValidationState.Skipped);

            modelState = validationContext.ModelState["user.ConfirmPassword"];
            Assert.Empty(modelState.Errors);
            Assert.Equal(modelState.ValidationState, ModelValidationState.Skipped);
        }

        [Fact]
        public void NonRequestBoundModel_MarkedAsSkipped()
        {
            // Arrange
            var testValidationContext = GetModelValidationContext(
                new TestServiceProvider(),
                typeof(TestServiceProvider),
                "serviceProvider");

            var validationContext = testValidationContext.ModelValidationContext;
            var validator = new DefaultObjectValidator(
                testValidationContext.ExcludeFiltersProvider, 
                testValidationContext.ModelMetadataProvider);

            // Act
            validator.Validate(validationContext);

            // Assert
            var modelState = validationContext.ModelState["serviceProvider.TestService"];
            Assert.Empty(modelState.Errors);
            Assert.Equal(modelState.ValidationState, ModelValidationState.Skipped);
        }

        private TestModelValidationContext GetModelValidationContext(
            object model,
            Type type,
            string key = "",
            List<Type> excludedTypes = null)
        {
            var modelStateDictionary = new ModelStateDictionary();

            var providers = new IModelValidatorProvider[]
            {
                new DataAnnotationsModelValidatorProvider(),
                new DataMemberModelValidatorProvider()
            };

            var modelMetadataProvider = new DataAnnotationsModelMetadataProvider();

            var excludedValidationTypesPredicate = new List<IExcludeTypeValidationFilter>();
            if (excludedTypes != null)
            {
                var mockExcludeTypeFilter = new Mock<IExcludeTypeValidationFilter>();
                mockExcludeTypeFilter
                    .Setup(o => o.IsTypeExcluded(It.IsAny<Type>()))
                    .Returns<Type>(excludedType => excludedTypes.Any(t => t.IsAssignableFrom(excludedType)));

                excludedValidationTypesPredicate.Add(mockExcludeTypeFilter.Object);
            }

            var mockValidationExcludeFiltersProvider = new Mock<IValidationExcludeFiltersProvider>();
            mockValidationExcludeFiltersProvider
                .SetupGet(o => o.ExcludeFilters)
                .Returns(excludedValidationTypesPredicate);

            var modelExplorer = modelMetadataProvider.GetModelExplorerForType(type, model);

            return new TestModelValidationContext
            {
                ModelValidationContext = new ModelValidationContext(
                    key,
                    new CompositeModelValidatorProvider(providers),
                    modelStateDictionary,
                    modelExplorer),
                ModelMetadataProvider = modelMetadataProvider,
                ExcludeFiltersProvider = mockValidationExcludeFiltersProvider.Object
            };
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

        private class User : IValidatableObject
        {
            public string Password { get; set; }

            [Compare("Password")]
            public string ConfirmPassword { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (Password == "password")
                {
                    yield return new ValidationResult("Password does not meet complexity requirements.");
                }
            }
        }

        public class TestServiceProvider
        {
            [FromServices]
            [Required]
            public ITestService TestService { get; set; }
        }

        public interface ITestService
        {
        }

        private class TestModelValidationContext
        {
            public ModelValidationContext ModelValidationContext { get; set; }

            public IModelMetadataProvider ModelMetadataProvider { get; set; }

            public IValidationExcludeFiltersProvider ExcludeFiltersProvider { get; set; }
        }
    }
}
#endif