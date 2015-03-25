using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Framework.Logging;
using Xunit;

namespace E2ETests
{
    public partial class SmokeTests
    {
        private void VerifyStaticContentServed()
        {
            _logger.LogInformation("Validating if static contents are served..");
            _logger.LogInformation("Fetching favicon.ico..");
            var response = _httpClient.GetAsync("favicon.ico").Result;
            ThrowIfResponseStatusNotOk(response);
            _logger.LogInformation("Etag received: {etag}", response.Headers.ETag.Tag);

            //Check if you receive a NotModified on sending an etag
            _logger.LogInformation("Sending an IfNoneMatch header with e-tag");
            _httpClient.DefaultRequestHeaders.IfNoneMatch.Add(response.Headers.ETag);
            response = _httpClient.GetAsync("favicon.ico").Result;
            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
            _httpClient.DefaultRequestHeaders.IfNoneMatch.Clear();
            _logger.LogInformation("Successfully received a NotModified status");

            _logger.LogInformation("Fetching /Content/bootstrap.css..");
            response = _httpClient.GetAsync("Content/bootstrap.css").Result;
            ThrowIfResponseStatusNotOk(response);
            _logger.LogInformation("Verified static contents are served successfully");
        }

        private void VerifyHomePage(HttpResponseMessage response, string responseContent, bool useNtlmAuthentication = false)
        {
            // This seems to not print anything if the successive Assert fails.
            //_logger.WriteVerbose("Home page content : {0}", responseContent);

            Console.WriteLine("Home page content : {0}", responseContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            ValidateLayoutPage(responseContent);
            Assert.Contains(PrefixBaseAddress("<a href=\"/{0}/Store/Details/"), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<title>Home Page – MVC Music Store</title>", responseContent, StringComparison.OrdinalIgnoreCase);

            if (!useNtlmAuthentication)
            {
                //We don't display these for Ntlm
                Assert.Contains("Register", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Login", responseContent, StringComparison.OrdinalIgnoreCase);
            }

            Assert.Contains("www.github.com/aspnet/MusicStore", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/Images/home-showcase.png", responseContent, StringComparison.OrdinalIgnoreCase);
            _logger.LogInformation("Application initialization successful.");

            _logger.LogInformation("Application runtime information");
            var runtimeResponse = _httpClient.GetAsync("runtimeinfo").Result;

            if (_startParameters.RuntimeFlavor != RuntimeFlavor.CoreClr)
            {
                // Runtime info middleware broken on coreclr.
                ThrowIfResponseStatusNotOk(runtimeResponse);
            }

            var runtimeInfo = runtimeResponse.Content.ReadAsStringAsync().Result;
            _logger.LogInformation(runtimeInfo);
        }

        private string PrefixBaseAddress(string url)
        {
#if DNX451
            url = (_startParameters.ServerType == ServerType.IISNativeModule ||
                _startParameters.ServerType == ServerType.IIS) ?
                string.Format(url, _startParameters.IISApplication.VirtualDirectoryName) :
                string.Format(url, string.Empty);
#else
            url = string.Format(url, string.Empty);
#endif

            return url.Replace("//", "/").Replace("%2F%2F", "%2F").Replace("%2F/", "%2F");
        }

        private void ValidateLayoutPage(string responseContent)
        {
            Assert.Contains("ASP.NET MVC Music Store", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(PrefixBaseAddress("<li><a href=\"/{0}\">Home</a></li>"), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(PrefixBaseAddress("<a href=\"/{0}/Store\" class=\"dropdown-toggle\" data-toggle=\"dropdown\">Store <b class=\"caret\"></b></a>"), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<ul class=\"dropdown-menu\">", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<li class=\"divider\"></li>", responseContent, StringComparison.OrdinalIgnoreCase);
        }

        private void AccessStoreWithoutPermissions(string email = null)
        {
            _logger.LogInformation("Trying to access StoreManager that needs ManageStore claim with the current user : {email}", email ?? "Anonymous");
            var response = _httpClient.GetAsync("Admin/StoreManager/").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            ValidateLayoutPage(responseContent);
            Assert.Contains("<title>Log in – MVC Music Store</title>", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<h4>Use a local account to log in.</h4>", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Equal<string>(_applicationBaseUrl + PrefixBaseAddress("Account/Login?ReturnUrl=%2F{0}%2FAdmin%2FStoreManager%2F"), response.RequestMessage.RequestUri.AbsoluteUri);
            _logger.LogInformation("Redirected to login page as expected.");
        }

        private void AccessStoreWithPermissions()
        {
            _logger.LogInformation("Trying to access the store inventory..");
            var response = _httpClient.GetAsync("Admin/StoreManager/").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Equal<string>(_applicationBaseUrl + "Admin/StoreManager/", response.RequestMessage.RequestUri.AbsoluteUri);
            _logger.LogInformation("Successfully acccessed the store inventory");
        }

        private void RegisterUserWithNonMatchingPasswords()
        {
            _logger.LogInformation("Trying to create user with not matching password and confirm password");
            var response = _httpClient.GetAsync("Account/Register").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            ValidateLayoutPage(responseContent);

            var generatedEmail = Guid.NewGuid().ToString().Replace("-", string.Empty) + "@test.com";
            _logger.LogInformation("Creating a new user with name '{email}'", generatedEmail);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Email", generatedEmail),
                    new KeyValuePair<string, string>("Password", "Password~1"),
                    new KeyValuePair<string, string>("ConfirmPassword", "Password~2"),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Register")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = _httpClient.PostAsync("Account/Register", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Null(_httpClientHandler.CookieContainer.GetCookies(new Uri(_applicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Application"));
            Assert.Contains("<div class=\"validation-summary-errors text-danger\" data-valmsg-summary=\"true\"><ul><li>The password and confirmation password do not match.</li>", responseContent, StringComparison.OrdinalIgnoreCase);
            _logger.LogInformation("Server side model validator rejected the user '{email}''s registration as passwords do not match.", generatedEmail);
        }

        private string RegisterValidUser()
        {
            var response = _httpClient.GetAsync("Account/Register").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            ValidateLayoutPage(responseContent);

            var generatedEmail = Guid.NewGuid().ToString().Replace("-", string.Empty) + "@test.com";
            _logger.LogInformation("Creating a new user with name '{email}'", generatedEmail);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Email", generatedEmail),
                    new KeyValuePair<string, string>("Password", "Password~1"),
                    new KeyValuePair<string, string>("ConfirmPassword", "Password~1"),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Register")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = _httpClient.PostAsync("Account/Register", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;

            //Account verification
            Assert.Equal<string>(_applicationBaseUrl + "Account/Register", response.RequestMessage.RequestUri.AbsoluteUri);
            Assert.Contains("For DEMO only: You can click this link to confirm the email:", responseContent, StringComparison.OrdinalIgnoreCase);
            var startIndex = responseContent.IndexOf("[[<a href=\"", 0) + "[[<a href=\"".Length;
            var endIndex = responseContent.IndexOf("\">link</a>]]", startIndex);
            var confirmUrl = responseContent.Substring(startIndex, endIndex - startIndex);
            confirmUrl = WebUtility.HtmlDecode(confirmUrl);
            response = _httpClient.GetAsync(confirmUrl).Result;
            ThrowIfResponseStatusNotOk(response);
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains("Thank you for confirming your email.", responseContent, StringComparison.OrdinalIgnoreCase);
            return generatedEmail;
        }

        private void RegisterExistingUser(string email)
        {
            _logger.LogInformation("Trying to register a user with name '{email}' again", email);
            var response = _httpClient.GetAsync("Account/Register").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            _logger.LogInformation("Creating a new user with name '{email}'", email);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Email", email),
                    new KeyValuePair<string, string>("Password", "Password~1"),
                    new KeyValuePair<string, string>("ConfirmPassword", "Password~1"),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Register")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = _httpClient.PostAsync("Account/Register", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains(string.Format("User name &#x27;{0}&#x27; is already taken.", email), responseContent, StringComparison.OrdinalIgnoreCase);
            _logger.LogInformation("Identity threw a valid exception that user '{email}' already exists in the system", email);
        }

        private void SignOutUser(string email)
        {
            _logger.LogInformation("Signing out from '{email}''s session", email);
            var response = _httpClient.GetAsync(string.Empty).Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            ValidateLayoutPage(responseContent);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/LogOff")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = _httpClient.PostAsync("Account/LogOff", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;

            if (!Helpers.RunningOnMono)
            {
                Assert.Contains("ASP.NET MVC Music Store", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Register", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Login", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("www.github.com/aspnet/MusicStore", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("/Images/home-showcase.png", responseContent, StringComparison.OrdinalIgnoreCase);
                //Verify cookie cleared on logout
                Assert.Null(_httpClientHandler.CookieContainer.GetCookies(new Uri(_applicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Application"));
                _logger.LogInformation("Successfully signed out of '{email}''s session", email);
            }
            else
            {
                //Bug in Mono - on logout the cookie is not cleared in the cookie container and not redirected. Work around by reinstantiating the httpClient.
                _httpClientHandler = new HttpClientHandler();
                _httpClient = new HttpClient(_httpClientHandler) { BaseAddress = new Uri(_applicationBaseUrl) };
            }
        }

        private void SignInWithInvalidPassword(string email, string invalidPassword)
        {
            var response = _httpClient.GetAsync("Account/Login").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            _logger.LogInformation("Signing in with user '{email}'", email);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Email", email),
                    new KeyValuePair<string, string>("Password", invalidPassword),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Login")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = _httpClient.PostAsync("Account/Login", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains("<div class=\"validation-summary-errors text-danger\"><ul><li>Invalid login attempt.</li>", responseContent, StringComparison.OrdinalIgnoreCase);
            //Verify cookie not sent
            Assert.Null(_httpClientHandler.CookieContainer.GetCookies(new Uri(_applicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Application"));
            _logger.LogInformation("Identity successfully prevented an invalid user login.");
        }

        private void SignInWithUser(string email, string password)
        {
            var response = _httpClient.GetAsync("Account/Login").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            _logger.LogInformation("Signing in with user '{email}'", email);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Email", email),
                    new KeyValuePair<string, string>("Password", password),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Login")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = _httpClient.PostAsync("Account/Login", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains(string.Format("Hello {0}!", email), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Log off", responseContent, StringComparison.OrdinalIgnoreCase);
            //Verify cookie sent
            Assert.NotNull(_httpClientHandler.CookieContainer.GetCookies(new Uri(_applicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Application"));
            _logger.LogInformation("Successfully signed in with user '{email}'", email);
        }

        private void ChangePassword(string email)
        {
            var response = _httpClient.GetAsync("Manage/ChangePassword").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("OldPassword", "Password~1"),
                    new KeyValuePair<string, string>("NewPassword", "Password~2"),
                    new KeyValuePair<string, string>("ConfirmPassword", "Password~2"),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Manage/ChangePassword")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = _httpClient.PostAsync("Manage/ChangePassword", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains("Your password has been changed.", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(_httpClientHandler.CookieContainer.GetCookies(new Uri(_applicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Application"));
            _logger.LogInformation("Successfully changed the password for user '{email}'", email);
        }

        private string CreateAlbum()
        {
            var albumName = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 12);
#if DNX451
            string dataFromHub = null;
            var OnReceivedEvent = new AutoResetEvent(false);
            var hubConnection = new HubConnection(_applicationBaseUrl + "SignalR");
            hubConnection.Received += (data) =>
            {
                _logger.LogVerbose("Data received by SignalR client: {receivedData}", data);
                dataFromHub = data;
                OnReceivedEvent.Set();
            };

            IHubProxy proxy = hubConnection.CreateHubProxy("Announcement");
            hubConnection.Start().Wait();
#endif
            _logger.LogInformation("Trying to create an album with name '{album}'", albumName);
            var response = _httpClient.GetAsync("Admin/StoreManager/create").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Admin/StoreManager/create")),
                    new KeyValuePair<string, string>("GenreId", "1"),
                    new KeyValuePair<string, string>("ArtistId", "1"),
                    new KeyValuePair<string, string>("Title", albumName),
                    new KeyValuePair<string, string>("Price", "9.99"),
                    new KeyValuePair<string, string>("AlbumArtUrl", "http://myapp/testurl"),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = _httpClient.PostAsync("Admin/StoreManager/create", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Equal<string>(_applicationBaseUrl + "Admin/StoreManager", response.RequestMessage.RequestUri.AbsoluteUri);
            Assert.Contains(albumName, responseContent);
#if DNX451
            _logger.LogInformation("Waiting for the SignalR client to receive album created announcement");
            OnReceivedEvent.WaitOne(TimeSpan.FromSeconds(10));
            dataFromHub = dataFromHub ?? "No relevant data received from Hub";
            Assert.Contains(albumName, dataFromHub);
#endif
            _logger.LogInformation("Successfully created an album with name '{album}' in the store", albumName);
            return albumName;
        }

        private string FetchAlbumIdFromName(string albumName)
        {
            _logger.LogInformation("Fetching the album id of '{album}'", albumName);
            var response = _httpClient.GetAsync(string.Format("Admin/StoreManager/GetAlbumIdFromName?albumName={0}", albumName)).Result;
            ThrowIfResponseStatusNotOk(response);
            var albumId = response.Content.ReadAsStringAsync().Result;
            _logger.LogInformation("Album id for album '{album}' is '{id}'", albumName, albumId);
            return albumId;
        }

        private void VerifyAlbumDetails(string albumId, string albumName)
        {
            _logger.LogInformation("Getting details of album with Id '{id}'", albumId);
            var response = _httpClient.GetAsync(string.Format("Admin/StoreManager/Details?id={0}", albumId)).Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains(albumName, responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("http://myapp/testurl", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(PrefixBaseAddress(string.Format("<a href=\"/{0}/Admin/StoreManager/Edit?id={1}\">Edit</a>", "{0}", albumId)), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(PrefixBaseAddress("<a href=\"/{0}/Admin/StoreManager\">Back to List</a>"), responseContent, StringComparison.OrdinalIgnoreCase);
        }

        private void VerifyStatusCodePages()
        {
            _logger.LogInformation("Getting details of a non-existing album with Id '-1'");
            var response = _httpClient.GetAsync("Admin/StoreManager/Details?id=-1").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains("Item not found.", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(PrefixBaseAddress("/{0}/Home/StatusCodePage"), response.RequestMessage.RequestUri.AbsolutePath);
        }

        // This gets the view that non-admin users get to see.
        private void GetAlbumDetailsFromStore(string albumId, string albumName)
        {
            _logger.LogInformation("Getting details of album with Id '{id}'", albumId);
            var response = _httpClient.GetAsync(string.Format("Store/Details/{0}", albumId)).Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains(albumName, responseContent, StringComparison.OrdinalIgnoreCase);
        }

        private void AddAlbumToCart(string albumId, string albumName)
        {
            _logger.LogInformation("Adding album id '{albumId}' to the cart", albumId);
            var response = _httpClient.GetAsync(string.Format("ShoppingCart/AddToCart?id={0}", albumId)).Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains(albumName, responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<span class=\"glyphicon glyphicon glyphicon-shopping-cart\"></span>", responseContent, StringComparison.OrdinalIgnoreCase);
            _logger.LogInformation("Verified that album is added to cart");
        }

        private void CheckOutCartItems()
        {
            _logger.LogInformation("Checking out the cart contents...");
            var response = _httpClient.GetAsync("Checkout/AddressAndPayment").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;

            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Checkout/AddressAndPayment")),
                    new KeyValuePair<string, string>("FirstName", "FirstNameValue"),
                    new KeyValuePair<string, string>("LastName", "LastNameValue"),
                    new KeyValuePair<string, string>("Address", "AddressValue"),
                    new KeyValuePair<string, string>("City", "Redmond"),
                    new KeyValuePair<string, string>("State", "WA"),
                    new KeyValuePair<string, string>("PostalCode", "98052"),
                    new KeyValuePair<string, string>("Country", "USA"),
                    new KeyValuePair<string, string>("Phone", "PhoneValue"),
                    new KeyValuePair<string, string>("Email", "email@email.com"),
                    new KeyValuePair<string, string>("PromoCode", "FREE"),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = _httpClient.PostAsync("Checkout/AddressAndPayment", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains("<h2>Checkout Complete</h2>", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(_applicationBaseUrl + "Checkout/Complete/", response.RequestMessage.RequestUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
        }

        private void DeleteAlbum(string albumId, string albumName)
        {
            _logger.LogInformation("Deleting album '{album}' from the store..", albumName);

            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("id", albumId)
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            var response = _httpClient.PostAsync("Admin/StoreManager/RemoveAlbum", content).Result;
            ThrowIfResponseStatusNotOk(response);

            _logger.LogInformation("Verifying if the album '{album}' is deleted from store", albumName);
            response = _httpClient.GetAsync(string.Format("Admin/StoreManager/GetAlbumIdFromName?albumName={0}", albumName)).Result;
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            _logger.LogInformation("Album '{album}' with id '{Id}' is successfully deleted from the store.", albumName, albumId);
        }

        private void ThrowIfResponseStatusNotOk(HttpResponseMessage response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError(response.Content.ReadAsStringAsync().Result);
                throw new Exception(string.Format("Received the above response with status code : {0}", response.StatusCode));
            }
        }
    }
}
