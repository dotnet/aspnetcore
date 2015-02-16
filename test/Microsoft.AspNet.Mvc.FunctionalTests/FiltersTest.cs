// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc.Xml;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class FiltersTest
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("FiltersWebSite");
        private readonly Action<IApplicationBuilder> _app = new FiltersWebSite.Startup().Configure;

        // A controller can only be an action filter and result filter, so we don't have entries
        // for the other filter types implemented by the controller.
        [Fact]
        public async Task ListAllFilters()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            var expected = new string[]
            {
                "Global Authorization Filter - OnAuthorization",
                "On Controller Authorization Filter - OnAuthorization",
                "Authorize Filter On Action - OnAuthorization",
                "Global Resource Filter - OnResourceExecuting",
                "Controller Resource Filter - OnResourceExecuting",
                "Action Resource Filter - OnResourceExecuting",
                "Controller Override - OnActionExecuting",
                "Global Action Filter - OnActionExecuting",
                "On Controller Action Filter - OnActionExecuting",
                "On Action Action Filter - OnActionExecuting",
                "Executing Action",
                "On Action Action Filter - OnActionExecuted",
                "On Controller Action Filter - OnActionExecuted",
                "Global Action Filter - OnActionExecuted",
                "Controller Override - OnActionExecuted",
                "Controller Override - OnResultExecuting",
                "Global Result Filter - OnResultExecuted",
                "On Controller Result Filter - OnResultExecuting",
                "On Action Result Filter - OnResultExecuting",
                "On Action Result Filter - OnResultExecuted",
                "On Controller Result Filter - OnResultExecuted",
                "Global Result Filter - OnResultExecuted",
                "Controller Override - OnResultExecuted",
                "Action Resource Filter - OnResourceExecuted",
                "Controller Resource Filter - OnResourceExecuted",
                "Global Resource Filter - OnResourceExecuted",
            };

            // Act
            var response = await client.GetAsync("http://localhost/Products/GetPrice/5");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();

            var filters = response.Headers.GetValues("filters").Single().Split(',');

            var i = 0;
            foreach (var filter in filters)
            {
                Assert.Equal(filter, expected[i++]);
            }

            Assert.Equal(expected.Length, filters.Length);
        }

        [Fact]
        public async Task AnonymousUsersAreBlocked()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Anonymous/GetHelloWorld");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AllowsAnonymousUsersToAccessController()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/RandomNumber/GetRandomNumber");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("4", await response.Content.ReadAsStringAsync());
        }


        [Theory]
        [InlineData("AdminRole")]
        [InlineData("InteractiveUsers")]
        [InlineData("ApiManagers")]
        public async Task CanAuthorize(string testAction)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
                "http://localhost/AuthorizeUser/"+testAction);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello World!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task AllowAnonymousOverridesAuthorize()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
                "http://localhost/AuthorizeUser/AlwaysCanCallAllowAnonymous");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello World!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ImpossiblePolicyFailsAuthorize()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
                "http://localhost/AuthorizeUser/Impossible");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ServiceFilterUsesRegisteredServicesAsFilter()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/RandomNumber/GetRandomNumber");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("4", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ServiceFilterThrowsIfServiceIsNotRegistered()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var url = "http://localhost/RandomNumber/GetAuthorizedRandomNumber";

            // Act
            var response = await client.GetAsync(url);

            // Assert
            var exception = response.GetServerException();
            Assert.Equal(typeof(InvalidOperationException).FullName, exception.ExceptionType);
        }

        [Fact]
        public async Task TypeFilterInitializesArguments()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var url = "http://localhost/RandomNumber/GetModifiedRandomNumber?randomNumber=10";

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("22", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task TypeFilterThrowsIfServicesAreNotRegistered()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var url = "http://localhost/RandomNumber/GetHalfOfModifiedRandomNumber?randomNumber=3";

            // Act
            var response = await client.GetAsync(url);

            // Assert
            var exception = response.GetServerException();
            Assert.Equal(typeof(InvalidOperationException).FullName, exception.ExceptionType);
        }

        [Fact]
        public async Task ActionFilterOverridesActionExecuted()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/XmlSerializer/GetDummyClass?sampleInput=10");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            XmlAssert.Equal("<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ResultFilterOverridesOnResultExecuting()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/DummyClass/GetDummyClass");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            XmlAssert.Equal("<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>120</SampleInt></DummyClass>",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ResultFilterOverridesOnResultExecuted()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/DummyClass/GetEmptyActionResult");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = response.Headers.GetValues("OnResultExecuted");
            Assert.Equal(new string[] { "ResultExecutedSuccessfully" }, result);
        }

        // Verifies result filter is executed after action filter.
        [Fact]
        public async Task OrderOfExecutionOfFilters_WhenOrderAttribute_IsNotMentioned()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/GetSampleString");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Result filter, Action Filter - OnActionExecuted, From Controller",
                await response.Content.ReadAsStringAsync());
        }

        // Action filter handles the exception thrown in the action.
        // Verifies if Result filter is executed after that.
        [Fact]
        public async Task ExceptionsHandledInActionFilters_WillNotShortCircuitResultFilters()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/ThrowExceptionAndHandleInActionFilter");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Result filter, Hi from Action Filter", await response.Content.ReadAsStringAsync());
        }

        // Exception filter present on the Action handles the exception, followed by Global Exception filter.
        // Verifies that Result filter is skipped.
        [Fact]
        public async Task ExceptionFilter_OnAction_ShortCircuitsResultFilters()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/ThrowExcpetion");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "GlobalExceptionFilter.OnException, Action Exception Filter",
                await response.Content.ReadAsStringAsync());
        }

        // No Exception filter is present on Action, Controller.
        // Verifies if Global exception filter handles the exception.
        [Fact]
        public async Task GlobalExceptionFilter_HandlesAnException()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Exception/GetError?error=RandomError");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("GlobalExceptionFilter.OnException", await response.Content.ReadAsStringAsync());
        }

        // Action, Controller, and a Global Exception filters are present.
        // Verifies they are executed in the above mentioned order.
        [Fact]
        public async Task ExceptionFilter_Scope()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ExceptionOrder/GetError");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "GlobalExceptionFilter.OnException, " +
                "ControllerExceptionFilter.OnException, " +
                "Action Exception Filter",
                await response.Content.ReadAsStringAsync());
        }

        // Action, Controller have an action filter.
        // Verifies they are executed in the mentioned order.
        [Fact]
        public async Task ActionFilter_Scope()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ActionFilter/GetHelloWorld");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "Controller override - OnActionExecuted, " +
                "GlobalActionFilter.OnActionExecuted, " +
                "Controller Action filter - OnActionExecuted, " +
                "Action Filter - OnActionExecuted, " +
                "Hello World, " + // Return value from Action
                "Action Filter - OnActionExecuting, " +
                "Controller Action filter - OnActionExecuting, " +
                "GlobalActionFilter.OnActionExecuting, " +
                "Controller override - OnActionExecuting",
                await response.Content.ReadAsStringAsync());
        }

        // Action, Controller have an result filter.
        // Verifies that Controller Result filter is executed before Action filter.
        [Fact]
        public async Task ResultFilter_Scope()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ResultFilter/GetHelloWorld");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "Result filter, " +
                "Controller Result filter, " +
                "GlobalResultFilter.OnResultExecuting, " +
                "Controller Override, " +
                "Hello World", // Return value from Action
                await response.Content.ReadAsStringAsync());
        }

        // Action has multiple TypeFilters with Order.
        // Verifies if the filters are executed in the mentioned order.
        [Fact]
        public async Task FiltersWithOrder()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/RandomNumber/GetOrderedRandomNumber");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("88", await response.Content.ReadAsStringAsync());
        }

        // Action has multiple action filters with Order.
        // Verifies they are executed in the mentioned order.
        [Fact]
        public async Task ActionFiltersWithOrder()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/ActionFilterOrder");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "Action Filter - OnActionExecuted, " +
                "Controller Action filter - OnActionExecuted, " +
                "Hello World", // Return value from Action
                await response.Content.ReadAsStringAsync());
        }

        // Action has multiple result filters with Order.
        // Verifies they are executed in the mentioned order.
        [Fact]
        public async Task ResultFiltersWithOrder()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/ResultFilterOrder");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "Result filter, Controller Result filter, Hello World",
                await response.Content.ReadAsStringAsync());
        }

        // Action has an action filter which sets the Result.
        // Verifies the Action was not executed
        [Fact]
        public async Task ActionFilterShortCircuitsAction()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/DummyClass/ActionNeverGetsExecuted");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("The Action was never executed", await response.Content.ReadAsStringAsync());
        }

        // Action has an Result filter which sets the Result.
        // Verifies ObjectResult was not executed.
        [Fact]
        public async Task ResultFilterShortCircuitsResult()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/DummyClass/ResultNeverGetsExecuted");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("The Result was never executed", await response.Content.ReadAsStringAsync());
        }

        // Action has two Exception filters.
        // Verifies that the second Exception Filter was not executed.
        [Fact]
        public async Task ExceptionFilterShortCircuitsAnotherExceptionFilter()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/ThrowRandomExcpetion");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }

        // Result Filter throws.
        // Exception Filters don't get a chance to handle this.
        [Fact]
        public async Task ThrowingFilters_ResultFilter_NotHandledByGlobalExceptionFilter()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/ThrowingResultFilter");

            // Assert
            var exception = response.GetServerException();
            Assert.Equal(typeof(InvalidProgramException).FullName, exception.ExceptionType);
        }

        // Action Filter throws.
        // Verifies the Global Exception Filter handles it.
        [Fact]
        public async Task ThrowingFilters_ActionFilter_HandledByGlobalExceptionFilter()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/ThrowingActionFilter");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("GlobalExceptionFilter.OnException", await response.Content.ReadAsStringAsync());
        }

        // Authorization Filter throws.
        // Exception Filters don't get a chance to handle this.
        [Fact]
        public async Task ThrowingFilters_AuthFilter_NotHandledByGlobalExceptionFilter()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/ThrowingAuthorizationFilter");

            // Assert
            var exception = response.GetServerException();
            Assert.Equal(typeof(InvalidProgramException).FullName, exception.ExceptionType);
        }

        // Exception Filter throws.
        // Verifies the thrown exception is ignored.
        [Fact]
        public async Task ThrowingExceptionFilter_ExceptionFilter_NotHandledByGlobalExceptionFilter()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/ThrowingExceptionFilter");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Throwing Exception Filter", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ResourceFilter_ChangesInputFormatters_JsonAccepted()
        {
            // Arrange
            var input = "{ sampleInt: 10 }";

            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Json");
            request.Content = new StringContent(input, Encoding.UTF8, "application/json");

            // Act
            var response = await client.SendAsync(request);
       
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("10", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ResourceFilter_ChangesInputFormatters_XmlDenied()
        {
            // Arrange
            var input =
                "<DummyClass xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\">" +
                "<SampleInt>10</SampleInt>" +
                "</DummyClass>";

            // There's nothing that can deserialize the body, so the result contains the default
            // value.
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Json");
            request.Content = new StringContent(input, Encoding.UTF8, "application/xml");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("0", await response.Content.ReadAsStringAsync());
        }
    }
}