// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using OperatingSystem = Microsoft.Azure.Management.AppService.Fluent.OperatingSystem;

namespace Microsoft.AspNetCore.AzureAppServices.FunctionalTests
{
    public class AzureFixture : IDisposable
    {
        public string Timestamp { get; set; }

        public AzureFixture()
        {
            TestLog = AssemblyTestLog.ForAssembly(typeof(AzureFixture).Assembly);

            // TODO: Temporary to see if it's useful and worth exposing
            var globalLoggerFactory =
                (ILoggerFactory) TestLog.GetType().GetField("_globalLoggerFactory", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(TestLog);

            var logger = globalLoggerFactory.CreateLogger<AzureFixture>();

            ServiceClientTracing.IsEnabled = true;
            ServiceClientTracing.AddTracingInterceptor(new LoggingInterceptor(globalLoggerFactory.CreateLogger(nameof(ServiceClientTracing))));

            var clientId = GetRequiredEnvironmentVariable("AZURE_AUTH_CLIENT_ID");
            var clientSecret = GetRequiredEnvironmentVariable("AZURE_AUTH_CLIENT_SECRET");
            var tenant = GetRequiredEnvironmentVariable("AZURE_AUTH_TENANT");

            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientSecret, tenant, AzureEnvironment.AzureGlobalCloud);
            Azure = Microsoft.Azure.Management.Fluent.Azure.Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithDefaultSubscription();

            Timestamp = DateTime.Now.ToString("yyyyMMddhhmmss");
            var testRunName = GetTimestampedName("FunctionalTests");

            logger.LogInformation("Creating resource group {TestRunName}", testRunName);
            ResourceGroup = Azure.ResourceGroups
                .Define(testRunName)
                .WithRegion(Region.USWest2)
                .Create();

            var servicePlanName = GetTimestampedName("TestPlan");
            logger.LogInformation("Creating service plan {servicePlanName}", testRunName);

            Plan = Azure.AppServices.AppServicePlans.Define(servicePlanName)
                .WithRegion(Region.USWest2)
                .WithExistingResourceGroup(ResourceGroup)
                .WithPricingTier(PricingTier.BasicB1)
                .WithOperatingSystem(OperatingSystem.Windows)
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

        public IAppServicePlan Plan { get; set; }

        public IStorageAccount DeploymentStorageAccount { get; set; }

        public AssemblyTestLog TestLog { get; set; }

        public bool DeleteResourceGroup { get; set; } = true;

        public IResourceGroup ResourceGroup { get; set; }

        public IAzure Azure { get; set; }

        public string GetTimestampedName(string name)
        {
            return name + Timestamp;
        }

        public async Task<IWebApp> Deploy(string template, IDictionary<string, string> additionalArguments = null, [CallerMemberName] string baseName = null)
        {
            var siteName = GetTimestampedName(baseName);
            var parameters = new Dictionary<string, string>
            {
                {"siteName", siteName},
                {"hostingPlanName", Plan.Name},
                {"resourceGroupName", ResourceGroup.Name},
            };

            foreach (var pair in additionalArguments ?? Enumerable.Empty<KeyValuePair<string, string>>())
            {
                parameters[pair.Key] = pair.Value;
            }

            var readAllText = File.ReadAllText(template);
            var deployment = await Azure.Deployments.Define(GetTimestampedName("D" + baseName))
                .WithExistingResourceGroup(ResourceGroup)
                .WithTemplate(readAllText)
                .WithParameters(ToParametersObject(parameters))
                .WithMode(DeploymentMode.Incremental)
                .CreateAsync();

            deployment = await deployment.RefreshAsync();

            var outputs = (JObject)deployment.Outputs;

            var siteIdOutput = outputs["siteId"];
            if (siteIdOutput == null)
            {
                throw new InvalidOperationException("Deployment was expected to have 'siteId' output parameter");
            }
            var siteId = siteIdOutput["value"].Value<string>();
            return await Azure.AppServices.WebApps.GetByIdAsync(siteId);
        }

        private JObject ToParametersObject(Dictionary<string, string> parameters)
        {
            return new JObject(
                parameters.Select(parameter =>
                    new JProperty(
                        parameter.Key,
                        new JObject(
                            new JProperty("value", parameter.Value)))));
        }

        public void Dispose()
        {
            TestLog.Dispose();
            if (DeleteResourceGroup && ResourceGroup != null)
            {
                Azure.ResourceGroups.DeleteByName(ResourceGroup.Name);
            }
        }
    }
}
