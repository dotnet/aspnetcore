// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Testing;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.DependencyInjection;
using ModelBindingWebSite.Models;
using ModelBindingWebSite.ViewModels;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ModelBindingTest
    {
        private const string SiteName = nameof(ModelBindingWebSite);
        private static readonly Assembly _assembly = typeof(ModelBindingTest).GetTypeInfo().Assembly;

        private readonly Action<IApplicationBuilder> _app = new ModelBindingWebSite.Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new ModelBindingWebSite.Startup().ConfigureServices;

        [Fact]
        public async Task DoNotValidate_ParametersOrControllerProperties_IfSourceNotFromRequest()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/Validation/DoNotValidateParameter");

            // Assert
            Assert.Equal("true", response);
        }

        [Fact]
        public async Task ModelValidation_DoesNotValidate_AnAlreadyValidatedObject()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/Validation/AvoidRecursive?Name=selfish");

            // Assert
            Assert.Equal("true", response);
        }

        [Theory]
        [InlineData("RestrictValueProvidersUsingFromRoute", "valueFromRoute")]
        [InlineData("RestrictValueProvidersUsingFromQuery", "valueFromQuery")]
        [InlineData("RestrictValueProvidersUsingFromForm", "valueFromForm")]
        public async Task CompositeModelBinder_Restricts_ValueProviders(string actionName, string expectedValue)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Provide all three values, it should bind based on the attribute on the action method.
            var request = new HttpRequestMessage(HttpMethod.Post,
                string.Format("http://localhost/CompositeTest/{0}/valueFromRoute?param=valueFromQuery", actionName));
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("param", "valueFromForm"),
            };

            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedValue, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task TryUpdateModel_WithAPropertyFromBody()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // the name would be of customer.Department.Name
            // and not for the top level customer object.
            var input = "{\"Name\":\"RandomDepartment\"}";
            var content = new StringContent(input, Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("http://localhost/Home/GetCustomer?Id=1234", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var customer = JsonConvert.DeserializeObject<Customer>(
                        await response.Content.ReadAsStringAsync());
            Assert.NotNull(customer.Department);
            Assert.Equal("RandomDepartment", customer.Department.Name);
            Assert.Equal(1234, customer.Id);
            Assert.Equal(25, customer.Age);
            Assert.Equal("dummy", customer.Name);
        }

        [Fact]
        public async Task CanModelBindServiceToAnArgument()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FromServices_Calculator/Add?left=1234&right=1");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("1235", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CanModelBindServiceToAProperty()
        {
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
                "http://localhost/FromServices_Calculator/Calculate?Left=10&Right=5&Operator=*");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("50", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CanModelBindServiceToAProperty_OnBaseType()
        {
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
               "http://localhost/FromServices_Calculator/CalculateWithPrecision?Left=10&Right=5&Operator=*");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("50", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ControllerPropertyAndAnActionWithoutFromBody_InvokesWithoutErrors()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FromBodyControllerProperty/GetSiteUser");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task CanBind_MultipleParameters_UsingFromForm()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Post,
                "http://localhost/FromAttributes/MultipleFromFormParameters");
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("homeAddress.Street", "1"),
                new KeyValuePair<string,string>("homeAddress.State", "WA_Form_Home"),
                new KeyValuePair<string,string>("homeAddress.Zip", "2"),
                new KeyValuePair<string,string>("officeAddress.Street", "3"),
                new KeyValuePair<string,string>("officeAddress.State", "WA_Form_Office"),
                new KeyValuePair<string,string>("officeAddress.Zip", "4"),
            };

            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var user = JsonConvert.DeserializeObject<User_FromForm>(
                      await response.Content.ReadAsStringAsync());

            Assert.Equal("WA_Form_Home", user.HomeAddress.State);
            Assert.Equal(1, user.HomeAddress.Street);
            Assert.Equal(2, user.HomeAddress.Zip);

            Assert.Equal("WA_Form_Office", user.OfficeAddress.State);
            Assert.Equal(3, user.OfficeAddress.Street);
            Assert.Equal(4, user.OfficeAddress.Zip);
        }

        [Fact]
        public async Task CanBind_MultipleProperties_UsingFromForm()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Post,
                "http://localhost/FromAttributes/MultipleFromFormParameterAndProperty");
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("Street", "1"),
                new KeyValuePair<string,string>("State", "WA_Form_Home"),
                new KeyValuePair<string,string>("Zip", "2"),
                new KeyValuePair<string,string>("officeAddress.Street", "3"),
                new KeyValuePair<string,string>("officeAddress.State", "WA_Form_Office"),
                new KeyValuePair<string,string>("officeAddress.Zip", "4"),
            };

            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var user = JsonConvert.DeserializeObject<User_FromForm>(
                      await response.Content.ReadAsStringAsync());

            Assert.Equal("WA_Form_Home", user.HomeAddress.State);
            Assert.Equal(1, user.HomeAddress.Street);
            Assert.Equal(2, user.HomeAddress.Zip);

            Assert.Equal("WA_Form_Office", user.OfficeAddress.State);
            Assert.Equal(3, user.OfficeAddress.Street);
            Assert.Equal(4, user.OfficeAddress.Zip);
        }

        [Fact]
        public async Task CanBind_ComplexData_OnParameters_UsingFromAttributes()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Provide all three values, it should bind based on the attribute on the action method.
            var request = new HttpRequestMessage(HttpMethod.Post,
                "http://localhost/FromAttributes/GetUser/5/WA_Route/6" +
                "?Street=3&State=WA_Query&Zip=4");
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("Street", "1"),
                new KeyValuePair<string,string>("State", "WA_Form"),
                new KeyValuePair<string,string>("Zip", "2"),
            };

            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var user = JsonConvert.DeserializeObject<User_FromForm>(
                       await response.Content.ReadAsStringAsync());

            // Assert FromRoute
            Assert.Equal("WA_Route", user.HomeAddress.State);
            Assert.Equal(5, user.HomeAddress.Street);
            Assert.Equal(6, user.HomeAddress.Zip);

            // Assert FromForm
            Assert.Equal("WA_Form", user.OfficeAddress.State);
            Assert.Equal(1, user.OfficeAddress.Street);
            Assert.Equal(2, user.OfficeAddress.Zip);

            // Assert FromQuery
            Assert.Equal("WA_Query", user.ShippingAddress.State);
            Assert.Equal(3, user.ShippingAddress.Street);
            Assert.Equal(4, user.ShippingAddress.Zip);
        }

        [Fact]
        public async Task CanBind_ComplexData_OnProperties_UsingFromAttributes()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Provide all three values, it should bind based on the attribute on the action method.
            var request = new HttpRequestMessage(HttpMethod.Post,
                "http://localhost/FromAttributes/GetUser_FromForm/5/WA_Route/6" +
                "?ShippingAddress.Street=3&ShippingAddress.State=WA_Query&ShippingAddress.Zip=4");
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("OfficeAddress.Street", "1"),
                new KeyValuePair<string,string>("OfficeAddress.State", "WA_Form"),
                new KeyValuePair<string,string>("OfficeAddress.Zip", "2"),
            };

            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var user = JsonConvert.DeserializeObject<User_FromForm>(
                       await response.Content.ReadAsStringAsync());

            // Assert FromRoute
            Assert.Equal("WA_Route", user.HomeAddress.State);
            Assert.Equal(5, user.HomeAddress.Street);
            Assert.Equal(6, user.HomeAddress.Zip);

            // Assert FromForm
            Assert.Equal("WA_Form", user.OfficeAddress.State);
            Assert.Equal(1, user.OfficeAddress.Street);
            Assert.Equal(2, user.OfficeAddress.Zip);

            // Assert FromQuery
            Assert.Equal("WA_Query", user.ShippingAddress.State);
            Assert.Equal(3, user.ShippingAddress.Street);
            Assert.Equal(4, user.ShippingAddress.Zip);
        }

        [Fact]
        public async Task CanBind_ComplexData_OnProperties_UsingFromAttributes_WithBody()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Provide all three values, it should bind based on the attribute on the action method.
            var request = new HttpRequestMessage(HttpMethod.Post,
                "http://localhost/FromAttributes/GetUser_FromBody/5/WA_Route/6" +
                "?ShippingAddress.Street=3&ShippingAddress.State=WA_Query&ShippingAddress.Zip=4");
            var input = "{\"State\":\"WA_Body\",\"Street\":1,\"Zip\":2}";

            request.Content = new StringContent(input, Encoding.UTF8, "application/json");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var user = JsonConvert.DeserializeObject<User_FromBody>(
                       await response.Content.ReadAsStringAsync());

            // Assert FromRoute
            Assert.Equal("WA_Route", user.HomeAddress.State);
            Assert.Equal(5, user.HomeAddress.Street);
            Assert.Equal(6, user.HomeAddress.Zip);

            // Assert FromBody
            Assert.Equal("WA_Body", user.OfficeAddress.State);
            Assert.Equal(1, user.OfficeAddress.Street);
            Assert.Equal(2, user.OfficeAddress.Zip);

            // Assert FromQuery
            Assert.Equal("WA_Query", user.ShippingAddress.State);
            Assert.Equal(3, user.ShippingAddress.Street);
            Assert.Equal(4, user.ShippingAddress.Zip);
        }


        [Fact]
        public async Task NonExistingModelBinder_ForABinderMetadata_DoesNotRecurseInfinitely()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act & Assert
            var response = await client.GetStringAsync("http://localhost/WithBinderMetadata/EchoDocument");

            var document = JsonConvert.DeserializeObject<Document>
                          (response);

            Assert.NotNull(document);
            Assert.Null(document.Version);
            Assert.Null(document.SubDocument);
        }

        [Fact]
        public async Task ParametersWithNoValueProviderMetadataUseTheAvailableValueProviders()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await
                     client.GetAsync("http://localhost/WithBinderMetadata" +
                     "/ParametersWithNoValueProviderMetadataUseTheAvailableValueProviders" +
                     "?Name=somename&Age=12");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var emp = JsonConvert.DeserializeObject<Employee>(
                            await response.Content.ReadAsStringAsync());
            Assert.Null(emp.Department);
            Assert.Equal("somename", emp.Name);
            Assert.Equal(12, emp.Age);
        }

        [Fact]
        public async Task ParametersAreAlwaysCreated_IfValuesAreProvidedWithoutModelName()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await
                     client.GetAsync("http://localhost/WithoutBinderMetadata" +
                     "/GetPersonParameter" +
                     "?Name=somename&Age=12");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var person = JsonConvert.DeserializeObject<Person>(
                            await response.Content.ReadAsStringAsync());
            Assert.NotNull(person);
            Assert.Equal("somename", person.Name);
            Assert.Equal(12, person.Age);
        }

        [Fact]
        public async Task ParametersAreAlwaysCreated_IfValueIsProvidedForModelName()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await
                     client.GetAsync("http://localhost/WithoutBinderMetadata" +
                     "/GetPersonParameter?p="); // here p is the model name.

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var person = JsonConvert.DeserializeObject<Person>(
                            await response.Content.ReadAsStringAsync());
            Assert.NotNull(person);
            Assert.Null(person.Name);
            Assert.Equal(0, person.Age);
        }

        [Fact]
        public async Task ParametersAreAlwaysCreated_IfNoValuesAreProvided()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await
                     client.GetAsync("http://localhost/WithoutBinderMetadata" +
                     "/GetPersonParameter");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var person = JsonConvert.DeserializeObject<Person>(
                            await response.Content.ReadAsStringAsync());
            Assert.NotNull(person);
            Assert.Null(person.Name);
            Assert.Equal(0, person.Age);
        }

        [Fact]
        public async Task PropertiesAreBound_IfTheyAreProvidedByValueProviders()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await
                     client.GetAsync("http://localhost/Properties" +
                     "/GetCompany?Employees[0].Name=somename&Age=12");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var company = JsonConvert.DeserializeObject<Company>(
                            await response.Content.ReadAsStringAsync());
            Assert.NotNull(company);
            Assert.NotNull(company.Employees);
            Assert.Equal(1, company.Employees.Count);
            Assert.NotNull(company.Employees[0].Name);
        }

        [Fact]
        public async Task PropertiesAreBound_IfTheyAreMarkedExplicitly()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await
                     client.GetAsync("http://localhost/Properties" +
                     "/GetCompany");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var company = JsonConvert.DeserializeObject<Company>(
                            await response.Content.ReadAsStringAsync());
            Assert.NotNull(company);
            Assert.NotNull(company.CEO);
            Assert.Null(company.CEO.Name);
        }

        [Fact]
        public async Task PropertiesAreBound_IfTheyArePocoMetadataMarkedTypes()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await
                     client.GetAsync("http://localhost/Properties" +
                     "/GetCompany");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var company = JsonConvert.DeserializeObject<Company>(
                            await response.Content.ReadAsStringAsync());
            Assert.NotNull(company);

            // Department property is not null because it was a marker poco.
            Assert.NotNull(company.Department);

            // beacause no value is provided.
            Assert.Null(company.Department.Name);
        }

        [Fact]
        public async Task PropertiesAreNotBound_ByDefault()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await
                     client.GetAsync("http://localhost/Properties" +
                     "/GetCompany");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var company = JsonConvert.DeserializeObject<Company>(
                            await response.Content.ReadAsStringAsync());
            Assert.NotNull(company);
            Assert.Null(company.Employees);
        }

        [Fact]
        public async Task PocoGetsCreated_IfTopLevelNoProperties()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await
                     client.GetAsync("http://localhost/Properties" +
                     "/GetPerson");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var person = JsonConvert.DeserializeObject<PersonWithNoProperties>(
                            await response.Content.ReadAsStringAsync());
            Assert.NotNull(person);
            Assert.Null(person.Name);
        }

        [Fact]
        public async Task ArrayOfPocoGetsCreated_PoCoWithNoProperties()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await
                     client.GetAsync("http://localhost/Properties" +
                     "/GetPeople?people[0].Name=asdf");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var arrperson = JsonConvert.DeserializeObject<ArrayOfPersonWithNoProperties>(
                            await response.Content.ReadAsStringAsync());
            Assert.NotNull(arrperson);
            Assert.NotNull(arrperson.people);
            Assert.Equal(0, arrperson.people.Length);
        }

        [Theory]
        [InlineData("http://localhost/Home/ActionWithPersonFromUrlWithPrefix/Javier/26")]
        [InlineData("http://localhost/Home/ActionWithPersonFromUrlWithoutPrefix/Javier/26")]
        public async Task CanBind_ComplexData_FromRouteData(string url)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await
                     client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.NotNull(body);

            var person = JsonConvert.DeserializeObject<Person>(body);
            Assert.NotNull(person);
            Assert.Equal("Javier", person.Name);
            Assert.Equal(26, person.Age);
        }

        [Fact]
        public async Task ModelBindCancellationTokenParameteres()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/ActionWithCancellationToken");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("true", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ModelBindCancellationToken_ForProperties()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
                "http://localhost/Home/ActionWithCancellationTokenModel?wrapper=bogusValue");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("true", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ModelBindingBindsBase64StringsToByteArrays()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/Index?byteValues=SGVsbG9Xb3JsZA==");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("HelloWorld", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ModelBindingBindsEmptyStringsToByteArrays()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/Index?byteValues=");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ModelBinding_LimitsErrorsToMaxErrorCount_DoesNotValidateMembersOfMissingProperties()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var queryString = string.Join("=&", Enumerable.Range(0, 10).Select(i => "field" + i));

            // Act
            var response = await client.GetStringAsync("http://localhost/Home/ModelWithTooManyValidationErrors?" + queryString);

            //Assert
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

            // 8 is the value of MaxModelValidationErrors for the application being tested.
            // Mono issue - https://github.com/aspnet/External/issues/19
            Assert.Equal(8, json.Count);
            Assert.Equal(PlatformNormalizer.NormalizeContent("The Field1 field is required."), json["Field1"]);
            Assert.Equal(PlatformNormalizer.NormalizeContent("The Field2 field is required."), json["Field2"]);
            Assert.Equal(PlatformNormalizer.NormalizeContent("The Field3 field is required."), json["Field3"]);
            Assert.Equal(PlatformNormalizer.NormalizeContent("The Field4 field is required."), json["Field4"]);
            Assert.Equal(PlatformNormalizer.NormalizeContent("The Field5 field is required."), json["Field5"]);
            Assert.Equal(PlatformNormalizer.NormalizeContent("The Field6 field is required."), json["Field6"]);
            Assert.Equal(PlatformNormalizer.NormalizeContent("The Field7 field is required."), json["Field7"]);
            Assert.Equal("The maximum number of allowed model errors has been reached.", json[""]);
        }

        [Fact]
        public async Task ModelBinding_FallsBackAndValidatesAllPropertiesInModel()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/Home/ModelWithFewValidationErrors?model=");

            // Assert
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            Assert.Equal(3, json.Count);
            // Mono issue - https://github.com/aspnet/External/issues/19
            Assert.Equal(PlatformNormalizer.NormalizeContent("The Field1 field is required."), json["Field1"]);
            Assert.Equal(PlatformNormalizer.NormalizeContent("The Field2 field is required."), json["Field2"]);
            Assert.Equal(PlatformNormalizer.NormalizeContent("The Field3 field is required."), json["Field3"]);
        }

        [Fact]
        public async Task ModelBinding_FallsBackAndSuccessfullyBindsStructCollection()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var contentDictionary = new Dictionary<string, string>
            {
                { "[0]", "23" },
                { "[1]", "97" },
                { "[2]", "103" },
            };
            var requestContent = new FormUrlEncodedContent(contentDictionary);

            // Act
            var response = await client.PostAsync("http://localhost/integers", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var array = JsonConvert.DeserializeObject<int[]>(responseContent);

            Assert.Equal(3, array.Length);
            Assert.Equal(23, array[0]);
            Assert.Equal(97, array[1]);
            Assert.Equal(103, array[2]);
        }

        [Fact]
        public async Task ModelBinding_FallsBackAndSuccessfullyBindsPOCOCollection()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var contentDictionary = new Dictionary<string, string>
            {
                { "[0].CityCode", "YYZ" },
                { "[0].CityName", "Toronto" },
                { "[1].CityCode", "SEA" },
                { "[1].CityName", "Seattle" },
            };
            var requestContent = new FormUrlEncodedContent(contentDictionary);

            // Act
            var response = await client.PostAsync("http://localhost/cities", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<List<City>>(responseContent);

            Assert.Equal(2, list.Count);
            Assert.Equal(contentDictionary["[0].CityCode"], list[0].CityCode);
            Assert.Equal(contentDictionary["[0].CityName"], list[0].CityName);
            Assert.Equal(contentDictionary["[1].CityCode"], list[1].CityCode);
            Assert.Equal(contentDictionary["[1].CityName"], list[1].CityName);
        }

        [Fact]
        public async Task BindAttribute_Filters_UsingDefaultPropertyFilterProvider_WithExpressions()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/BindAttribute/" +
                "EchoUser" +
                "?user.UserName=someValue&user.RegisterationMonth=March&user.Id=123");

            // Assert
            var json = JsonConvert.DeserializeObject<User>(response);

            // Does not touch what is not in the included expression.
            Assert.Equal(0, json.Id);

            // Updates the included properties.
            Assert.Equal("someValue", json.UserName);
            Assert.Equal("March", json.RegisterationMonth);
        }

        [Fact]
        public async Task BindAttribute_Filters_UsingPropertyFilterProvider_UsingServices()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/BindAttribute/" +
                "EchoUserUsingServices" +
                "?user.UserName=someValue&user.RegisterationMonth=March&user.Id=123");

            // Assert
            var json = JsonConvert.DeserializeObject<User>(response);

            // Does not touch what is not in the included expression.
            Assert.Equal(0, json.Id);

            // Updates the included properties.
            Assert.Equal("someValue", json.UserName);
            Assert.Equal("March", json.RegisterationMonth);
        }

        [Fact]
        public async Task BindAttribute_Filters_UsingDefaultPropertyFilterProvider_WithPredicate()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/BindAttribute/" +
                "UpdateUserId_BlackListingAtEitherLevelDoesNotBind" +
                "?param1.LastName=someValue&param2.Id=123");

            // Assert
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            Assert.Equal(2, json.Count);
            Assert.Null(json["param1.LastName"]);
            Assert.Equal("0", json["param2.Id"]);
        }

        [Fact]
        public async Task BindAttribute_AppliesAtBothParameterAndTypeLevelTogether_BlacklistedAtEitherLevelIsNotBound()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/BindAttribute/" +
                "UpdateUserId_BlackListingAtEitherLevelDoesNotBind" +
                "?param1.LastName=someValue&param2.Id=123");

            // Assert
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            Assert.Equal(2, json.Count);
            Assert.Null(json["param1.LastName"]);
            Assert.Equal("0", json["param2.Id"]);
        }

        [Fact]
        public async Task BindAttribute_AppliesAtBothParameterAndTypeLevelTogether_IncludedAtBothLevelsIsBound()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/BindAttribute/" +
                "UpdateFirstName_IncludingAtBothLevelBinds" +
                "?param1.FirstName=someValue&param2.Id=123");

            // Assert
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            Assert.Equal(1, json.Count);
            Assert.Equal("someValue", json["param1.FirstName"]);
        }

        [Fact]
        public async Task BindAttribute_AppliesAtBothParameterAndTypeLevelTogether_IncludingAtOneLevelIsNotBound()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/BindAttribute/" +
                "UpdateIsAdmin_IncludingAtOnlyOneLevelDoesNotBind" +
                "?param1.FirstName=someValue&param1.IsAdmin=true");

            // Assert
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            Assert.Equal(2, json.Count);
            Assert.Equal("False", json["param1.IsAdmin"]);
            Assert.Null(json["param1.FirstName"]);
        }

        [Fact]
        public async Task BindAttribute_BindsUsingParameterPrefix()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/BindAttribute/" +
                "BindParameterUsingParameterPrefix" +
                "?randomPrefix.Value=someValue");

            // Assert
            Assert.Equal("someValue", response);
        }

        [Fact]
        public async Task BindAttribute_FallsBackOnTypePrefixIfNoParameterPrefixIsProvided()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/BindAttribute/" +
                "TypePrefixIsUsed" +
                "?TypePrefix.Value=someValue");

            // Assert
            Assert.Equal("someValue", response);
        }

        [Fact]
        public async Task BindAttribute_DoesNotFallBackOnEmptyPrefixIfParameterPrefixIsProvided()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/BindAttribute/" +
                "BindParameterUsingParameterPrefix" +
                "?Value=someValue");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task TryUpdateModel_IncludeTopLevelProperty_IncludesAllSubProperties()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/TryUpdateModel/" +
                "GetUserAsync_IncludesAllSubProperties" +
                "?id=123&Key=34&RegistrationMonth=March&Address.Street=123&Address.Country.Name=USA&" +
                "Address.State=WA&Address.Country.Cities[0].CityName=Seattle&Address.Country.Cities[0].CityCode=SEA");

            // Assert
            var user = JsonConvert.DeserializeObject<User>(response);

            // Should update everything under Address.
            Assert.Equal(123, user.Address.Street); // Included by default as sub properties are included.
            Assert.Equal("WA", user.Address.State); // Included by default as sub properties of address are included.
            Assert.Equal("USA", user.Address.Country.Name); // Included by default.
            Assert.Equal("Seattle", user.Address.Country.Cities[0].CityName); // Included by default.
            Assert.Equal("SEA", user.Address.Country.Cities[0].CityCode); // Included by default.

            // Should not update Any property at the same level as address.
            // Key is id + 20.
            Assert.Equal(143, user.Key);
            Assert.Null(user.RegisterationMonth);
        }

        [Fact]
        public async Task TryUpdateModel_ChainedPropertyExpression_Throws()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            Expression<Func<User, object>> expression = model => model.Address.Country;

            var expected = string.Format(
                "The passed expression of expression node type '{0}' is invalid." +
                " Only simple member access expressions for model properties are supported.",
                expression.Body.NodeType);

            // Act
            var response = await client.GetAsync("http://localhost/TryUpdateModel/GetUserAsync_WithChainedProperties?id=123");

            // Assert
            var exception = response.GetServerException();
            Assert.Equal(typeof(InvalidOperationException).FullName, exception.ExceptionType);
            Assert.Equal(expected, exception.ExceptionMessage);
        }

        [Fact]
        public async Task TryUpdateModel_FailsToUpdateProperties()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/TryUpdateModel/" +
                "TryUpdateModelFails" +
                "?id=123&RegisterationMonth=March&Key=123&UserName=SomeName");

            // Assert
            var result = JsonConvert.DeserializeObject<bool>(response);

            // Act
            Assert.False(result);
        }

        [Fact]
        public async Task TryUpdateModel_IncludeExpression_WorksOnlyAtTopLevel()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/TryUpdateModel/" +
                "GetPerson" +
                "?Parent.Name=fatherName&Parent.Parent.Name=grandFatherName");

            // Assert
            var person = JsonConvert.DeserializeObject<Person>(response);

            // Act
            Assert.Equal("fatherName", person.Parent.Name);

            // Includes this as there is data from value providers, the include filter
            // only works for top level objects.
            Assert.Equal("grandFatherName", person.Parent.Parent.Name);
        }

        [Fact]
        public async Task TryUpdateModel_Validates_ForTopLevelNotIncludedProperties()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/TryUpdateModel/" +
                "CreateAndUpdateUser" +
                "?RegistedeburationMonth=March");

            // Assert
            var result = JsonConvert.DeserializeObject<bool>(response);
            Assert.False(result);
        }

        [Fact]
        public async Task TryUpdateModelExcludeSpecific_Properties()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/TryUpdateModel/" +
                "GetUserAsync_ExcludeSpecificProperties" +
                "?id=123&RegisterationMonth=March&Key=123&UserName=SomeName");

            // Assert
            var user = JsonConvert.DeserializeObject<User>(response);

            // Should not update excluded properties.
            Assert.NotEqual(123, user.Key);

            // Should Update all explicitly included properties.
            Assert.Equal("March", user.RegisterationMonth);
            Assert.Equal("SomeName", user.UserName);
        }

        [Fact]
        public async Task TryUpdateModelIncludeSpecific_Properties()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/TryUpdateModel/" +
                "GetUserAsync_IncludeSpecificProperties" +
                "?id=123&RegisterationMonth=March&Key=123&UserName=SomeName");

            // Assert
            var user = JsonConvert.DeserializeObject<User>(response);

            // Should not update any not explicitly mentioned properties.
            Assert.NotEqual("SomeName", user.UserName);
            Assert.NotEqual(123, user.Key);

            // Should Update all included properties.
            Assert.Equal("March", user.RegisterationMonth);
        }

        [Fact]
        public async Task TryUpdateModelIncludesAllProperties_ByDefault()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/TryUpdateModel/" +
                "GetUserAsync_IncludeAllByDefault" +
                "?id=123&RegisterationMonth=March&Key=123&UserName=SomeName");

            // Assert
            var user = JsonConvert.DeserializeObject<User>(response);

            // Should not update any not explicitly mentioned properties.
            Assert.Equal("SomeName", user.UserName);
            Assert.Equal(123, user.Key);

            // Should Update all included properties.
            Assert.Equal("March", user.RegisterationMonth);
        }

        [Fact]
        public async Task UpdateVehicle_WithJson_ProducesModelStateErrors()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var content = new
            {
                Year = 3012,
                InspectedDates = new[]
                {
                    new DateTime(4065, 10, 10)
                },
                Make = "Volttrax",
                Model = "Epsum"
            };

            // Act
            var response = await client.PutAsJsonAsync("http://localhost/api/vehicles/520", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var modelStateErrors = JsonConvert.DeserializeObject<IDictionary<string, IEnumerable<string>>>(body);

            Assert.Equal(2, modelStateErrors.Count);
            // OrderBy is used because the order of the results may very depending on the platform / client.
            Assert.Equal(new[] {
                    "The field Year must be between 1980 and 2034.",
                    "Year is invalid"
                }, modelStateErrors["Year"].OrderBy(item => item, StringComparer.Ordinal));

            var vinError = Assert.Single(modelStateErrors["Vin"]);
            // Mono issue - https://github.com/aspnet/External/issues/19
            Assert.Equal(PlatformNormalizer.NormalizeContent("The Vin field is required."), vinError);
        }

        [Fact]
        public async Task UpdateVehicle_WithJson_DoesPropertyValidationPriorToValidationAtType()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var content = new
            {
                Year = 2007,
                InspectedDates = new[]
                {
                   new DateTime(4065, 10, 10)
                },
                Make = "Volttrax",
                Model = "Epsum",
                Vin = "Pqrs"
            };
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-TrackingId", "trackingid");

            // Act
            var response = await client.PutAsJsonAsync("http://localhost/api/vehicles/520", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var modelStateErrors = JsonConvert.DeserializeObject<IDictionary<string, IEnumerable<string>>>(body);

            var item = Assert.Single(modelStateErrors);
            Assert.Equal("InspectedDates", item.Key);
            var error = Assert.Single(item.Value);
            Assert.Equal("Inspection date cannot be later than year of manufacture.", error);
        }

        [Fact]
        public async Task UpdateVehicle_WithJson_BindsBodyAndServices()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var trackingId = Guid.NewGuid().ToString();
            var postedContent = new
            {
                Year = 2010,
                InspectedDates = new List<DateTime>
                {
                    new DateTime(2008, 10, 01),
                    new DateTime(2009, 03, 01),
                },
                Make = "Volttrax",
                Model = "Epsum",
                Vin = "PQRS"
            };
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-TrackingId", trackingId);

            // Act
            var response = await client.PutAsJsonAsync("http://localhost/api/vehicles/520", postedContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var actual = JsonConvert.DeserializeObject<VehicleViewModel>(body);

            Assert.Equal(postedContent.Vin, actual.Vin);
            Assert.Equal(postedContent.Make, actual.Make);
            Assert.Equal(postedContent.InspectedDates, actual.InspectedDates.Select(d => d.DateTime));
            Assert.Equal(trackingId, actual.LastUpdatedTrackingId);
        }

#if DNX451
        [ConditionalTheory]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task UpdateVehicle_WithXml_BindsBodyServicesAndHeaders()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var trackingId = Guid.NewGuid().ToString();
            var postedContent = new VehicleViewModel
            {
                Year = 2010,
                InspectedDates = new DateTimeOffset[]
                {
                    new DateTimeOffset(2008, 10, 01, 8, 3, 1, TimeSpan.Zero),
                    new DateTime(2009, 03, 01),
                },
                Make = "Volttrax",
                Model = "Epsum",
                Vin = "PQRS"
            };
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-TrackingId", trackingId);

            // Act
            var response = await client.PutAsXmlAsync("http://localhost/api/vehicles/520", postedContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var actual = JsonConvert.DeserializeObject<VehicleViewModel>(body);

            Assert.Equal(postedContent.Vin, actual.Vin);
            Assert.Equal(postedContent.Make, actual.Make);
            Assert.Equal(postedContent.InspectedDates, actual.InspectedDates);
            Assert.Equal(trackingId, actual.LastUpdatedTrackingId);
        }
#endif

        // Simulates a browser based client that does a Ajax post for partial page updates.
        [Fact]
        public async Task UpdateDealerVehicle_PopulatesPropertyErrorsInViews()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var outputFile = "compiler/resources/UpdateDealerVehicle_PopulatesPropertyErrorsInViews.txt";
            var expectedContent = await ResourceFile.ReadResourceAsync(_assembly, outputFile, sourceFile: false);
            var postedContent = new
            {
                Year = 9001,
                InspectedDates = new List<DateTime>
                {
                    new DateTime(2008, 01, 01)
                },
                Make = "Acme",
                Model = "Epsum",
                Vin = "LongerThan8Chars",

            };
            var url = "http://localhost/dealers/32/update-vehicle?dealer.name=TestCarDealer&dealer.location=SE";

            // Act
            var response = await client.PostAsJsonAsync(url, postedContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_assembly, outputFile, expectedContent, responseContent);
#else
            // Mono issue - https://github.com/aspnet/External/issues/19
            expectedContent = PlatformNormalizer.NormalizeContent(expectedContent);
            if (TestPlatformHelper.IsMono)
            {
                expectedContent = expectedContent.Replace(
                    "<span class=\"field-validation-error\" data-valmsg-for=\"Vehicle.Year\"" +
                    " data-valmsg-replace=\"true\">The field Year must be between 1980 and 2034.</span>",
                    "<span class=\"field-validation-error\" data-valmsg-for=\"Vehicle.Year\"" +
                    " data-valmsg-replace=\"true\">Year is invalid</span>");
            }

            Assert.Equal(expectedContent, responseContent, ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task UpdateDealerVehicle_PopulatesValidationSummary()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var outputFile = "compiler/resources/UpdateDealerVehicle_PopulatesValidationSummary.txt";
            var expectedContent = await ResourceFile.ReadResourceAsync(_assembly, outputFile, sourceFile: false);
            var postedContent = new
            {
                Year = 2013,
                InspectedDates = new List<DateTime>
                {
                    new DateTime(2008, 01, 01)
                },
                Make = "Acme",
                Model = "Epsum",
                Vin = "8chars",

            };
            var url = "http://localhost/dealers/43/update-vehicle?dealer.name=TestCarDealer&dealer.location=SE";

            // Act
            var response = await client.PostAsJsonAsync(url, postedContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_assembly, outputFile, expectedContent, responseContent);
#else
            // Mono issue - https://github.com/aspnet/External/issues/19
            Assert.Equal(
                PlatformNormalizer.NormalizeContent(expectedContent),
                responseContent,
                ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task UpdateDealerVehicle_UsesDefaultValuesForOptionalProperties()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var outputFile = "compiler/resources/UpdateDealerVehicle_UpdateSuccessful.txt";
            var expectedContent = await ResourceFile.ReadResourceAsync(_assembly, outputFile, sourceFile: false);
            var postedContent = new
            {
                Year = 2013,
                InspectedDates = new DateTimeOffset[]
                {
                    new DateTimeOffset(new DateTime(2008, 11, 01), TimeSpan.FromHours(-7))
                },
                Make = "RealSlowCars",
                Model = "Epsum",
                Vin = "8chars",

            };
            var url = "http://localhost/dealers/43/update-vehicle?dealer.name=TestCarDealer&dealer.location=NE";

            // Act
            var response = await client.PostAsJsonAsync(url, postedContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_assembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent, ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task FormFileModelBinder_CanBind_SingleFile()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var url = "http://localhost/FileUpload/UploadSingle";
            var formData = new MultipartFormDataContent("Upload----");
            formData.Add(new StringContent("Test Content"), "file", "test.txt");

            // Act
            var response = await client.PostAsync(url, formData);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var fileDetails = JsonConvert.DeserializeObject<FileDetails>(
                                    await response.Content.ReadAsStringAsync());
            Assert.Equal("test.txt", fileDetails.Filename);
            Assert.Equal("Test Content", fileDetails.Content);
        }

        [Fact]
        public async Task FormFileModelBinder_CanBind_MultipleFiles()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var url = "http://localhost/FileUpload/UploadMultiple";
            var formData = new MultipartFormDataContent("Upload----");
            formData.Add(new StringContent("Test Content 1"), "files", "test1.txt");
            formData.Add(new StringContent("Test Content 2"), "files", "test2.txt");

            // Act
            var response = await client.PostAsync(url, formData);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var fileDetailsArray = JsonConvert.DeserializeObject<FileDetails[]>(
                                        await response.Content.ReadAsStringAsync());
            Assert.Equal(2, fileDetailsArray.Length);
            Assert.Equal("test1.txt", fileDetailsArray[0].Filename);
            Assert.Equal("Test Content 1", fileDetailsArray[0].Content);
            Assert.Equal("test2.txt", fileDetailsArray[1].Filename);
            Assert.Equal("Test Content 2", fileDetailsArray[1].Content);
        }

        [Fact]
        public async Task FormFileModelBinder_CanBind_MultipleListOfFiles()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var url = "http://localhost/FileUpload/UploadMultipleList";
            var formData = new MultipartFormDataContent("Upload----");
            formData.Add(new StringContent("Test Content 1"), "filelist1", "test1.txt");
            formData.Add(new StringContent("Test Content 2"), "filelist1", "test2.txt");
            formData.Add(new StringContent("Test Content 3"), "filelist2", "test3.txt");
            formData.Add(new StringContent("Test Content 4"), "filelist2", "test4.txt");

            // Act
            var response = await client.PostAsync(url, formData);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var fileDetailsLookup = JsonConvert.DeserializeObject<IDictionary<string, IList<FileDetails>>>(
                                        await response.Content.ReadAsStringAsync());
            Assert.Equal(2, fileDetailsLookup.Count);
            var fileDetailsList1 = fileDetailsLookup["filelist1"];
            var fileDetailsList2 = fileDetailsLookup["filelist2"];
            Assert.Equal(2, fileDetailsList1.Count);
            Assert.Equal(2, fileDetailsList2.Count);
            Assert.Equal("test1.txt", fileDetailsList1[0].Filename);
            Assert.Equal("Test Content 1", fileDetailsList1[0].Content);
            Assert.Equal("test2.txt", fileDetailsList1[1].Filename);
            Assert.Equal("Test Content 2", fileDetailsList1[1].Content);
            Assert.Equal("test3.txt", fileDetailsList2[0].Filename);
            Assert.Equal("Test Content 3", fileDetailsList2[0].Content);
            Assert.Equal("test4.txt", fileDetailsList2[1].Filename);
            Assert.Equal("Test Content 4", fileDetailsList2[1].Content);
        }

        [Fact]
        public async Task FormFileModelBinder_CanBind_FileInsideModel()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var url = "http://localhost/FileUpload/UploadModelWithFile";
            var formData = new MultipartFormDataContent("Upload----");
            formData.Add(new StringContent("Test Book"), "Name");
            formData.Add(new StringContent("Test Content"), "File", "test.txt");

            // Act
            var response = await client.PostAsync(url, formData);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var book = JsonConvert.DeserializeObject<KeyValuePair<string, FileDetails>>(
                                    await response.Content.ReadAsStringAsync());
            var bookName = book.Key;
            var fileDetails = book.Value;
            Assert.Equal("Test Book", bookName);
            Assert.Equal("test.txt", fileDetails.Filename);
            Assert.Equal("Test Content", fileDetails.Content);
        }

        [Fact]
        public async Task TryUpdateModel_ReturnDerivedAndBaseProperties()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/TryUpdateModel/" +
                "GetEmployeeAsync_BindToBaseDeclaredType" +
                "?Parent.Name=fatherName&Parent.Parent.Name=grandFatherName&Department=Sales");

            // Assert
            var employee = JsonConvert.DeserializeObject<Employee>(response);
            Assert.Equal("fatherName", employee.Parent.Name);
            Assert.Equal("Sales", employee.Department);

            // Round-tripped value includes descendent instances for all properties with data in the request.
            Assert.Equal("grandFatherName", employee.Parent.Parent.Name);
        }

        [Fact]
        public async Task HtmlHelper_DisplayFor_ShowsPropertiesInModelMetadataOrder()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var outputFile = "compiler/resources/ModelBindingWebSite.Vehicle.Details.html";
            var expectedContent = await ResourceFile.ReadResourceAsync(_assembly, outputFile, sourceFile: false);
            var url = "http://localhost/vehicles/42";

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_assembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent, ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task HtmlHelper_EditorFor_ShowsPropertiesInModelMetadataOrder()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var outputFile = "compiler/resources/ModelBindingWebSite.Vehicle.Edit.html";
            var expectedContent = await ResourceFile.ReadResourceAsync(_assembly, outputFile, sourceFile: false);
            var url = "http://localhost/vehicles/42/edit";

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_assembly, outputFile, expectedContent, responseContent);
#else
            // Mono issue - https://github.com/aspnet/External/issues/19
            Assert.Equal(
                PlatformNormalizer.NormalizeContent(expectedContent),
                responseContent,
                ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task HtmlHelper_EditorFor_ShowsPropertiesAndErrorsInModelMetadataOrder()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var outputFile = "compiler/resources/ModelBindingWebSite.Vehicle.Edit.Invalid.html";
            var expectedContent = await ResourceFile.ReadResourceAsync(_assembly, outputFile, sourceFile: false);
            var url = "http://localhost/vehicles/42/edit";
            var contentDictionary = new Dictionary<string, string>
            {
                { "Make", "Fast Cars" },
                { "Model", "the Fastener" },
                { "InspectedDates[0]", "14/10/1979 00:00:00 -08:00" },
                { "InspectedDates[1]", "16/10/1979 00:00:00 -08:00" },
                { "InspectedDates[2]", "02/11/1979 00:00:00 -08:00" },
                { "InspectedDates[3]", "13/11/1979 00:00:00 -08:00" },
                { "InspectedDates[4]", "05/12/1979 00:00:00 -08:00" },
                { "InspectedDates[5]", "12/12/1979 00:00:00 -08:00" },
                { "InspectedDates[6]", "19/12/1979 00:00:00 -08:00" },
                { "InspectedDates[7]", "26/12/1979 00:00:00 -08:00" },
                { "InspectedDates[8]", "28/12/1979 00:00:00 -08:00" },
                { "InspectedDates[9]", "29/12/1979 00:00:00 -08:00" },
                { "InspectedDates[10]", "01/04/1980 00:00:00 -08:00" },
                { "Vin", "8765432112345678" },
                { "Year", "1979" },
            };
            var requestContent = new FormUrlEncodedContent(contentDictionary);

            // Act
            var response = await client.PostAsync(url, requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_assembly, outputFile, expectedContent, responseContent);
#else
            // Mono issue - https://github.com/aspnet/External/issues/19
            Assert.Equal(
                PlatformNormalizer.NormalizeContent(expectedContent),
                responseContent,
                ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task ModelBinder_FormatsDontMatch_ThrowsUserFriendlyException()

        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var url = "http://localhost/Home/GetErrorMessage";

            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("birthdate", "random string"),
            };
            var formData = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.PostAsync(url, formData);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal("The value 'random string' is not valid for birthdate.", result);
        }

        [Fact]
        public async Task OverriddenMetadataProvider_CanChangeAdditionalValues()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var url = "http://localhost/AdditionalValues";
            var expectedDictionary = new Dictionary<string, string>
            {
                { "key1", "7d6d0de2-8d59-49ac-99cc-881423b75a76" },
                { "key2", "value2" },
            };

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var dictionary = JsonConvert.DeserializeObject<IDictionary<string, string>>(responseContent);
            Assert.Equal(expectedDictionary, dictionary);
        }

        [Fact]
        public async Task OverriddenMetadataProvider_CanUseAttributesToChangeAdditionalValues()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var url = "http://localhost/GroupNames";
            var expectedDictionary = new Dictionary<string, string>
            {
                { "Model", "MakeAndModelGroup" },
                { "Make", "MakeAndModelGroup" },
                { "Vin", null },
                { "Year", null },
                { "InspectedDates", null },
                { "LastUpdatedTrackingId", "TrackingIdGroup" },
            };

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var dictionary = JsonConvert.DeserializeObject<IDictionary<string, string>>(responseContent);
            Assert.Equal(expectedDictionary, dictionary);
        }

        [Fact]
        public async Task TryUpdateModelNonGeneric_IncludesAllProperties_CanBind()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/TryUpdateModel/" +
                "GetUserAsync_ModelType_IncludeAll" +
                "?id=123&RegisterationMonth=March&Key=123&UserName=SomeName");

            // Assert
            var user = JsonConvert.DeserializeObject<User>(response);

            // Should not update any not explicitly mentioned properties.
            Assert.Equal("SomeName", user.UserName);
            Assert.Equal(123, user.Key);

            // Should Update all included properties.
            Assert.Equal("March", user.RegisterationMonth);
        }

        [Fact]
        public async Task FormCollectionModelBinder_CanBind_FormValues()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var url = "http://localhost/FormCollection/ReturnValuesAsList";
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("field1", "value1"),
                new KeyValuePair<string, string>("field2", "value2"),
            };
            var formData = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.PostAsync(url, formData);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var valuesList = JsonConvert.DeserializeObject<IList<string>>(
                                    await response.Content.ReadAsStringAsync());
            Assert.Equal(new List<string> { "value1", "value2" }, valuesList);
        }

        [Fact]
        public async Task FormCollectionModelBinder_CanBind_FormValuesWithDuplicateKeys()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var url = "http://localhost/FormCollection/ReturnValuesAsList";
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("field1", "value1"),
                new KeyValuePair<string, string>("field2", "value2"),
                new KeyValuePair<string, string>("field1", "value3"),
            };
            var formData = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.PostAsync(url, formData);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var valuesList = JsonConvert.DeserializeObject<IList<string>>(
                                    await response.Content.ReadAsStringAsync());
            Assert.Equal(new List<string> { "value1,value3", "value2" }, valuesList);
        }

        [Fact]
        public async Task FormCollectionModelBinder_CannotBind_NonFormValues()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var url = "http://localhost/FormCollection/ReturnCollectionCount";
            var data = new StringContent("Non form content");

            // Act
            var response = await client.PostAsync(url, data);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var collectionCount = JsonConvert.DeserializeObject<int>(
                                    await response.Content.ReadAsStringAsync());
            Assert.Equal(0, collectionCount);
        }

        [Fact]
        public async Task FormCollectionModelBinder_CanBind_FormWithFile()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var url = "http://localhost/FormCollection/ReturnFileContent";
            var expectedContent = "Test Content";
            var formData = new MultipartFormDataContent("Upload----");
            formData.Add(new StringContent(expectedContent), "File", "test.txt");

            // Act
            var response = await client.PostAsync(url, formData);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var fileContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedContent, fileContent);
        }

        [Fact]
        public async Task TryUpdateModelNonGenericIncludesAllProperties_ByDefault()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/TryUpdateModel/" +
                "GetUserAsync_ModelType_IncludeAllByDefault" +
                "?id=123&RegisterationMonth=March&Key=123&UserName=SomeName");

            // Assert
            var user = JsonConvert.DeserializeObject<User>(response);

            // Should not update any not explicitly mentioned properties.
            Assert.Equal("SomeName", user.UserName);
            Assert.Equal(123, user.Key);

            // Should Update all included properties.
            Assert.Equal("March", user.RegisterationMonth);
        }

        [Fact]
        public async Task BindModelAsync_WithCollection()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var content = new Dictionary<string, string>
            {
                { "AddressLines[0].Line", "Street Address 0" },
                { "AddressLines[1].Line", "Street Address 1" },
                { "ZipCode", "98052" },
            };
            var url = "http://localhost/Person_CollectionBinder/CollectionType";
            var formData = new FormUrlEncodedContent(content);

            // Act
            var response = await client.PutAsync(url, formData);

            // Assert
            var address = await ReadValue<PersonAddress>(response);
            Assert.Equal(2, address.AddressLines.Count);
            Assert.Equal("Street Address 0", address.AddressLines[0].Line);
            Assert.Equal("Street Address 1", address.AddressLines[1].Line);
            Assert.Equal("98052", address.ZipCode);
        }

        [Fact]
        public async Task BindModelAsync_WithCollection_SpecifyingIndex()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var content = new[]
            {
                new KeyValuePair<string, string>("AddressLines.index", "3"),
                new KeyValuePair<string, string>("AddressLines.index", "10000"),
                new KeyValuePair<string, string>("AddressLines[3].Line", "Street Address 0"),
                new KeyValuePair<string, string>("AddressLines[10000].Line", "Street Address 1"),
            };
            var url = "http://localhost/Person_CollectionBinder/CollectionType";
            var formData = new FormUrlEncodedContent(content);

            // Act
            var response = await client.PutAsync(url, formData);

            // Assert
            var address = await ReadValue<PersonAddress>(response);
            Assert.Equal(2, address.AddressLines.Count);
            Assert.Equal("Street Address 0", address.AddressLines[0].Line);
            Assert.Equal("Street Address 1", address.AddressLines[1].Line);
        }

        [Fact]
        public async Task BindModelAsync_WithNestedCollection()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var content = new Dictionary<string, string>
            {
                { "Addresses[0].AddressLines[0].Line", "Street Address 00" },
                { "Addresses[0].AddressLines[1].Line", "Street Address 01" },
                { "Addresses[0].ZipCode", "98052" },
                { "Addresses[1].AddressLines[0].Line", "Street Address 10" },
                { "Addresses[1].AddressLines[3].Line", "Street Address 13" },
            };
            var url = "http://localhost/Person_CollectionBinder/NestedCollectionType";
            var formData = new FormUrlEncodedContent(content);

            // Act
            var response = await client.PostAsync(url, formData);

            // Assert
            var result = await ReadValue<UserWithAddress>(response);
            Assert.Equal(2, result.Addresses.Count);
            var address = result.Addresses[0];
            Assert.Equal(2, address.AddressLines.Count);
            Assert.Equal("Street Address 00", address.AddressLines[0].Line);
            Assert.Equal("Street Address 01", address.AddressLines[1].Line);
            Assert.Equal("98052", address.ZipCode);

            address = result.Addresses[1];
            Assert.Single(address.AddressLines);
            Assert.Equal("Street Address 10", address.AddressLines[0].Line);
            Assert.Null(address.ZipCode);
        }

        [Fact]
        public async Task BindModelAsync_WithIncorrectlyFormattedNestedCollectionValue_BindsSingleNullEntry()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var content = new Dictionary<string, string>
            {
                { "Addresses", "Street Address 00" },
            };
            var url = "http://localhost/Person_CollectionBinder/NestedCollectionType";
            var formData = new FormUrlEncodedContent(content);

            // Act
            var response = await client.PostAsync(url, formData);

            // Assert
            var result = await ReadValue<UserWithAddress>(response);
            Assert.Empty(result.Addresses);
        }

        [Fact]
        public async Task BindModelAsync_WithNestedCollectionContainingRecursiveRelation()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var content = new Dictionary<string, string>
            {
                { "People[0].Name", "Person 0" },
                { "People[0].Parent.Name", "Person 0 Parent" },
                { "People[1].Parent.Name", "Person 1 Parent" },
                { "People[2].Parent", "Person 2 Parent" },
                { "People[1000].Name", "Person 1000 Parent" },
            };
            var url = "http://localhost/Person_CollectionBinder/NestedCollectionOfRecursiveTypes";
            var formData = new FormUrlEncodedContent(content);

            // Act
            var response = await client.PostAsync(url, formData);

            // Assert
            var result = await ReadValue<PeopleModel>(response);
            Assert.Equal(3, result.People.Count);
            var person = result.People[0];

            Assert.Equal("Person 0", person.Name);
            Assert.Equal("Person 0 Parent", person.Parent.Name);
            Assert.Null(person.Parent.Parent);

            person = result.People[1];
            Assert.Equal("Person 1 Parent", person.Parent.Name);
            Assert.Null(person.Parent.Parent);

            person = result.People[2];
            Assert.Null(person.Name);
            Assert.Null(person.Parent);
        }

        [Fact]
        public async Task
            BindModelAsync_WithNestedCollectionContainingRecursiveRelation_WithMalformedValue_BindsSingleNullEntry()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var content = new Dictionary<string, string>
            {
                { "People", "Person 0" },
            };
            var url = "http://localhost/Person_CollectionBinder/NestedCollectionOfRecursiveTypes";
            var formData = new FormUrlEncodedContent(content);

            // Act
            var response = await client.PostAsync(url, formData);

            // Assert
            var result = await ReadValue<PeopleModel>(response);

            Assert.Empty(result.People);
        }

        [Theory]
        [InlineData("true", "false", true)]
        [InlineData("false", "true", false)]
        public async Task BindModelAsync_MultipleCheckBoxesWithSameKey_BindsFirstValue(string firstValue,
                                                                                       string secondValue,
                                                                                       bool expectedResult)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var content = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("isValid", firstValue),
                new KeyValuePair<string,string>("isValid", secondValue),
            };
            var url = "http://localhost/Person_CollectionBinder/PostCheckBox";
            var formData = new FormUrlEncodedContent(content);

            // Act
            var response = await client.PostAsync(url, formData);

            // Assert
            var result = await ReadValue<bool>(response);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task BindModelAsync_CheckBoxesList_BindSuccessful()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var content = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("userPreferences[0].Id", "1"),
                new KeyValuePair<string,string>("userPreferences[0].Checked", "true"),
                new KeyValuePair<string,string>("userPreferences[1].Id", "2"),
                new KeyValuePair<string,string>("userPreferences[1].Checked", "false"),
            };
            var url = "http://localhost/Person_CollectionBinder/PostCheckBoxList";
            var formData = new FormUrlEncodedContent(content);

            // Act
            var response = await client.PostAsync(url, formData);

            // Assert
            var result = await ReadValue<List<UserPreference>>(response);
            Assert.True(result[0].Checked);
            Assert.False(result[1].Checked);
        }

        [Fact]
        public async Task TryUpdateModel_ClearsModelStateEntries()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var url = "http://localhost/TryUpdateModel/TryUpdateModel_ClearsModelStateEntries";

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(string.Empty, body);
        }

        private async Task<TVal> ReadValue<TVal>(HttpResponseMessage response)
        {
            Assert.True(response.IsSuccessStatusCode);
            return JsonConvert.DeserializeObject<TVal>(await response.Content.ReadAsStringAsync());
        }
    }
}
