using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
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

                Console.WriteLine("Verifying home page");
                await validator.VerifyHomePage(response);

                Console.WriteLine("Verifying static files are served from static file middleware");
                await validator.VerifyStaticContentServed();

                Console.WriteLine("Verifying access to a protected resource should automatically redirect to login page.");
                await validator.AccessStoreWithoutPermissions();

                Console.WriteLine("Verifying mismatched passwords trigger validaton errors during user registration");
                await validator.RegisterUserWithNonMatchingPasswords();

                Console.WriteLine("Verifying valid user registration");
                var generatedEmail = await validator.RegisterValidUser();

                Console.WriteLine("Verifying duplicate user email registration");
                await validator.RegisterExistingUser(generatedEmail);

                Console.WriteLine("Verifying incorrect password login");
                await validator.SignInWithInvalidPassword(generatedEmail, "InvalidPassword~1");

                Console.WriteLine("Verifying valid user log in");
                await validator.SignInWithUser(generatedEmail, "Password~1");

                Console.WriteLine("Verifying change password");
                await validator.ChangePassword(generatedEmail);

                Console.WriteLine("Verifying old password is not valid anymore");
                await validator.SignOutUser(generatedEmail);
                await validator.SignInWithInvalidPassword(generatedEmail, "Password~1");
                await validator.SignInWithUser(generatedEmail, "Password~2");

                Console.WriteLine("Verifying authenticated user trying to access unauthorized resource");
                await validator.AccessStoreWithoutPermissions(generatedEmail);

                Console.WriteLine("Verifying user log out");
                await validator.SignOutUser(generatedEmail);

                Console.WriteLine("Verifying admin user login");
                await validator.SignInWithUser("Administrator@test.com", "YouShouldChangeThisPassword1!");

                Console.WriteLine("Verifying admin user's access to store manager page");
                await validator.AccessStoreWithPermissions();

                Console.WriteLine("Verifying creating a new album");
                var albumName = await validator.CreateAlbum();
                var albumId = await validator.FetchAlbumIdFromName(albumName);

                Console.WriteLine("Verifying retrieved album details");
                await validator.VerifyAlbumDetails(albumId, albumName);

                Console.WriteLine("Verifying status code pages for non-existing items");
                await validator.VerifyStatusCodePages();

                Console.WriteLine("Verifying non-admin view of an album");
                await validator.GetAlbumDetailsFromStore(albumId, albumName);

                Console.WriteLine("Verifying adding album to a cart");
                await validator.AddAlbumToCart(albumId, albumName);

                Console.WriteLine("Verifying cart checkout");
                await validator.CheckOutCartItems();

                Console.WriteLine("Verifying deletion of album from a cart");
                await validator.DeleteAlbum(albumId, albumName);

                Console.WriteLine("Verifying administrator log out");
                await validator.SignOutUser("Administrator");

                Console.WriteLine("Verifying Google login scenarios");
                await validator.LoginWithGoogle();

                Console.WriteLine("Verifying Facebook login scenarios");
                await validator.LoginWithFacebook();

                Console.WriteLine("Verifying Twitter login scenarios");
                await validator.LoginWithTwitter();

                Console.WriteLine("Verifying Microsoft login scenarios");
                await validator.LoginWithMicrosoftAccount();

                logger.LogInformation("Variation completed successfully.");
            }
        }
    }
}
