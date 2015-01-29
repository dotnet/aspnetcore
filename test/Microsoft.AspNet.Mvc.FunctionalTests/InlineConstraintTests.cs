// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using InlineConstraints;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class InlineConstraintTests
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices("InlineConstraintsWebSite");
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task RoutingToANonExistantArea_WithExistConstraint_RoutesToCorrectAction()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/area-exists/Users");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var returnValue = await response.Content.ReadAsStringAsync();
            Assert.Equal("Users.Index", returnValue);
        }

        [Fact]
        public async Task RoutingToANonExistantArea_WithoutExistConstraint_RoutesToIncorrectAction()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/area-withoutexists/Users");

            // Assert
            var exception = response.GetServerException();
            Assert.Equal("The view 'Index' was not found." +
                         " The following locations were searched:__/Areas/Users/Views/Home/Index.cshtml__" +
                         "/Areas/Users/Views/Shared/Index.cshtml__/Views/Shared/Index.cshtml.",
                         exception.ExceptionMessage);
        }

        [Fact]
        public async Task GetProductById_IntConstraintForOptionalId_IdPresent()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/products/GetProductById/5");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await GetResponseValues(response);
            Assert.Equal(result["id"], "5");
            Assert.Equal(result["controller"], "InlineConstraints_Products");
            Assert.Equal(result["action"], "GetProductById");
        }

        [Fact]
        public async Task GetProductById_IntConstraintForOptionalId_NoId()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/products/GetProductById");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await GetResponseValues(response);
            Assert.Equal(result["controller"], "InlineConstraints_Products");
            Assert.Equal(result["action"], "GetProductById");
        }

        [Fact]
        public async Task GetProductById_IntConstraintForOptionalId_NotIntId()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/products/GetProductById/asdf");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetProductByName_AlphaContraintForMandatoryName_ValidName()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/products/GetProductByName/asdf");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await GetResponseValues(response);
            Assert.Equal(result["name"], "asdf");
            Assert.Equal(result["controller"], "InlineConstraints_Products");
            Assert.Equal(result["action"], "GetProductByName");
        }

        [Fact]
        public async Task GetProductByName_AlphaContraintForMandatoryName_NonAlphaName()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/products/GetProductByName/asd123");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetProductByName_AlphaContraintForMandatoryName_NoName()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/products/GetProductByName");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetProductByManufacturingDate_DateTimeConstraintForMandatoryDateTime_ValidDateTime()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = 
                await client.GetAsync(@"http://localhost/products/GetProductByManufacturingDate/2014-10-11T13:45:30");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await GetResponseValues(response);
            Assert.Equal(result["dateTime"], new DateTime(2014, 10, 11, 13, 45, 30));
            Assert.Equal(result["controller"], "InlineConstraints_Products");
            Assert.Equal(result["action"], "GetProductByManufacturingDate");
        }

        [Fact]
        public async Task GetProductByCategoryName_StringLengthConstraint_ForOptionalCategoryName_ValidCatName()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            
            // Act
            var response = await client.GetAsync("http://localhost/products/GetProductByCategoryName/Sports");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await GetResponseValues(response);
            Assert.Equal(result["name"], "Sports");
            Assert.Equal(result["controller"], "InlineConstraints_Products");
            Assert.Equal(result["action"], "GetProductByCategoryName");
        }

        [Fact]
        public async Task GetProductByCategoryName_StringLengthConstraint_ForOptionalCategoryName_InvalidCatName()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = 
                await client.GetAsync("http://localhost/products/GetProductByCategoryName/SportsSportsSportsSports");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetProductByCategoryName_StringLength1To20Constraint_ForOptionalCategoryName_NoCatName()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/products/GetProductByCategoryName");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await GetResponseValues(response);
            Assert.Equal(result["controller"], "InlineConstraints_Products");
            Assert.Equal(result["action"], "GetProductByCategoryName");
        }

        [Fact]
        public async Task GetProductByCategoryId_Int10To100Constraint_ForMandatoryCatId_ValidId()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            
            // Act
            var response = await client.GetAsync("http://localhost/products/GetProductByCategoryId/40");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await GetResponseValues(response);
            Assert.Equal(result["catId"], "40");
            Assert.Equal(result["controller"], "InlineConstraints_Products");
            Assert.Equal(result["action"], "GetProductByCategoryId");
        }

        [Fact]
        public async Task GetProductByCategoryId_Int10To100Constraint_ForMandatoryCatId_InvalidId()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/products/GetProductByCategoryId/5");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetProductByCategoryId_Int10To100Constraint_ForMandatoryCatId_NotIntId()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/products/GetProductByCategoryId/asdf");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetProductByPrice_FloatContraintForOptionalPrice_Valid()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/products/GetProductByPrice/4023.23423");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await GetResponseValues(response);
            Assert.Equal(result["price"], "4023.23423");
            Assert.Equal(result["controller"], "InlineConstraints_Products");
            Assert.Equal(result["action"], "GetProductByPrice");
        }

        [Fact]
        public async Task GetProductByPrice_FloatContraintForOptionalPrice_NoPrice()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/products/GetProductByPrice");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await GetResponseValues(response);
            Assert.Equal(result["controller"], "InlineConstraints_Products");
            Assert.Equal(result["action"], "GetProductByPrice");
        }

        [Fact]
        public async Task GetProductByManufacturerId_IntMin10Constraint_ForOptionalManufacturerId_Valid()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/products/GetProductByManufacturerId/57");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await GetResponseValues(response);
            Assert.Equal(result["manId"], "57");
            Assert.Equal(result["controller"], "InlineConstraints_Products");
            Assert.Equal(result["action"], "GetProductByManufacturerId");
        }

        [Fact]
        public async Task GetProductByManufacturerId_IntMin10Cinstraint_ForOptionalManufacturerId_NoId()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/products/GetProductByManufacturerId");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await GetResponseValues(response);
            Assert.Equal(result["controller"], "InlineConstraints_Products");
            Assert.Equal(result["action"], "GetProductByManufacturerId");
        }

        [Fact]
        public async Task GetUserByName_RegExConstraint_ForMandatoryName_Valid()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/products/GetUserByName/abc");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await GetResponseValues(response);
            Assert.Equal(result["controller"], "InlineConstraints_Products");
            Assert.Equal(result["action"], "GetUserByName");
            Assert.Equal(result["name"], "abc");
        }

        [Fact]
        public async Task GetUserByName_RegExConstraint_ForMandatoryName_InValid()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/products/GetUserByName/abcd");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetStoreById_GuidConstraintForOptionalId_Valid()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response =
                await client.GetAsync("http://localhost/Store/GetStoreById/691cf17a-791b-4af8-99fd-e739e168170f");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await GetResponseValues(response);
            Assert.Equal(result["id"], "691cf17a-791b-4af8-99fd-e739e168170f");
            Assert.Equal(result["controller"], "InlineConstraints_Store");
            Assert.Equal(result["action"], "GetStoreById");
        }

        [Fact]
        public async Task GetStoreById_GuidConstraintForOptionalId_NoId()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Store/GetStoreById");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await GetResponseValues(response);
            Assert.Equal(result["controller"], "InlineConstraints_Store");
            Assert.Equal(result["action"], "GetStoreById");
        }

        [Fact]
        public async Task GetStoreById_GuidConstraintForOptionalId_NotGuidId()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Store/GetStoreById/691cf17a-791b");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetStoreByLocation_StringLengthConstraint_AlphaConstraint_ForMandatoryLocation_Valid()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Store/GetStoreByLocation/Bellevue");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await GetResponseValues(response);
            Assert.Equal(result["location"], "Bellevue");
            Assert.Equal(result["controller"], "InlineConstraints_Store");
            Assert.Equal(result["action"], "GetStoreByLocation");
        }

        [Fact]
        public async Task GetStoreByLocation_StringLengthConstraint_AlphaConstraint_ForMandatoryLocation_MoreLength()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Store/GetStoreByLocation/BellevueRedmond");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetStoreByLocation_StringLengthConstraint_AlphaConstraint_ForMandatoryLocation_LessLength()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Store/GetStoreByLocation/Be");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetStoreByLocation_StringLengthConstraint_AlphaConstraint_ForMandatoryLocation_NoAlpha()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Store/GetStoreByLocation/Bell124");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        public static IEnumerable<object[]> QueryParameters
        {
            // The first four parameters are controller name, action name, parameters in the query and their values.   
            // These are used to generate a link, the last parameter is expected generated link 
            get
            {
                // Attribute Route, id:int? constraint
                yield return new object[]
                    {
                        "InlineConstraints_Products",
                        "GetProductById",
                        "id",
                        "5",
                        "/products/GetProductById/5"
                    };

                // Attribute Route, id:int? constraint
                yield return new object[] 
                    {
                        "InlineConstraints_Products",
                        "GetProductById",
                        "id",
                        "sdsd", ""
                    };

                // Attribute Route, name:alpha constraint
                yield return new object[]
                    {
                        "InlineConstraints_Products",
                        "GetProductByName",
                        "name",
                        "zxcv",
                        "/products/GetProductByName/zxcv"
                    };

                // Attribute Route, name:length(1,20)? constraint
                yield return new object[] 
                    {
                        "InlineConstraints_Products",
                        "GetProductByCategoryName",
                        "name",
                        "sports",
                        "/products/GetProductByCategoryName/sports"
                    };

                // Attribute Route, name:length(1,20)? constraint
                yield return new object[] 
                    {
                        "InlineConstraints_Products",
                        "GetProductByCategoryName",
                        null,
                        null,
                        "/products/GetProductByCategoryName"
                    };

                // Attribute Route, catId:int:range(10, 100) constraint
                yield return new object[] 
                    {
                        "InlineConstraints_Products",
                        "GetProductByCategoryId",
                        "catId",
                        "50",
                        "/products/GetProductByCategoryId/50"
                    };

                // Attribute Route, catId:int:range(10, 100) constraint
                yield return new object[] 
                    {
                        "InlineConstraints_Products",
                        "GetProductByCategoryId",
                        "catId",
                        "500",
                        ""
                    };

                // Attribute Route, name:length(1,20)? constraint
                yield return new object[] 
                    {
                        "InlineConstraints_Products",
                        "GetProductByPrice",
                        "price",
                        "123.45",
                        "/products/GetProductByPrice/123.45"
                    };

                // Attribute Route, price:float? constraint
                yield return new object[] 
                    {
                        "InlineConstraints_Products",
                        "GetProductByManufacturerId",
                        "manId",
                        "15",
                        "/products/GetProductByManufacturerId/15"
                    };

                // Attribute Route, manId:int:min(10)? constraint
                yield return new object[] 
                    {
                        "InlineConstraints_Products",
                        "GetProductByManufacturerId",
                        "manId",
                        "qwer",
                        ""
                    };

                // Attribute Route, manId:int:min(10)? constraint
                yield return new object[] 
                    {
                        "InlineConstraints_Products",
                        "GetProductByManufacturerId",
                        "manId",
                        "1",
                        ""
                    };

                // Attribute Route, dateTime:datetime constraint
                yield return new object[] 
                    {
                        "InlineConstraints_Products",
                        "GetProductByManufacturingDate",
                        "dateTime",
                        "2014-10-11T13:45:30",
                        "/products/GetProductByManufacturingDate/2014-10-11T13%3a45%3a30"
                    };

                // Conventional Route, id:guid? constraint
                yield return new object[] 
                    {
                        "InlineConstraints_Store",
                        "GetStoreById",
                        "id",
                        "691cf17a-791b-4af8-99fd-e739e168170f",
                        "/store/GetStoreById/691cf17a-791b-4af8-99fd-e739e168170f"
                    };
            }
        }

        [Theory]
        [MemberData(nameof(QueryParameters))]
        public async Task GetGeneratedLink(
            string controller, 
            string action, 
            string parameterName, 
            string parameterValue, 
            string expectedLink)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            string url;

            if (parameterName == null)
            {
                url = string.Format(
                    "{0}newController={1}&newAction={2}",
                    "http://localhost/products/GetGeneratedLink?",
                    controller,
                    action);
            }
            else
            {
                url = string.Format(
                    "{0}newController={1}&newAction={2}&{3}={4}",
                    "http://localhost/products/GetGeneratedLink?",
                    controller,
                    action,
                    parameterName,
                    parameterValue);                
            }

            var response = await client.GetAsync(url);

            // Assert            
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedLink, body);
        }

        private async Task<IDictionary<string, object>> GetResponseValues(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IDictionary<string, object>>(body);
        }
    }
}