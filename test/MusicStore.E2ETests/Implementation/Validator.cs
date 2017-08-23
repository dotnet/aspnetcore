using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Xunit;

namespace E2ETests
{
    public partial class Validator
    {
        private static readonly string IdentityCookieName = CookieAuthenticationDefaults.CookiePrefix + IdentityConstants.ApplicationScheme;
        private static readonly string ExternalLoginCookieName = CookieAuthenticationDefaults.CookiePrefix + IdentityConstants.ExternalScheme;
        private HttpClient _httpClient;

        private HttpClientHandler _httpClientHandler;

        private readonly ILogger _logger;

        private readonly DeploymentResult _deploymentResult;

        public Validator(
            HttpClient httpClient,
            HttpClientHandler httpClientHandler,
            ILogger logger,
            DeploymentResult deploymentResult)
        {
            _httpClient = httpClient;
            _httpClientHandler = httpClientHandler;
            _logger = logger;
            _deploymentResult = deploymentResult;
        }

        private Task<HttpResponseMessage> DoGetAsync(string uri)
            => DoGetAsync(new Uri(uri, UriKind.RelativeOrAbsolute));

        private async Task<HttpResponseMessage> DoGetAsync(Uri uri)
        {
            _logger.LogInformation("GET {0}", uri.ToString());
            var resp = await _httpClient.GetAsync(uri);
            LogHeaders(resp, LogLevel.Information);
            return resp;
        }

        private Task<HttpResponseMessage> DoPostAsync(string uri, HttpContent content)
            => DoPostAsync(new Uri(uri, UriKind.RelativeOrAbsolute), content);

        private async Task<HttpResponseMessage> DoPostAsync(Uri uri, HttpContent content)
        {
            _logger.LogInformation("POST {0}", uri.ToString());
            var resp = await _httpClient.PostAsync(uri, content);
            LogHeaders(resp, LogLevel.Information);
            return resp;
        }
        private void LogHeaders(HttpResponseMessage response, LogLevel logLevel)
        {
            var requestHeaders = string.Join("\n", response.RequestMessage.Headers.Select(h => h.Key + "=" + string.Join(",", h.Value)));
            _logger.Log(logLevel, 0,
               new FormattedLogValues("Request headers: {0}", requestHeaders),
               exception: null,
               formatter: (o, e) => o.ToString());

            var responseHeaders = string.Join("\n", response.Headers.Select(h => h.Key + "=" + string.Join(",", h.Value)));
            _logger.Log(logLevel, 0,
                new FormattedLogValues("Response headers: {0}", responseHeaders),
                exception: null,
                formatter: (o, e) => o.ToString());
        }

        public async Task VerifyHomePage(
            HttpResponseMessage response,
            bool useNtlmAuthentication = false)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogInformation("Home page content : {0}", responseContent);
            }

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            ValidateLayoutPage(responseContent);
            Assert.Contains(PrefixBaseAddress("<a href=\"/{0}/Store/Details/"), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<title>Home Page – ASP.NET MVC Music Store</title>", responseContent, StringComparison.OrdinalIgnoreCase);

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
            //var runtimeResponse = await DoGetAsync("runtimeinfo");

            // https://github.com/aspnet/Diagnostics/issues/108
            if (_deploymentResult.DeploymentParameters.RuntimeFlavor != RuntimeFlavor.CoreClr)
            {
                //Helpers.ThrowIfResponseStatusNotOk(runtimeResponse, _logger);
            }

            //var runtimeInfo = await runtimeResponse.Content.ReadAsStringAsync();
            //_logger.LogInformation(runtimeInfo);
        }

        public async Task VerifyNtlmHomePage(HttpResponseMessage response)
        {
            await VerifyHomePage(response, useNtlmAuthentication: true);

            var homePageContent = await response.Content.ReadAsStringAsync();

            //Check if the user name appears in the page
            Assert.Contains(
                WindowsIdentity.GetCurrent().Name,
                homePageContent,
                StringComparison.OrdinalIgnoreCase);
        }

        public void ValidateLayoutPage(string responseContent)
        {
            Assert.Contains("ASP.NET MVC Music Store", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(PrefixBaseAddress("<li><a href=\"/{0}\">Home</a></li>"), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(PrefixBaseAddress("<a class=\"dropdown-toggle\" data-toggle=\"dropdown\" href=\"/{0}/Store\">Store <b class=\"caret\"></b></a>"), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<ul class=\"dropdown-menu\">", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<li class=\"divider\"></li>", responseContent, StringComparison.OrdinalIgnoreCase);
        }

        public async Task VerifyStaticContentServed()
        {
            _logger.LogInformation("Validating if static contents are served..");
            _logger.LogInformation("Fetching favicon.ico..");
            var response = await DoGetAsync("favicon.ico");
            await ThrowIfResponseStatusNotOk(response);
            Assert.NotNull(response.Headers.ETag);
            _logger.LogInformation("Etag received: {etag}", response.Headers.ETag.Tag);

            //Check if you receive a NotModified on sending an etag
            _logger.LogInformation("Sending an IfNoneMatch header with e-tag");
            _httpClient.DefaultRequestHeaders.IfNoneMatch.Add(response.Headers.ETag);
            response = await DoGetAsync("favicon.ico");
            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
            _httpClient.DefaultRequestHeaders.IfNoneMatch.Clear();
            _logger.LogInformation("Successfully received a NotModified status");

            _logger.LogInformation("Fetching /Content/bootstrap.css..");
            response = await DoGetAsync("Content/bootstrap.css");
            await ThrowIfResponseStatusNotOk(response);
            _logger.LogInformation("Verified static contents are served successfully");
        }

        public async Task AccessStoreWithPermissions()
        {
            _logger.LogInformation("Trying to access the store inventory..");
            var response = await DoGetAsync("Admin/StoreManager/");
            await ThrowIfResponseStatusNotOk(response);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(_deploymentResult.ApplicationBaseUri + "Admin/StoreManager/", response.RequestMessage.RequestUri.AbsoluteUri);
            _logger.LogInformation("Successfully acccessed the store inventory");
        }

        private string PrefixBaseAddress(string url)
        {
            url = string.Format(url, string.Empty);

            return url.Replace("//", "/").Replace("%2F%2F", "%2F").Replace("%2F/", "%2F");
        }

        // Making a request to a protected resource that this user does not have access to - should
        // automatically redirect to the configured access denied page
        public async Task AccessStoreWithoutPermissions(string email = null)
        {
            _logger.LogInformation("Trying to access StoreManager that needs ManageStore claim with the current user : {email}", email ?? "Anonymous");
            var response = await DoGetAsync("Admin/StoreManager/");
            var responseContent = await response.Content.ReadAsStringAsync();
            ValidateLayoutPage(responseContent);

            if (email == null)
            {
                Assert.Contains("<title>Log in – ASP.NET MVC Music Store</title>", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("<h4>Use a local account to log in.</h4>", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Equal(_deploymentResult.ApplicationBaseUri + PrefixBaseAddress("Account/Login?ReturnUrl=%2F{0}%2FAdmin%2FStoreManager%2F"), response.RequestMessage.RequestUri.AbsoluteUri);
                _logger.LogInformation("Redirected to login page as expected.");
            }
            else
            {
                Assert.Contains("<title>Access denied due to insufficient permissions – ASP.NET MVC Music Store</title>", responseContent, StringComparison.OrdinalIgnoreCase);
            }
        }

        public async Task RegisterUserWithNonMatchingPasswords()
        {
            _logger.LogInformation("Trying to create user with not matching password and confirm password");
            var response = await DoGetAsync("Account/Register");
            await ThrowIfResponseStatusNotOk(response);
            LogHeaders(response, LogLevel.Trace);

            var responseContent = await response.Content.ReadAsStringAsync();
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
            response = await DoPostAsync("Account/Register", content);
            await ThrowIfResponseStatusNotOk(response);
            responseContent = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(IdentityCookieName, GetCookieNames());
            Assert.Contains("<div class=\"text-danger validation-summary-errors\" data-valmsg-summary=\"true\"><ul><li>The password and confirmation password do not match.</li>", responseContent, StringComparison.OrdinalIgnoreCase);
            _logger.LogInformation("Server side model validator rejected the user '{email}''s registration as passwords do not match.", generatedEmail);
        }

        public async Task<string> RegisterValidUser()
        {
            var response = await DoGetAsync("Account/Register");
            await ThrowIfResponseStatusNotOk(response);
            var responseContent = await response.Content.ReadAsStringAsync();
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
            response = await DoPostAsync("Account/Register", content);
            responseContent = await response.Content.ReadAsStringAsync();

            //Account verification
            Assert.Equal(_deploymentResult.ApplicationBaseUri + "Account/Register", response.RequestMessage.RequestUri.AbsoluteUri);
            Assert.Contains("For DEMO only: You can click this link to confirm the email:", responseContent, StringComparison.OrdinalIgnoreCase);
            var startIndex = responseContent.IndexOf("[[<a href=\"", 0) + "[[<a href=\"".Length;
            var endIndex = responseContent.IndexOf("\">link</a>]]", startIndex);
            var confirmUrl = responseContent.Substring(startIndex, endIndex - startIndex);
            confirmUrl = WebUtility.HtmlDecode(confirmUrl);
            response = await DoGetAsync(confirmUrl);
            await ThrowIfResponseStatusNotOk(response);
            responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Thank you for confirming your email.", responseContent, StringComparison.OrdinalIgnoreCase);
            return generatedEmail;
        }

        public async Task RegisterExistingUser(string email)
        {
            _logger.LogInformation("Trying to register a user with name '{email}' again", email);
            var response = await DoGetAsync("Account/Register");
            await ThrowIfResponseStatusNotOk(response);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Creating a new user with name '{email}'", email);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Email", email),
                    new KeyValuePair<string, string>("Password", "Password~1"),
                    new KeyValuePair<string, string>("ConfirmPassword", "Password~1"),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Register")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = await DoPostAsync("Account/Register", content);
            responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains(string.Format("User name &#x27;{0}&#x27; is already taken.", email), responseContent, StringComparison.OrdinalIgnoreCase);
            _logger.LogInformation("Identity threw a valid exception that user '{email}' already exists in the system", email);
        }

        public async Task SignOutUser(string email)
        {
            _logger.LogInformation("Signing out from '{email}''s session", email);
            var response = await DoGetAsync(string.Empty);
            await ThrowIfResponseStatusNotOk(response);
            var responseContent = await response.Content.ReadAsStringAsync();
            ValidateLayoutPage(responseContent);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/LogOff")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = await DoPostAsync("Account/LogOff", content);
            responseContent = await response.Content.ReadAsStringAsync();

            Assert.Contains("ASP.NET MVC Music Store", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Register", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Login", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("www.github.com/aspnet/MusicStore", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/Images/home-showcase.png", responseContent, StringComparison.OrdinalIgnoreCase);
            //Verify cookie cleared on logout
            Assert.DoesNotContain(IdentityCookieName, GetCookieNames());
            _logger.LogInformation("Successfully signed out of '{email}''s session", email);
        }

        public async Task SignInWithInvalidPassword(string email, string invalidPassword)
        {
            var response = await DoGetAsync("Account/Login");
            await ThrowIfResponseStatusNotOk(response);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Signing in with user '{email}'", email);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Email", email),
                    new KeyValuePair<string, string>("Password", invalidPassword),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Login")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = await DoPostAsync("Account/Login", content);
            responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("<div class=\"text-danger validation-summary-errors\" data-valmsg-summary=\"true\"><ul><li>Invalid login attempt.</li>", responseContent, StringComparison.OrdinalIgnoreCase);
            //Verify cookie not sent
            Assert.DoesNotContain(IdentityCookieName, GetCookieNames());
            _logger.LogInformation("Identity successfully prevented an invalid user login.");
        }

        public async Task SignInWithUser(string email, string password)
        {
            var response = await DoGetAsync("Account/Login");
            await ThrowIfResponseStatusNotOk(response);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Signing in with user '{email}'", email);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Email", email),
                    new KeyValuePair<string, string>("Password", password),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Login")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = await DoPostAsync("Account/Login", content);
            responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains(string.Format("Hello {0}!", email), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Log off", responseContent, StringComparison.OrdinalIgnoreCase);
            //Verify cookie sent
            Assert.Contains(IdentityCookieName, GetCookieNames());
            _logger.LogInformation("Successfully signed in with user '{email}'", email);
        }

        public async Task ChangePassword(string email)
        {
            var response = await DoGetAsync("Manage/ChangePassword");
            await ThrowIfResponseStatusNotOk(response);
            var responseContent = await response.Content.ReadAsStringAsync();
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("OldPassword", "Password~1"),
                    new KeyValuePair<string, string>("NewPassword", "Password~2"),
                    new KeyValuePair<string, string>("ConfirmPassword", "Password~2"),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Manage/ChangePassword")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = await DoPostAsync("Manage/ChangePassword", content);
            responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Your password has been changed.", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(IdentityCookieName, GetCookieNames());
            _logger.LogInformation("Successfully changed the password for user '{email}'", email);
        }

        public async Task<string> CreateAlbum()
        {
            var albumName = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 12);
            _logger.LogInformation("Trying to create an album with name '{album}'", albumName);
            var response = await DoGetAsync("Admin/StoreManager/create");
            await ThrowIfResponseStatusNotOk(response);
            var responseContent = await response.Content.ReadAsStringAsync();
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
            response = await DoPostAsync("Admin/StoreManager/create", content);
            responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(_deploymentResult.ApplicationBaseUri + "Admin/StoreManager", response.RequestMessage.RequestUri.AbsoluteUri);
            Assert.Contains(albumName, responseContent);
            _logger.LogInformation("Successfully created an album with name '{album}' in the store", albumName);
            return albumName;
        }

        public async Task<string> FetchAlbumIdFromName(string albumName)
        {
            // Run some CORS validation.
            _logger.LogInformation("Fetching the album id of '{album}'", albumName);
            _httpClient.DefaultRequestHeaders.Add("Origin", "http://notpermitteddomain.com");
            var response = await DoGetAsync(string.Format("Admin/StoreManager/GetAlbumIdFromName?albumName={0}", albumName));
            await ThrowIfResponseStatusNotOk(response);
            Assert.False(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));

            _httpClient.DefaultRequestHeaders.Remove("Origin");
            _httpClient.DefaultRequestHeaders.Add("Origin", "http://example.com");
            response = await DoGetAsync(string.Format("Admin/StoreManager/GetAlbumIdFromName?albumName={0}", albumName));
            await ThrowIfResponseStatusNotOk(response);
            Assert.Equal("http://example.com", response.Headers.GetValues("Access-Control-Allow-Origin").First());
            _httpClient.DefaultRequestHeaders.Remove("Origin");

            var albumId = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Album id for album '{album}' is '{id}'", albumName, albumId);
            return albumId;
        }

        public async Task VerifyAlbumDetails(string albumId, string albumName)
        {
            _logger.LogInformation("Getting details of album with Id '{id}'", albumId);
            var response = await DoGetAsync(string.Format("Admin/StoreManager/Details?id={0}", albumId));
            await ThrowIfResponseStatusNotOk(response);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains(albumName, responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("http://myapp/testurl", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(PrefixBaseAddress(string.Format("<a href=\"/{0}/Admin/StoreManager/Edit?id={1}\">Edit</a>", "{0}", albumId)), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(PrefixBaseAddress("<a href=\"/{0}/Admin/StoreManager\">Back to List</a>"), responseContent, StringComparison.OrdinalIgnoreCase);
        }

        public async Task VerifyStatusCodePages()
        {
            _logger.LogInformation("Getting details of a non-existing album with Id '-1'");
            var response = await DoGetAsync("Admin/StoreManager/Details?id=-1");
            await ThrowIfResponseStatusNotOk(response);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Item not found.", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(PrefixBaseAddress("/{0}/Home/StatusCodePage"), response.RequestMessage.RequestUri.AbsolutePath);
        }

        // This gets the view that non-admin users get to see.

        public async Task GetAlbumDetailsFromStore(string albumId, string albumName)
        {
            _logger.LogInformation("Getting details of album with Id '{id}'", albumId);
            var response = await DoGetAsync(string.Format("Store/Details/{0}", albumId));
            await ThrowIfResponseStatusNotOk(response);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains(albumName, responseContent, StringComparison.OrdinalIgnoreCase);
        }

        public async Task AddAlbumToCart(string albumId, string albumName)
        {
            _logger.LogInformation("Adding album id '{albumId}' to the cart", albumId);
            var response = await DoGetAsync(string.Format("ShoppingCart/AddToCart?id={0}", albumId));
            await ThrowIfResponseStatusNotOk(response);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains(albumName, responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<span class=\"glyphicon glyphicon-shopping-cart\"></span>", responseContent, StringComparison.OrdinalIgnoreCase);
            _logger.LogInformation("Verified that album is added to cart");
        }

        public async Task CheckOutCartItems()
        {
            _logger.LogInformation("Checking out the cart contents...");
            var response = await DoGetAsync("Checkout/AddressAndPayment");
            await ThrowIfResponseStatusNotOk(response);
            var responseContent = await response.Content.ReadAsStringAsync();

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
            response = await DoPostAsync("Checkout/AddressAndPayment", content);
            responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("<h2>Checkout Complete</h2>", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(_deploymentResult.ApplicationBaseUri + "Checkout/Complete/", response.RequestMessage.RequestUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
        }

        public async Task DeleteAlbum(string albumId, string albumName)
        {
            _logger.LogInformation("Deleting album '{album}' from the store..", albumName);

            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("id", albumId)
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            var response = await DoPostAsync("Admin/StoreManager/RemoveAlbum", content);
            await ThrowIfResponseStatusNotOk(response);

            _logger.LogInformation("Verifying if the album '{album}' is deleted from store", albumName);
            response = await DoGetAsync(string.Format("Admin/StoreManager/GetAlbumIdFromName?albumName={0}", albumName));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            _logger.LogInformation("Album '{album}' with id '{Id}' is successfully deleted from the store.", albumName, albumId);
        }

        private async Task ThrowIfResponseStatusNotOk(HttpResponseMessage response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                LogHeaders(response, LogLevel.Information);
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogError("Content: Length={0}\n{1}", content.Length, content);
                throw new Exception(string.Format("Received the above response with status code : {0}", response.StatusCode));
            }
        }

        private IEnumerable<string> GetCookieNames(string uri = null)
        {
            if (uri == null)
            {
                uri = _deploymentResult.ApplicationBaseUri;
            }

            return _httpClientHandler.CookieContainer.GetCookies(new Uri(uri))
                .OfType<Cookie>()
                .Select(c => c.Name);
        }
    }
}