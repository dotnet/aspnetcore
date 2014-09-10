// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class AntiForgeryTests
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("AntiForgeryWebSite");
        private readonly Action<IApplicationBuilder> _app = new AntiForgeryWebSite.Startup().Configure;

        [Fact]
        public async Task MultipleAFTokensWithinTheSamePage_GeneratesASingleCookieToken()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Account/Login");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var header = Assert.Single(response.Headers.GetValues("X-Frame-Options"));
            Assert.Equal("SAMEORIGIN", header);

            var setCookieHeader = response.Headers.GetValues("Set-Cookie").ToArray();

            // Even though there are two forms there should only be one response cookie, 
            // as for the second form, the cookie from the first token should be reused.
            Assert.Equal(1, setCookieHeader.Length);
            Assert.True(setCookieHeader[0].StartsWith("__RequestVerificationToken"));
        }

        [Fact]
        public async Task MultipleFormPostWithingASingleView_AreAllowed()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // do a get response.
            var getResponse = await client.GetAsync("http://localhost/Account/Login");
            var resposneBody = await getResponse.Content.ReadAsStringAsync();

            // Get the AF token for the second login. If the cookies are generated twice(i.e are different), 
            // this AF token will not work with the first cookie.
            var formToken = AntiForgeryTestHelper.RetrieveAntiForgeryToken(resposneBody, "Account/UseFacebookLogin");
            var cookieToken = AntiForgeryTestHelper.RetrieveAntiForgeryCookie(getResponse);

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Account/Login");
            request.Headers.Add("Cookie", "__RequestVerificationToken=" + cookieToken);
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", formToken),
                new KeyValuePair<string,string>("UserName", "abra"),
                new KeyValuePair<string,string>("Password", "cadabra"),
            };

            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("OK", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task InvalidCookieToken_Throws()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            var cookieToken = "asdad";
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Account/Login");
            request.Headers.Add("Cookie", "__RequestVerificationToken=" + cookieToken);
            var formToken = "AQAAANCMnd8BFdERjHoAwE_Cl-sBAAAADBPoDUIPtEee8EZ40kjaOQAAAAACAAAAAAADZgAAwAAAABAAAABx9"+
                            "2btLE7MLa5AVabrJ3TOAAAAAASAAACgAAAAEAAAAOc8lIs3RfhLkS2fHqBHeuIYAAAACIspnfiEu6QYzrfOul"+
                            "vXbCNm5E7VyKW8FAAAAOD25c81cu0Zi06Myn8Ne1JLOK2K";
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", formToken),
                new KeyValuePair<string,string>("UserName", "abra"),
                new KeyValuePair<string,string>("Password", "cadabra"),
            };

            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
            Assert.Equal("The anti-forgery token could not be decrypted.", ex.Message);
        }

        [Fact]
        public async Task InvalidFormToken_Throws()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            var cookieToken = "AQAAANCMnd8BFdERjHoAwE_Cl-sBAAAADBPoDUIPtEee8EZ40kjaOQAAAAACAAAAAAADZgAAwAAAABAAAAD"+
                              "2ZaQPi5Dq1fUTYj06LxMVAAAAAASAAACgAAAAEAAAADVYzWBsC5SHK_AWCieAFsgYAAAA-XHHnq2Yz2GS-e"+
                              "R8cHq-A2T8BfPHM21GFAAAALpW0H8-5oPxbe2DOKuj8ZG3bohn";
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Account/Login");
            var formToken = "adsad";
            request.Headers.Add("Cookie", "__RequestVerificationToken=" + cookieToken);
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", formToken),
                new KeyValuePair<string,string>("UserName", "abra"),
                new KeyValuePair<string,string>("Password", "cadabra"),
            };

            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
            Assert.Equal("The anti-forgery token could not be decrypted.", ex.Message);
        }

        [Fact]
        public async Task IncompatibleCookieToken_Throws()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            var cookieToken = "AQAAANCMnd8BFdERjHoAwE_Cl-sBAAAADBPoDUIPtEee8EZ40kjaOQAAAAACAAAAAAADZgAAwAAAABAAAAB"+
                              "QrVKmVeuzQHJX3jUAzFNNAAAAAASAAACgAAAAEAAAADd2PjKWhB8NmuaPMZDDutgYAAAAXGTjIRTnjLHqwC"+
                              "KFGx9ZVQOLVfWIGQxiFAAAAIVqLISuhF2sFrd3UQqLDteT0vRu";
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Account/Login");
            request.Headers.Add("Cookie", "__RequestVerificationToken=" + cookieToken);
            var formToken = "AQAAANCMnd8BFdERjHoAwE_Cl-sBAAAADBPoDUIPtEee8EZ40kjaOQAAAAACAAAAAAADZgAAwAAAABAAAABx9"+
                            "2btLE7MLa5AVabrJ3TOAAAAAASAAACgAAAAEAAAAOc8lIs3RfhLkS2fHqBHeuIYAAAACIspnfiEu6QYzrfOul"+
                            "vXbCNm5E7VyKW8FAAAAOD25c81cu0Zi06Myn8Ne1JLOK2K";
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", formToken),
                new KeyValuePair<string,string>("UserName", "abra"),
                new KeyValuePair<string,string>("Password", "cadabra"),
            };

            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
            Assert.Equal("The anti-forgery cookie token and form field token do not match.", ex.Message);
        }

        [Fact]
        public async Task IncompatibleFormToken_Throws()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            var cookieToken = "AQAAANCMnd8BFdERjHoAwE_Cl-sBAAAADBPoDUIPtEee8EZ40kjaOQAAAAACAAAAAAADZgAAwAAAABAAAAD"+
                              "2ZaQPi5Dq1fUTYj06LxMVAAAAAASAAACgAAAAEAAAADVYzWBsC5SHK_AWCieAFsgYAAAA-XHHnq2Yz2GS-e"+
                              "R8cHq-A2T8BfPHM21GFAAAALpW0H8-5oPxbe2DOKuj8ZG3bohn";
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Account/Login");
            var formToken = "AQAAANCMnd8BFdERjHoAwE_Cl-sBAAAADBPoDUIPtEee8EZ40kjaOQAAAAACAAAAAAADZgAAwAAAABAAAAApj"+
                            "_D9vARroIdg2t6sfw06AAAAAASAAACgAAAAEAAAAKMz9G6buL-JM_3eknUq4aoYAAAAuPBN2dc0RsRvrde1V6"+
                            "FBrQYCEuUdx-nSFAAAAEvqGxIwr8zFKO_osS6r953VqjS-";
            request.Headers.Add("Cookie", "__RequestVerificationToken=" + cookieToken);
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", formToken),
                new KeyValuePair<string,string>("UserName", "abra"),
                new KeyValuePair<string,string>("Password", "cadabra"),
            };

            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
            Assert.Equal("The anti-forgery cookie token and form field token do not match.", ex.Message);
        }

        [Fact]
        public async Task MissingCookieToken_Throws()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Account/Login");
            var formToken = "AQAAANCMnd8BFdERjHoAwE_Cl-sBAAAADBPoDUIPtEee8EZ40kjaOQAAAAACAAAAAAADZgAAwAAAABAAAABx9"+
                "2btLE7MLa5AVabrJ3TOAAAAAASAAACgAAAAEAAAAOc8lIs3RfhLkS2fHqBHeuIYAAAACIspnfiEu6QYzrfOulvXbCNm5E7VyK"+
                "W8FAAAAOD25c81cu0Zi06Myn8Ne1JLOK2K";
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", formToken),
                new KeyValuePair<string,string>("UserName", "abra"),
                new KeyValuePair<string,string>("Password", "cadabra"),
            };

            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
            Assert.Equal("The required anti-forgery cookie \"__RequestVerificationToken\" is not present.", ex.Message);
        }

        [Fact]
        public async Task MissingAFToken_Throws()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var cookieToken = "AQAAANCMnd8BFdERjHoAwE_Cl-sBAAAADBPoDUIPtEee8EZ40kjaOQAAAAACAAAAAAADZgAAwAAAABAAAAD"+
                              "2ZaQPi5Dq1fUTYj06LxMVAAAAAASAAACgAAAAEAAAADVYzWBsC5SHK_AWCieAFsgYAAAA-XHHnq2Yz2GS-e"+
                              "R8cHq-A2T8BfPHM21GFAAAALpW0H8-5oPxbe2DOKuj8ZG3bohn";
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Account/Login");
            request.Headers.Add("Cookie", "__RequestVerificationToken=" + cookieToken);
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("UserName", "abra"),
                new KeyValuePair<string,string>("Password", "cadabra"),
            };

            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
            Assert.Equal("The required anti-forgery form field \"__RequestVerificationToken\" is not present.",
                         ex.Message);
        }
    }
}