// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class RazorPagesTest : IClassFixture<MvcTestFixture<RazorPagesWebSite.Startup>>
    {
        public RazorPagesTest(MvcTestFixture<RazorPagesWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task Page_Handler_FormAction()
        {
            // Arrange & Act
            var content = await Client.GetStringAsync("http://localhost/HandlerTestPage/Customer");

            // Assert
            Assert.StartsWith("Method: OnGetCustomer", content.Trim());
        }

        [Fact]
        public async Task Page_Handler_Async()
        {
            // Arrange
            var getResponse = await Client.GetAsync("http://localhost/HandlerTestPage");
            var getResponseBody = await getResponse.Content.ReadAsStringAsync();
            var formToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(getResponseBody, "/ModelHandlerTestPage");
            var cookie = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(getResponse);

            var postRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost/HandlerTestPage");
            postRequest.Headers.Add("Cookie", cookie.Key + "=" + cookie.Value);
            postRequest.Headers.Add("RequestVerificationToken", formToken);

            // Act
            var response = await Client.SendAsync(postRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.StartsWith("Method: OnPostAsync", content.Trim());
        }

        [Fact]
        public async Task Page_Handler_AsyncFormAction()
        {
            // Arrange & Act
            var content = await Client.GetStringAsync("http://localhost/HandlerTestPage/ViewCustomer");

            // Assert
            Assert.StartsWith("Method: OnGetViewCustomerAsync", content.Trim());
        }

        [Fact]
        public async Task Page_Handler_ReturnTypeImplementsIActionResult()
        {
            // Arrange
            var getResponse = await Client.GetAsync("http://localhost/HandlerTestPage");
            var getResponseBody = await getResponse.Content.ReadAsStringAsync();
            var formToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(getResponseBody, "/ModelHandlerTestPage");
            var cookie = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(getResponse);

            var postRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost/HandlerTestPage/CustomActionResult");
            postRequest.Headers.Add("Cookie", cookie.Key + "=" + cookie.Value);
            postRequest.Headers.Add("RequestVerificationToken", formToken);
            // Act
            var response = await Client.SendAsync(postRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("CustomActionResult", content);
        }

        [Fact]
        public async Task Page_Handler_AsyncReturnTypeImplementsIActionResult()
        {
            // Arrange & Act
            var content = await Client.GetStringAsync("http://localhost/HandlerTestPage/CustomActionResult");

            // Assert
            Assert.Equal("CustomActionResult", content);
        }


        [Fact]
        public async Task PageModel_Handler_FormAction()
        {
            // Arrange & Act
            var content = await Client.GetStringAsync("http://localhost/ModelHandlerTestPage/Customer");

            // Assert
            Assert.StartsWith("Method: OnGetCustomer", content.Trim());
        }

        [Fact]
        public async Task PageModel_Handler_Async()
        {
            // Arrange
            var getResponse = await Client.GetAsync("http://localhost/ModelHandlerTestPage");
            var getResponseBody = await getResponse.Content.ReadAsStringAsync();
            var formToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(getResponseBody, "/ModelHandlerTestPage");
            var cookie = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(getResponse);

            var postRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost/ModelHandlerTestPage");
            postRequest.Headers.Add("Cookie", cookie.Key + "=" + cookie.Value);
            postRequest.Headers.Add("RequestVerificationToken", formToken);

            // Act
            var response = await Client.SendAsync(postRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.StartsWith("Method: OnPostAsync", content.Trim());
        }

        [Fact]
        public async Task PageModel_Handler_AsyncFormAction()
        {
            // Arrange & Act
            var content = await Client.GetStringAsync("http://localhost/ModelHandlerTestPage/ViewCustomer");

            // Assert
            Assert.StartsWith("Method: OnGetViewCustomerAsync", content.Trim());
        }

        [Fact]
        public async Task PageModel_Handler_ReturnTypeImplementsIActionResult()
        {
            // Arrange
            var getResponse = await Client.GetAsync("http://localhost/ModelHandlerTestPage");
            var getResponseBody = await getResponse.Content.ReadAsStringAsync();
            var formToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(getResponseBody, "/ModelHandlerTestPage");
            var cookie = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(getResponse);

            var postRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost/ModelHandlerTestPage/CustomActionResult");
            postRequest.Headers.Add("Cookie", cookie.Key + "=" + cookie.Value);
            postRequest.Headers.Add("RequestVerificationToken", formToken);
            // Act
            var response = await Client.SendAsync(postRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("CustomActionResult", content);
        }

        [Fact]
        public async Task PageModel_Handler_AsyncReturnTypeImplementsIActionResult()
        {
            // Arrange & Act
            var content = await Client.GetStringAsync("http://localhost/ModelHandlerTestPage/CustomActionResult");

            // Assert
            Assert.Equal("CustomActionResult", content);
        }

        [Fact]
        public async Task Page_SetsPath()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/PathSet");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Path: /PathSet.cshtml", content.Trim());
        }

        // Tests that RazorPage includes InvalidTagHelperIndexerAssignment which is called when the page has an indexer
        // Issue https://github.com/aspnet/Mvc/issues/5920
        [Fact]
        public async Task TagHelper_InvalidIndexerDoesNotFail()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/TagHelpers");

            // Act
            var response = await Client.SendAsync(request);

                // Assert
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.StartsWith("<a href=\"/Show?id=2\">Post title</a>", content.Trim());
        }

        [Fact]
        public async Task NoPage_NotFound()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/NoPage");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task HelloWorld_CanGetContent()
        {
            // Arrange
            // Note: If the route in this test case ever changes, the negative test case
            // RazorPagesWithBasePathTest.PageOutsideBasePath_IsNotRouteable needs to be updated too.
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/HelloWorld");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello, World!", content.Trim());
        }

        [Fact]
        public async Task HelloWorldWithRoute_CanGetContent()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/HelloWorldWithRoute/Some/Path/route");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello, route!", content.Trim());
        }

        [Fact]
        public async Task HelloWorldWithHandler_CanGetContent()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/HelloWorldWithHandler?message=handler");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello, handler!", content.Trim());
        }

        [Fact]
        public async Task HelloWorldWithPageModelHandler_CanPostContent()
        {
            // Arrange
            var getRequest = new HttpRequestMessage(HttpMethod.Get, "http://localhost/HelloWorldWithPageModelHandler?message=message");
            var getResponse = await Client.SendAsync(getRequest);
            var getResponseBody = await getResponse.Content.ReadAsStringAsync();
            var formToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(getResponseBody, "/HelloWorlWithPageModelHandler");
            var cookie = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(getResponse);


            var postRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost/HelloWorldWithPageModelHandler");
            postRequest.Headers.Add("Cookie", cookie.Key + "=" + cookie.Value);
            postRequest.Headers.Add("RequestVerificationToken", formToken);

            // Act
            var response = await Client.SendAsync(postRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.StartsWith("Hello, You posted!", content.Trim());
        }

        [Fact]
        public async Task HelloWorldWithPageModelHandler_CanGetContent()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/HelloWorldWithPageModelHandler?message=pagemodel");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.StartsWith("Hello, pagemodel!", content.Trim());
        }

        [Fact]
        public async Task PageWithoutContent()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/PageWithoutContent/No/Content/Path");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("", content);
        }

        [Fact]
        public async Task ViewReturnsPage()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/OnGetView");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("The message: From OnGet", content.Trim());
        }

        [Fact]
        public async Task TempData_SetTempDataInPage_CanReadValue()
        {
            // Arrange 1
            var url = "http://localhost/TempData/SetTempDataOnPageAndRedirect?message=Hi1";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act 1
            var response = await Client.SendAsync(request);

            // Assert 1
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            // Arrange 2
            request = new HttpRequestMessage(HttpMethod.Get, response.Headers.Location);
            request.Headers.Add("Cookie", GetCookie(response));

            // Act2
            response = await Client.SendAsync(request);

            // Assert 2
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hi1", content.Trim());
        }

        [Fact]
        public async Task TempData_SetTempDataInPageModel_CanReadValue()
        {
            // Arrange 1
            var url = "http://localhost/TempData/SetTempDataOnPageModelAndRedirect?message=Hi2";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act 1
            var response = await Client.SendAsync(request);

            // Assert 1
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            // Arrange 2
            request = new HttpRequestMessage(HttpMethod.Get, response.Headers.Location);
            request.Headers.Add("Cookie", GetCookie(response));

            // Act 2
            response = await Client.SendAsync(request);

            // Assert 2
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hi2", content.Trim());
        }

        [Fact]
        public async Task AuthorizePage_AddsAuthorizationForSpecificPages()
        {
            // Arrange
            var url = "/HelloWorldWithAuth";
            
            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Login?ReturnUrl=%2FHelloWorldWithAuth", response.Headers.Location.PathAndQuery);
        }


        [Fact]
        public async Task PageStart_IsDiscoveredWhenRootDirectoryIsNotSpecified()
        {
            // Test for https://github.com/aspnet/Mvc/issues/5915
            //Arrange
            var expected = $"Hello from _PageStart{Environment.NewLine}Hello from /Pages/WithPageStart/Index.cshtml!";

            // Act
            var response = await Client.GetStringAsync("/Pages/WithPageStart");

            // Assert
            Assert.Equal(expected, response.Trim());
        }

        [Fact]
        public async Task PageImport_IsDiscoveredWhenRootDirectoryIsNotSpecified()
        {
            // Test for https://github.com/aspnet/Mvc/issues/5915
            //Arrange
            var expected = "Hello from CustomService!";

            // Act
            var response = await Client.GetStringAsync("/Pages/WithPageImport");

            // Assert
            Assert.Equal(expected, response.Trim());
        }

        private static string GetCookie(HttpResponseMessage response)
        {
            var setCookie = response.Headers.GetValues("Set-Cookie").ToArray();
            return setCookie[0].Split(';').First();
        }

        public class CookieMetadata
        {
            public string Key { get; set; }

            public string Value { get; set; }
        }
    }
}
