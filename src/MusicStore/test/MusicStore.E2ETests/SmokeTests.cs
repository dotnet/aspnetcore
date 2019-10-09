using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    [Trait("E2Etests", "Smoke")]
    public class SmokeTests : LoggedTest
    {
        public static TestMatrix TestVariants
            => TestMatrix.ForServers(ServerType.IISExpress, ServerType.Kestrel, ServerType.HttpSys)
                .WithTfms(Tfm.NetCoreApp50)
                .WithAllApplicationTypes()
                .WithAllHostingModels();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task Smoke_Tests(TestVariant variant)
        {
            var testName = $"SmokeTestSuite_{variant}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("SmokeTestSuite");
                var musicStoreDbName = DbUtils.GetUniqueName();

                var deploymentParameters = new DeploymentParameters(variant)
                {
                    ApplicationPath = Helpers.GetApplicationPath(),
                    EnvironmentName = "SocialTesting",
                    PreservePublishedApplicationForDebugging = Helpers.PreservePublishedApplicationForDebugging,
                    UserAdditionalCleanup = parameters =>
                    {
                        DbUtils.DropDatabase(musicStoreDbName, logger);
                    }
                };

                // Override the connection strings using environment based configuration
                deploymentParameters.EnvironmentVariables
                    .Add(new KeyValuePair<string, string>(
                        MusicStoreConfig.ConnectionStringKey,
                        DbUtils.CreateConnectionString(musicStoreDbName)));

                using (var deployer = IISApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();

                    await RunTestsAsync(deploymentResult, logger);
                }
            }
        }

        internal static async Task RunTestsAsync(DeploymentResult deploymentResult, ILogger logger)
        {
            var httpClientHandler = new HttpClientHandler();
            var httpClient = deploymentResult.CreateHttpClient(httpClientHandler);
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            // Request to base address and check if various parts of the body are rendered
            // & measure the cold startup time.
            var response = await RetryHelper.RetryRequest(async () =>
            {
                return await httpClient.GetAsync(string.Empty);
            }, logger, cancellationToken: deploymentResult.HostShutdownToken);

            Assert.False(response == null, "Response object is null because the client could not " +
                "connect to the server after multiple retries");

            var validator = new Validator(httpClient, httpClientHandler, logger, deploymentResult);

            logger.LogInformation("Verifying home page");
            await validator.VerifyHomePage(response);

            logger.LogInformation("Verifying static files are served from static file middleware");
            await validator.VerifyStaticContentServed();

            logger.LogInformation("Verifying access to a protected resource should automatically redirect to login page.");
            await validator.AccessStoreWithoutPermissions();

            logger.LogInformation("Verifying mismatched passwords trigger validaton errors during user registration");
            await validator.RegisterUserWithNonMatchingPasswords();

            logger.LogInformation("Verifying valid user registration");
            var generatedEmail = await validator.RegisterValidUser();

            logger.LogInformation("Verifying duplicate user email registration");
            await validator.RegisterExistingUser(generatedEmail);

            logger.LogInformation("Verifying incorrect password login");
            await validator.SignInWithInvalidPassword(generatedEmail, "InvalidPassword~1");

            logger.LogInformation("Verifying valid user log in");
            await validator.SignInWithUser(generatedEmail, "Password~1");

            logger.LogInformation("Verifying change password");
            await validator.ChangePassword(generatedEmail);

            logger.LogInformation("Verifying old password is not valid anymore");
            await validator.SignOutUser(generatedEmail);
            await validator.SignInWithInvalidPassword(generatedEmail, "Password~1");
            await validator.SignInWithUser(generatedEmail, "Password~2");

            logger.LogInformation("Verifying authenticated user trying to access unauthorized resource");
            await validator.AccessStoreWithoutPermissions(generatedEmail);

            logger.LogInformation("Verifying user log out");
            await validator.SignOutUser(generatedEmail);

            logger.LogInformation("Verifying admin user login");
            await validator.SignInWithUser("Administrator@test.com", "YouShouldChangeThisPassword1!");

            logger.LogInformation("Verifying admin user's access to store manager page");
            await validator.AccessStoreWithPermissions();

            logger.LogInformation("Verifying creating a new album");
            var albumName = await validator.CreateAlbum();
            var albumId = await validator.FetchAlbumIdFromName(albumName);

            logger.LogInformation("Verifying retrieved album details");
            await validator.VerifyAlbumDetails(albumId, albumName);

            logger.LogInformation("Verifying status code pages for non-existing items");
            await validator.VerifyStatusCodePages();

            logger.LogInformation("Verifying non-admin view of an album");
            await validator.GetAlbumDetailsFromStore(albumId, albumName);

            logger.LogInformation("Verifying adding album to a cart");
            await validator.AddAlbumToCart(albumId, albumName);

            logger.LogInformation("Verifying cart checkout");
            await validator.CheckOutCartItems();

            logger.LogInformation("Verifying deletion of album from a cart");
            await validator.DeleteAlbum(albumId, albumName);

            logger.LogInformation("Verifying administrator log out");
            await validator.SignOutUser("Administrator");

            logger.LogInformation("Variation completed successfully.");
        }
    }
}
