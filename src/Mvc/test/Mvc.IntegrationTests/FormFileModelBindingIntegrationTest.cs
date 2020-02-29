// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public class FormFileModelBindingIntegrationTest
    {
        private class Person
        {
            public Address Address { get; set; }
        }

        private class Address
        {
            public int Zip { get; set; }

            public IFormFile File { get; set; }
        }

        [Fact]
        public async Task BindProperty_WithData_WithEmptyPrefix_GetsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Person)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = QueryString.Create("Address.Zip", "12345");
                    UpdateRequest(request, data, "Address.File");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
            Assert.NotNull(boundPerson.Address);
            var file = Assert.IsAssignableFrom<IFormFile>(boundPerson.Address.File);
            Assert.Equal("form-data; name=Address.File; filename=text.txt", file.ContentDisposition);
            var reader = new StreamReader(boundPerson.Address.File.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Equal(2, modelState.Count);
            Assert.Single(modelState.Keys, k => k == "Address.Zip");
            var key = Assert.Single(modelState.Keys, k => k == "Address.File");
            Assert.Null(modelState[key].RawValue);
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        [Fact]
        public async Task BindProperty_WithOnlyFormFile_WithEmptyPrefix()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Person)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    UpdateRequest(request, data, "Address.File");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
            Assert.NotNull(boundPerson.Address);
            var file = Assert.IsAssignableFrom<IFormFile>(boundPerson.Address.File);
            Assert.Equal("form-data; name=Address.File; filename=text.txt", file.ContentDisposition);
            using var reader = new StreamReader(boundPerson.Address.File.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Collection(
                modelState.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    var (key, value) = kvp;
                    Assert.Equal("Address.File", kvp.Key);
                    Assert.Null(value.RawValue);
                    Assert.Empty(value.Errors);
                    Assert.Equal(ModelValidationState.Valid, value.ValidationState);
                });
        }

        [Fact]
        public async Task BindProperty_WithOnlyFormFile_WithPrefix()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Person)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    UpdateRequest(request, data, "Parameter1.Address.File");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
            Assert.NotNull(boundPerson.Address);
            var file = Assert.IsAssignableFrom<IFormFile>(boundPerson.Address.File);
            Assert.Equal("form-data; name=Parameter1.Address.File; filename=text.txt", file.ContentDisposition);
            using var reader = new StreamReader(boundPerson.Address.File.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Collection(
                modelState.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    var (key, value) = kvp;
                    Assert.Equal("Parameter1.Address.File", kvp.Key);
                    Assert.Null(value.RawValue);
                    Assert.Empty(value.Errors);
                    Assert.Equal(ModelValidationState.Valid, value.ValidationState);
                });
        }

        private class Group
        {
            public string GroupName { get; set; }

            public Person Person { get; set; }
        }

        [Fact]
        public async Task BindProperty_OnFormFileInNestedSubClass_AtSecondLevel_WhenSiblingPropertyIsSpecified()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Group)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = QueryString.Create("Person.Address.Zip", "98056");
                    UpdateRequest(request, data, "Person.Address.File");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var group = Assert.IsType<Group>(modelBindingResult.Model);
            Assert.Null(group.GroupName);
            var boundPerson = group.Person;
            Assert.NotNull(boundPerson);
            Assert.NotNull(boundPerson.Address);
            var file = Assert.IsAssignableFrom<IFormFile>(boundPerson.Address.File);
            Assert.Equal("form-data; name=Person.Address.File; filename=text.txt", file.ContentDisposition);
            using var reader = new StreamReader(boundPerson.Address.File.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());
            Assert.Equal(98056, boundPerson.Address.Zip);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Collection(
                modelState.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    var (key, value) = kvp;
                    Assert.Equal("Person.Address.File", kvp.Key);
                    Assert.Null(value.RawValue);
                    Assert.Empty(value.Errors);
                    Assert.Equal(ModelValidationState.Valid, value.ValidationState);
                },
                kvp =>
                {
                    var (key, value) = kvp;
                    Assert.Equal("Person.Address.Zip", kvp.Key);
                    Assert.Equal("98056", value.RawValue);
                    Assert.Empty(value.Errors);
                    Assert.Equal(ModelValidationState.Valid, value.ValidationState);
                });
        }

        private class Fleet
        {
            public int? Id { get; set; }

            public FleetGarage Garage { get; set; }
        }

        public class FleetGarage
        {
            public string Name { get; set; }

            public FleetVehicle[] Vehicles { get; set; }
        }

        public class FleetVehicle
        {
            public string Name { get; set; }

            public IFormFile Spec { get; set; }

            public FleetVehicle BackupVehicle { get; set; }
        }

        [Fact]
        public async Task BindProperty_OnFormFileInNestedSubClass_AtSecondLevel_RecursiveModel()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "fleet",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Fleet)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = QueryString.Create("fleet.Garage.Name", "WestEnd");
                    UpdateRequest(request, data, "fleet.Garage.Vehicles[0].Spec");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var fleet = Assert.IsType<Fleet>(modelBindingResult.Model);
            Assert.Null(fleet.Id);

            Assert.NotNull(fleet.Garage);
            Assert.NotNull(fleet.Garage.Vehicles);

            var vehicle = Assert.Single(fleet.Garage.Vehicles);
            var file = Assert.IsAssignableFrom<IFormFile>(vehicle.Spec);

            using var reader = new StreamReader(file.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());
            Assert.Null(vehicle.Name);
            Assert.Null(vehicle.BackupVehicle);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Collection(
                modelState.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    var (key, value) = kvp;
                    Assert.Equal("fleet.Garage.Name", kvp.Key);
                    Assert.Equal("WestEnd", value.RawValue);
                    Assert.Empty(value.Errors);
                    Assert.Equal(ModelValidationState.Valid, value.ValidationState);
                },
                kvp =>
                {
                    var (key, value) = kvp;
                    Assert.Equal("fleet.Garage.Vehicles[0].Spec", kvp.Key);
                    Assert.Equal(ModelValidationState.Valid, value.ValidationState);
                });
        }

        [Fact]
        public async Task BindProperty_OnFormFileInNestedSubClass_AtThirdLevel_RecursiveModel()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "fleet",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Fleet)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = QueryString.Create("fleet.Garage.Name", "WestEnd");
                    UpdateRequest(request, data, "fleet.Garage.Vehicles[0].BackupVehicle.Spec");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var fleet = Assert.IsType<Fleet>(modelBindingResult.Model);
            Assert.Null(fleet.Id);

            Assert.NotNull(fleet.Garage);
            Assert.NotNull(fleet.Garage.Vehicles);

            var vehicle = Assert.Single(fleet.Garage.Vehicles);
            Assert.Null(vehicle.Spec);
            Assert.NotNull(vehicle.BackupVehicle);
            var file = Assert.IsAssignableFrom<IFormFile>(vehicle.BackupVehicle.Spec);

            using var reader = new StreamReader(file.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());
            Assert.Null(vehicle.Name);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Collection(
                modelState.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    var (key, value) = kvp;
                    Assert.Equal("fleet.Garage.Name", kvp.Key);
                    Assert.Equal("WestEnd", value.RawValue);
                    Assert.Empty(value.Errors);
                    Assert.Equal(ModelValidationState.Valid, value.ValidationState);
                },
                kvp =>
                {
                    var (key, value) = kvp;
                    Assert.Equal("fleet.Garage.Vehicles[0].BackupVehicle.Spec", kvp.Key);
                    Assert.Equal(ModelValidationState.Valid, value.ValidationState);
                });
        }

        [Fact]
        public async Task BindProperty_OnFormFileInNestedSubClass_AtSecondLevel_WhenSiblingPropertiesAreNotSpecified()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Group)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = QueryString.Create("GroupName", "TestGroup");
                    UpdateRequest(request, data, "Person.Address.File");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var group = Assert.IsType<Group>(modelBindingResult.Model);
            Assert.Equal("TestGroup", group.GroupName);
            var boundPerson = group.Person;
            Assert.NotNull(boundPerson);
            Assert.NotNull(boundPerson.Address);
            var file = Assert.IsAssignableFrom<IFormFile>(boundPerson.Address.File);
            Assert.Equal("form-data; name=Person.Address.File; filename=text.txt", file.ContentDisposition);
            using var reader = new StreamReader(boundPerson.Address.File.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());
            Assert.Equal(0, boundPerson.Address.Zip);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Collection(
                modelState.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    var (key, value) = kvp;
                    Assert.Equal("GroupName", kvp.Key);
                    Assert.Equal("TestGroup", value.RawValue);
                    Assert.Empty(value.Errors);
                    Assert.Equal(ModelValidationState.Valid, value.ValidationState);
                },
                kvp =>
                {
                    var (key, value) = kvp;
                    Assert.Equal("Person.Address.File", kvp.Key);
                    Assert.Null(value.RawValue);
                    Assert.Empty(value.Errors);
                    Assert.Equal(ModelValidationState.Valid, value.ValidationState);
                });
        }

        private class ListContainer1
        {
            [ModelBinder(Name = "files")]
            public List<IFormFile> ListProperty { get; set; }
        }

        [Fact]
        public async Task BindCollectionProperty_WithData_IsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(ListContainer1),
            };

            var data = "some data";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request => UpdateRequest(request, data, "files"));
            var modelState = testContext.ModelState;

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);

            // Model
            var boundContainer = Assert.IsType<ListContainer1>(result.Model);
            Assert.NotNull(boundContainer);
            Assert.NotNull(boundContainer.ListProperty);
            var file = Assert.Single(boundContainer.ListProperty);
            Assert.Equal("form-data; name=files; filename=text.txt", file.ContentDisposition);
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                Assert.Equal(data, reader.ReadToEnd());
            }

            // ModelState
            Assert.True(modelState.IsValid);
            var kvp = Assert.Single(modelState);
            Assert.Equal("files", kvp.Key);
            var modelStateEntry = kvp.Value;
            Assert.NotNull(modelStateEntry);
            Assert.Empty(modelStateEntry.Errors);
            Assert.Equal(ModelValidationState.Valid, modelStateEntry.ValidationState);
            Assert.Null(modelStateEntry.AttemptedValue);
            Assert.Null(modelStateEntry.RawValue);
        }

        [Fact]
        public async Task BindCollectionProperty_NoData_IsNotBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(ListContainer1),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request => UpdateRequest(request, data: null, name: null));
            var modelState = testContext.ModelState;

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);

            // Model (bound to an empty collection)
            var boundContainer = Assert.IsType<ListContainer1>(result.Model);
            Assert.NotNull(boundContainer);
            Assert.Null(boundContainer.ListProperty);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        private class ListContainer2
        {
            [ModelBinder(Name = "files")]
            public List<IFormFile> ListProperty { get; } = new List<IFormFile>
            {
                new FormFile(new MemoryStream(), baseStreamOffset: 0, length: 0, name: "file", fileName: "file1"),
                new FormFile(new MemoryStream(), baseStreamOffset: 0, length: 0, name: "file", fileName: "file2"),
                new FormFile(new MemoryStream(), baseStreamOffset: 0, length: 0, name: "file", fileName: "file3"),
            };
        }

        [Fact]
        public async Task BindReadOnlyCollectionProperty_WithData_IsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(ListContainer2),
            };

            var data = "some data";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request => UpdateRequest(request, data, "files"));
            var modelState = testContext.ModelState;

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);

            // Model
            var boundContainer = Assert.IsType<ListContainer2>(result.Model);
            Assert.NotNull(boundContainer);
            Assert.NotNull(boundContainer.ListProperty);
            var file = Assert.Single(boundContainer.ListProperty);
            Assert.Equal("form-data; name=files; filename=text.txt", file.ContentDisposition);
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                Assert.Equal(data, reader.ReadToEnd());
            }

            // ModelState
            Assert.True(modelState.IsValid);
            var kvp = Assert.Single(modelState);
            Assert.Equal("files", kvp.Key);
            var modelStateEntry = kvp.Value;
            Assert.NotNull(modelStateEntry);
            Assert.Empty(modelStateEntry.Errors);
            Assert.Equal(ModelValidationState.Valid, modelStateEntry.ValidationState);
            Assert.Null(modelStateEntry.AttemptedValue);
            Assert.Null(modelStateEntry.RawValue);
        }

        [Fact]
        public async Task BindParameter_WithData_GetsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo
                {
                    // Setting a custom parameter prevents it from falling back to an empty prefix.
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(IFormFile)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    UpdateRequest(request, data, "CustomParameter");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var file = Assert.IsType<FormFile>(modelBindingResult.Model);
            Assert.NotNull(file);
            Assert.Equal("form-data; name=CustomParameter; filename=text.txt", file.ContentDisposition);
            var reader = new StreamReader(file.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());

            // ModelState
            Assert.True(modelState.IsValid);
            var entry = Assert.Single(modelState);
            Assert.Equal("CustomParameter", entry.Key);
            Assert.Empty(entry.Value.Errors);
            Assert.Equal(ModelValidationState.Valid, entry.Value.ValidationState);
            Assert.Null(entry.Value.AttemptedValue);
            Assert.Null(entry.Value.RawValue);
        }

        [Fact]
        public async Task BindParameter_NoData_DoesNotGetBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                },

                ParameterType = typeof(IFormFile)
            };

            // No data is passed.
            var testContext = ModelBindingTestHelper.GetTestContext(
                request => UpdateRequest(request, data: null, name: null));

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.False(modelBindingResult.IsModelSet);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState.Keys);
        }

        private class Car1
        {
            public string Name { get; set; }

            public FormFileCollection Specs { get; set; }
        }

        [Fact]
        public async Task BindProperty_WithData_WithPrefix_GetsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "p",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Car1)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = QueryString.Create("p.Name", "Accord");
                    UpdateRequest(request, data, "p.Specs");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var car = Assert.IsType<Car1>(modelBindingResult.Model);
            Assert.NotNull(car.Specs);
            var file = Assert.Single(car.Specs);
            Assert.Equal("form-data; name=p.Specs; filename=text.txt", file.ContentDisposition);
            var reader = new StreamReader(file.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Equal(2, modelState.Count);

            var entry = Assert.Single(modelState, e => e.Key == "p.Name").Value;
            Assert.Equal("Accord", entry.AttemptedValue);
            Assert.Equal("Accord", entry.RawValue);

            Assert.Single(modelState, e => e.Key == "p.Specs");
        }

        private class House
        {
            public Garage Garage { get; set; }
        }

        private class Garage
        {
            public List<Car1> Cars { get; set; }
        }

        [Fact]
        public async Task BindProperty_FormFileCollectionInCollection_WithPrefix()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "house",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(House)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = QueryString.Create("house.Garage.Cars[0].Name", "Accord");
                    UpdateRequest(request, data + 1, "house.Garage.Cars[0].Specs");
                    AddFormFile(request, data + 2, "house.Garage.Cars[1].Specs");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var house = Assert.IsType<House>(modelBindingResult.Model);
            Assert.NotNull(house.Garage);
            Assert.NotNull(house.Garage.Cars);
            Assert.Collection(
                house.Garage.Cars,
                car =>
                {
                    Assert.Equal("Accord", car.Name);

                    var file = Assert.Single(car.Specs);
                    using var reader = new StreamReader(file.OpenReadStream());
                    Assert.Equal(data + 1, reader.ReadToEnd());
                },
                car =>
                {
                    Assert.Null(car.Name);

                    var file = Assert.Single(car.Specs);
                    using var reader = new StreamReader(file.OpenReadStream());
                    Assert.Equal(data + 2, reader.ReadToEnd());
                });

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Equal(3, modelState.Count);

            var entry = Assert.Single(modelState, e => e.Key == "house.Garage.Cars[0].Name").Value;
            Assert.Equal("Accord", entry.AttemptedValue);
            Assert.Equal("Accord", entry.RawValue);

            Assert.Single(modelState, e => e.Key == "house.Garage.Cars[0].Specs");
            Assert.Single(modelState, e => e.Key == "house.Garage.Cars[1].Specs");
        }

        [Fact]
        public async Task BindProperty_FormFileCollectionInCollection_OnlyFiles()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "house",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(House)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    UpdateRequest(request, data + 1, "house.Garage.Cars[0].Specs");
                    AddFormFile(request, data + 2, "house.Garage.Cars[1].Specs");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var house = Assert.IsType<House>(modelBindingResult.Model);
            Assert.NotNull(house.Garage);
            Assert.NotNull(house.Garage.Cars);
            Assert.Collection(
                house.Garage.Cars,
                car =>
                {
                    Assert.Null(car.Name);

                    var file = Assert.Single(car.Specs);
                    using var reader = new StreamReader(file.OpenReadStream());
                    Assert.Equal(data + 1, reader.ReadToEnd());
                },
                car =>
                {
                    Assert.Null(car.Name);

                    var file = Assert.Single(car.Specs);
                    using var reader = new StreamReader(file.OpenReadStream());
                    Assert.Equal(data + 2, reader.ReadToEnd());
                });

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Equal(2, modelState.Count);

            Assert.Single(modelState, e => e.Key == "house.Garage.Cars[0].Specs");
            Assert.Single(modelState, e => e.Key == "house.Garage.Cars[1].Specs");
        }

        [Fact]
        public async Task BindProperty_FormFileCollectionInCollection_OutOfOrderFile()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "house",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(House)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    UpdateRequest(request, data + 1, "house.Garage.Cars[800].Specs");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var house = Assert.IsType<House>(modelBindingResult.Model);
            Assert.NotNull(house.Garage);
            Assert.Empty(house.Garage.Cars);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        [Fact]
        public async Task BindProperty_FormFileCollectionInCollection_MultipleFiles()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "house",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(House)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    UpdateRequest(request, data + 1, "house.Garage.Cars[0].Specs");
                    AddFormFile(request, data + 2, "house.Garage.Cars[0].Specs");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var house = Assert.IsType<House>(modelBindingResult.Model);
            Assert.NotNull(house.Garage);
            Assert.NotNull(house.Garage.Cars);
            Assert.Collection(
                house.Garage.Cars,
                car =>
                {
                    Assert.Null(car.Name);
                    Assert.Collection(
                        car.Specs,
                        file =>
                        {
                            using var reader = new StreamReader(file.OpenReadStream());
                            Assert.Equal(data + 1, reader.ReadToEnd());

                        },
                        file =>
                        {
                            using var reader = new StreamReader(file.OpenReadStream());
                            Assert.Equal(data + 2, reader.ReadToEnd());

                        });
                });

            // ModelState
            Assert.True(modelState.IsValid);
            var kvp = Assert.Single(modelState);

            Assert.Equal("house.Garage.Cars[0].Specs", kvp.Key);
        }

        [Fact]
        public async Task BindProperty_FormFile_AsAPropertyOnNestedColection()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "p",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Car1)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = QueryString.Create("p.Name", "Accord");
                    UpdateRequest(request, data, "p.Specs");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var car = Assert.IsType<Car1>(modelBindingResult.Model);
            Assert.NotNull(car.Specs);
            var file = Assert.Single(car.Specs);
            Assert.Equal("form-data; name=p.Specs; filename=text.txt", file.ContentDisposition);
            var reader = new StreamReader(file.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Equal(2, modelState.Count);

            var entry = Assert.Single(modelState, e => e.Key == "p.Name").Value;
            Assert.Equal("Accord", entry.AttemptedValue);
            Assert.Equal("Accord", entry.RawValue);

            Assert.Single(modelState, e => e.Key == "p.Specs");
        }

        public class MultiDimensionalFormFileContainer
        {
            public IFormFile[][] FormFiles { get; set; }
        }

        [Fact]
        public async Task BindModelAsync_MultiDimensionalFormFile_Works()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "p",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(MultiDimensionalFormFileContainer)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    UpdateRequest(request, data + 1, "FormFiles[0]");
                    AddFormFile(request, data + 2, "FormFiles[1]");
                    AddFormFile(request, data + 3, "FormFiles[1]");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var container = Assert.IsType<MultiDimensionalFormFileContainer>(modelBindingResult.Model);
            Assert.NotNull(container.FormFiles);
            Assert.Collection(
                container.FormFiles,
                item =>
                {
                    Assert.Collection(
                        item,
                        file => Assert.Equal(data + 1, ReadFormFile(file)));
                },
                item =>
                {
                    Assert.Collection(
                        item,
                        file => Assert.Equal(data + 2, ReadFormFile(file)),
                        file => Assert.Equal(data + 3, ReadFormFile(file)));
                });
        }

        [Fact]
        public async Task BindModelAsync_MultiDimensionalFormFile_WithArrayNotation()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "p",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(MultiDimensionalFormFileContainer)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    UpdateRequest(request, data + 1, "FormFiles[0][0]");
                    AddFormFile(request, data + 2, "FormFiles[1][0]");
                    AddFormFile(request, data + 3, "FormFiles[1][0]");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);
            var container = Assert.IsType<MultiDimensionalFormFileContainer>(modelBindingResult.Model);
            Assert.NotNull(container.FormFiles);
            Assert.Empty(container.FormFiles);
        }

        public class MultiDimensionalFormFileContainerLevel2
        {
            public MultiDimensionalFormFileContainerLevel1 Level1 { get; set; }
        }

        public class MultiDimensionalFormFileContainerLevel1
        {
            public MultiDimensionalFormFileContainer Container { get; set; }
        }

        [Fact]
        public async Task BindModelAsync_DeeplyNestedMultiDimensionalFormFile_Works()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "p",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(MultiDimensionalFormFileContainerLevel2)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    UpdateRequest(request, data + 1, "p.Level1.Container.FormFiles[0]");
                    AddFormFile(request, data + 2, "p.Level1.Container.FormFiles[1]");
                    AddFormFile(request, data + 3, "p.Level1.Container.FormFiles[1]");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var level2 = Assert.IsType<MultiDimensionalFormFileContainerLevel2>(modelBindingResult.Model);
            Assert.NotNull(level2.Level1);
            var container = level2.Level1.Container;
            Assert.NotNull(container);
            Assert.NotNull(container.FormFiles);
            Assert.Collection(
                container.FormFiles,
                item =>
                {
                    Assert.Collection(
                        item,
                        file => Assert.Equal(data + 1, ReadFormFile(file)));
                },
                item =>
                {
                    Assert.Collection(
                        item,
                        file => Assert.Equal(data + 2, ReadFormFile(file)),
                        file => Assert.Equal(data + 3, ReadFormFile(file)));
                });
        }

        public class DictionaryContainer
        {
            public Dictionary<string, IFormFile> Dictionary { get; set; }
        }

        [Fact]
        public async Task BindModelAsync_DictionaryOfFormFiles()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "p",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(DictionaryContainer)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = QueryString.Create(new Dictionary<string, string>
                    {
                        { "p.Dictionary[0].Key", "key0" },
                        { "p.Dictionary[1].Key", "key1" },
                        { "p.Dictionary[4000].Key", "key1" },
                    });
                    UpdateRequest(request, data + 1, "p.Dictionary[0].Value");
                    AddFormFile(request, data + 2, "p.Dictionary[1].Value");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var container = Assert.IsType<DictionaryContainer>(modelBindingResult.Model);
            Assert.NotNull(container.Dictionary);
            Assert.Collection(
                container.Dictionary.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("key0", kvp.Key);
                    Assert.Equal(data + 1, ReadFormFile(kvp.Value));
                },
                kvp =>
                {
                    Assert.Equal("key1", kvp.Key);
                    Assert.Equal(data + 2, ReadFormFile(kvp.Value));
                });
        }

        private static string ReadFormFile(IFormFile file)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            return reader.ReadToEnd();
        }

        private void UpdateRequest(HttpRequest request, string data, string name)
        {
            var formCollection = new FormCollection(new Dictionary<string, StringValues>(), new FormFileCollection());
            request.Form = formCollection;

            request.ContentType = "multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq";

            AddFormFile(request, data, name);
        }

        private void AddFormFile(HttpRequest request, string data, string name)
        {
            const string fileName = "text.txt";

            if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(name))
            {
                // Leave the submission empty.
                return;
            }

            request.Headers["Content-Disposition"] = $"form-data; name={name}; filename={fileName}";

            var fileCollection = (FormFileCollection)request.Form.Files;
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            fileCollection.Add(new FormFile(memoryStream, 0, data.Length, name, fileName)
            {
                Headers = request.Headers
            });
        }
    }
}
