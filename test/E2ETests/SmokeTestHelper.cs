using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace E2ETests
{
    public static class SmokeTestHelper
    {
        public static async Task RunTestsAsync(DeploymentResult deploymentResult, ILogger logger)
        {
            var httpClientHandler = new HttpClientHandler();
            var httpClient = new HttpClient(httpClientHandler)
            {
                BaseAddress = new Uri(deploymentResult.ApplicationBaseUri),
                Timeout = TimeSpan.FromSeconds(5),
            };

            using (httpClient)
            {
                // Request to base address and check if various parts of the body are rendered
                // & measure the cold startup time.
                var response = await RetryHelper.RetryRequest(async () =>
                {
                    return await httpClient.GetAsync(string.Empty);
                }, logger, cancellationToken: deploymentResult.HostShutdownToken);

                Assert.False(response == null, "Response object is null because the client could not " +
                    "connect to the server after multiple retries");

                var validator = new Validator(httpClient, httpClientHandler, logger, deploymentResult);

                await validator.VerifyHomePage(response);

                // Verify the static file middleware can serve static content.
                await validator.VerifyStaticContentServed();

                // Making a request to a protected resource should automatically redirect to login page.
                await validator.AccessStoreWithoutPermissions();

                // Register a user - Negative scenario where the Password & ConfirmPassword do not match.
                await validator.RegisterUserWithNonMatchingPasswords();

                // Register a valid user.
                var generatedEmail = await validator.RegisterValidUser();

                await validator.SignInWithUser(generatedEmail, "Password~1");

                // Register a user - Negative scenario : Trying to register a user name that's already registered.
                await validator.RegisterExistingUser(generatedEmail);

                // Logout from this user session - This should take back to the home page
                await validator.SignOutUser(generatedEmail);

                // Sign in scenarios: Invalid password - Expected an invalid user name password error.
                await validator.SignInWithInvalidPassword(generatedEmail, "InvalidPassword~1");

                // Sign in scenarios: Valid user name & password.
                await validator.SignInWithUser(generatedEmail, "Password~1");

                // Change password scenario
                await validator.ChangePassword(generatedEmail);

                // SignIn with old password and verify old password is not allowed and new password is allowed
                await validator.SignOutUser(generatedEmail);
                await validator.SignInWithInvalidPassword(generatedEmail, "Password~1");
                await validator.SignInWithUser(generatedEmail, "Password~2");

                // Making a request to a protected resource that this user does not have access to - should
                // automatically redirect to the configured access denied page
                await validator.AccessStoreWithoutPermissions(generatedEmail);

                // Logout from this user session - This should take back to the home page
                await validator.SignOutUser(generatedEmail);

                // Login as an admin user
                await validator.SignInWithUser("Administrator@test.com", "YouShouldChangeThisPassword1!");

                // Now navigating to the store manager should work fine as this user has
                // the necessary permission to administer the store.
                await validator.AccessStoreWithPermissions();

                // Create an album
                var albumName = await validator.CreateAlbum();
                var albumId = await validator.FetchAlbumIdFromName(albumName);

                // Get details of the album
                await validator.VerifyAlbumDetails(albumId, albumName);

                // Verify status code pages acts on non-existing items.
                await validator.VerifyStatusCodePages();

                // Get the non-admin view of the album.
                await validator.GetAlbumDetailsFromStore(albumId, albumName);

                // Add an album to cart and checkout the same
                await validator.AddAlbumToCart(albumId, albumName);
                await validator.CheckOutCartItems();

                // Delete the album from store
                await validator.DeleteAlbum(albumId, albumName);

                // Logout from this user session - This should take back to the home page
                await validator.SignOutUser("Administrator");

                // Google login
                await validator.LoginWithGoogle();

                // Facebook login
                await validator.LoginWithFacebook();

                // Twitter login
                await validator.LoginWithTwitter();

                // MicrosoftAccountLogin
                await validator.LoginWithMicrosoftAccount();

                logger.LogInformation("Variation completed successfully.");
            }
        }
    }
}
