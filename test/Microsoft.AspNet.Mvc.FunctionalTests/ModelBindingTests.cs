// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using ModelBindingWebSite;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ModelBindingTests
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("ModelBindingWebSite");
        private readonly Action<IApplicationBuilder> _app = new ModelBindingWebSite.Startup().Configure;

        [Theory]
        [InlineData("RestrictValueProvidersUsingFromRoute", "valueFromRoute")]
        [InlineData("RestrictValueProvidersUsingFromQuery", "valueFromQuery")]
        [InlineData("RestrictValueProvidersUsingFromForm", "valueFromForm")]
        public async Task CompositeModelBinder_Restricts_ValueProviders(string actionName, string expectedValue)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
               "http://localhost/FromServices_Calculator/CalculateWithPrecision?Left=10&Right=5&Operator=*");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("50", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task MultipleParametersMarkedWithFromBody_Throws()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FromAttributes/FromBodyParametersThrows");

            // Assert
            var exception = response.GetServerException();
            Assert.Equal(typeof(InvalidOperationException).FullName, exception.ExceptionType);
            Assert.Equal(
                "More than one parameter and/or property is bound to the HTTP request's content.",
                exception.ExceptionMessage);
        }

        [Fact]
        public async Task MultipleParameterAndPropertiesMarkedWithFromBody_Throws()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FromAttributes/FromBodyParameterAndPropertyThrows");

            // Assert
            var exception = response.GetServerException();
            Assert.Equal(typeof(InvalidOperationException).FullName, exception.ExceptionType);
            Assert.Equal(
                "More than one parameter and/or property is bound to the HTTP request's content.",
                exception.ExceptionMessage);
        }

        [Fact]
        public async Task MultipleParametersMarkedWith_FromFormAndFromBody_Throws()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FromAttributes/FormAndBody_AsParameters_Throws");

            // Assert
            var exception = response.GetServerException();
            Assert.Equal(typeof(InvalidOperationException).FullName, exception.ExceptionType);
            Assert.Equal(
                "More than one parameter and/or property is bound to the HTTP request's content.",
                exception.ExceptionMessage);
        }

        [Fact]
        public async Task MultipleParameterAndPropertiesMarkedWith_FromFormAndFromBody_Throws()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FromAttributes/FormAndBody_Throws");

            // Assert
            var exception = response.GetServerException();
            Assert.Equal(typeof(InvalidOperationException).FullName, exception.ExceptionType);
            Assert.Equal(
                "More than one parameter and/or property is bound to the HTTP request's content.",
                exception.ExceptionMessage);
        }

        [Fact]
        public async Task CanBind_MultipleParameters_UsingFromForm()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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

        [Theory]
        [InlineData("http://localhost/Home/ActionWithPersonFromUrlWithPrefix/Javier/26")]
        [InlineData("http://localhost/Home/ActionWithPersonFromUrlWithoutPrefix/Javier/26")]
        public async Task CanBind_ComplexData_FromRouteData(string url)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/Index?byteValues=");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("\0", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ModelBinding_LimitsErrorsToMaxErrorCount()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var queryString = string.Join("=&", Enumerable.Range(0, 10).Select(i => "field" + i));

            // Act
            var response = await client.GetStringAsync("http://localhost/Home/ModelWithTooManyValidationErrors?" + queryString);

            //Assert
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            // 8 is the value of MaxModelValidationErrors for the application being tested.
            Assert.Equal(8, json.Count);
            Assert.Equal("The Field1 field is required.", json["Field1.Field1"]);
            Assert.Equal("The Field2 field is required.", json["Field1.Field2"]);
            Assert.Equal("The Field3 field is required.", json["Field1.Field3"]);
            Assert.Equal("The Field1 field is required.", json["Field2.Field1"]);
            Assert.Equal("The Field2 field is required.", json["Field2.Field2"]);
            Assert.Equal("The Field3 field is required.", json["Field2.Field3"]);
            Assert.Equal("The Field1 field is required.", json["Field3.Field1"]);
            Assert.Equal("The maximum number of allowed model errors has been reached.", json[""]);
        }

        [Fact]
        public async Task ModelBinding_ValidatesAllPropertiesInModel()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/Home/ModelWithFewValidationErrors?model=");

            //Assert
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            Assert.Equal(3, json.Count);
            Assert.Equal("The Field1 field is required.", json["model.Field1"]);
            Assert.Equal("The Field2 field is required.", json["model.Field2"]);
            Assert.Equal("The Field3 field is required.", json["model.Field3"]);
        }

        [Fact]
        public async Task BindAttribute_Filters_UsingDefaultPropertyFilterProvider_WithExpressions()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/BindAttribute/" +
                "BindParameterUsingParameterPrefix" +
                "?randomPrefix.Value=someValue");

            // Assert
            Assert.Equal("someValue", response);
        }

        [Fact]
        public async Task BindAttribute_DoesNotUseTypePrefix()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/BindAttribute/" +
                "TypePrefixIsNeverUsed" +
                "?param.Value=someValue");

            // Assert
            Assert.Equal("someValue", response);
        }

        [Fact]
        public async Task BindAttribute_FallsBackOnEmptyPrefixIfNoParameterPrefixIsProvided()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/BindAttribute/" +
                "TypePrefixIsNeverUsed" +
                "?Value=someValue");

            // Assert
            Assert.Equal("someValue", response);
        }

        [Fact]
        public async Task BindAttribute_DoesNotFallBackOnEmptyPrefixIfParameterPrefixIsProvided()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/TryUpdateModel/" +
                "CreateAndUpdateUser" +
                "?RegisterationMonth=March");

            // Assert
            var result = JsonConvert.DeserializeObject<bool>(response);
            Assert.False(result);
        }

        [Fact]
        public async Task TryUpdateModelExcludeSpecific_Properties()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
    }
}