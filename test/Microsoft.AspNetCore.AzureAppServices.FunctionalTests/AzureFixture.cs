// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Rest;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.AzureAppServices.FunctionalTests
{
    public class AzureFixture : IDisposable
    {
        public AzureFixture(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AzureFixture>();

            ServiceClientTracing.IsEnabled = true;
            ServiceClientTracing.AddTracingInterceptor(new LoggingInterceptor(loggerFactory.CreateLogger(nameof(ServiceClientTracing))));

            var clientId = GetRequiredEnvironmentVariable("AZURE_AUTH_CLIENT_ID");
            var clientSecret = GetRequiredEnvironmentVariable("AZURE_AUTH_CLIENT_SECRET");
            var tenant = GetRequiredEnvironmentVariable("AZURE_AUTH_TENANT");

            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                clientId, clientSecret, tenant, AzureEnvironment.AzureGlobalCloud);
            Azure = Microsoft.Azure.Management.Fluent.Azure.Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithDefaultSubscription();

            var testRunName = GetTimestampedName("FunctionalTests");

            _logger.LogInformation("Creating resource group {TestRunName}", testRunName);
            ResourceGroup = Azure.ResourceGroups
                .Define(testRunName)
                .WithRegion(Region.USWest2)
                .Create();
        }

        public static string GetRequiredEnvironmentVariable(string name)
        {
            var authFile = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(authFile))
            {
                throw new InvalidOperationException($"{name} environment variable has to be set to run these tests.");
            }

            return authFile;
        }

        public IStorageAccount DeploymentStorageAccount { get; set; }

        public AssemblyTestLog TestLog { get; set; }

        public bool DeleteResourceGroup { get; set; } = true;

        public IResourceGroup ResourceGroup { get; set; }

        public IAzure Azure { get; set; }

        public string TimeStamp { get; } = DateTime.Now.ToString("yyyyMMddhhmmss");

        public string GetTimestampedName(string name)
        {
            return name + "t" + TimeStamp;
        }

        private ILogger<AzureFixture> _logger;

        public Task<IWebApp> Deploy(string template, IDictionary<string, string> additionalArguments = null,
            [CallerMemberName] string baseName = null)
        {
            return Retry(() => DeployImp(template, additionalArguments, baseName));
        }

        public async Task<IWebApp> DeployImp(string template, IDictionary<string, string> additionalArguments = null,
            [CallerMemberName] string baseName = null)
        {
            var siteName = GetTimestampedName(baseName);
            var parameters = new Dictionary<string, string>
            {
                {"siteName", siteName},
                {"hostingPlanName", "P" + siteName},
                {"resourceGroupName", ResourceGroup.Name},
            };

            _logger.LogDebug("Deploying {SiteName}", siteName);

            foreach (var pair in additionalArguments ?? Enumerable.Empty<KeyValuePair<string, string>>())
            {
                parameters[pair.Key] = pair.Value;
            }

            var readAllText = File.ReadAllText(template);
            var deploymentName = GetTimestampedName("D" + baseName);

            IDeployment deployment;
            try
            {
                await Azure.Deployments.DeleteByResourceGroupAsync(ResourceGroup.Name, deploymentName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            deployment = await Azure.Deployments.Define(deploymentName)
                .WithExistingResourceGroup(ResourceGroup)
                .WithTemplate(readAllText)
                .WithParameters(ToParametersObject(parameters))
                .WithMode(DeploymentMode.Incremental)
                .CreateAsync();

            deployment = await deployment.RefreshAsync();

            var outputs = (JObject) deployment.Outputs;

            var siteIdOutput = outputs["siteId"];
            if (siteIdOutput == null)
            {
                throw new InvalidOperationException("Deployment was expected to have 'siteId' output parameter");
            }

            _logger.LogDebug("Deployed {SiteName}", siteName);

            var siteId = siteIdOutput["value"].Value<string>();
            return await Azure.AppServices.WebApps.GetByIdAsync(siteId);
        }

        private JObject ToParametersObject(Dictionary<string, string> parameters)
        {
            return new JObject(
                parameters.Select(
                    parameter =>
                        new JProperty(
                            parameter.Key,
                            new JObject(
                                new JProperty("value", parameter.Value)))));
        }

        public void Dispose()
        {
            _logger.LogInformation("Cleaning up Resource Group");
            if (DeleteResourceGroup && ResourceGroup != null)
            {
                Azure.ResourceGroups.DeleteByName(ResourceGroup.Name);
            }
        }

        private async Task<T> Retry<T>(Expression<Func<Task<T>>> function)
        {
            var func = function.Compile();
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return await func();
                }
                catch (Exception) when (i == 4)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Operation {Operation} failed: {Message}", function.ToString(), ex.Message);
                }
            }
            return default(T);
        }
    }
}
