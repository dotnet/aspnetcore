// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.HttpSys.NonHelixTests;

public class DelegateOutOfProcTests : LoggedTest
{
    public DelegateOutOfProcTests(ITestOutputHelper output) : base(output) { }

    [ConditionalFact]
    [DelegateSupportedCondition(true)]
    public async Task CanDelegateOutOfProcess()
    {
        using var _ = StartLog(out var loggerFactory);

        var logger = loggerFactory.CreateLogger("CanDelegateOutOfProcess");

        // https://github.com/dotnet/aspnetcore/issues/8247
#pragma warning disable 0618
        var applicationPath = Path.Combine(TestPathUtilities.GetSolutionRootDirectory("HttpSysServer"), "test", "testassets",
            "DelegationSite");
#pragma warning restore 0618

        var deploymentParameters = new DeploymentParameters(
            applicationPath,
            ServerType.HttpSys,
            RuntimeFlavor.CoreClr,
            RuntimeArchitectures.Current)
        {
            EnvironmentName = "Testing",
            TargetFramework = Tfm.Default,
            ApplicationType = ApplicationType.Portable,
            PublishApplicationBeforeDeployment = true,
            StatusMessagesEnabled = true
        };

        var queueName = Guid.NewGuid().ToString();
        deploymentParameters.EnvironmentVariables["queue"] = queueName;

        using var deployer = new SelfHostDeployer(deploymentParameters, loggerFactory);
        var deploymentResult = await deployer.DeployAsync().DefaultTimeout();

        // Make sure the deployment really worked
        var responseString = await deploymentResult.HttpClient.GetStringAsync("").DefaultTimeout();
        Assert.Equal("Hello from delegatee", responseString);

        DelegationRule destination = default;
        using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, httpContext =>
        {
            var delegateFeature = httpContext.Features.Get<IHttpSysRequestDelegationFeature>();
            delegateFeature.DelegateRequest(destination);
            return Task.CompletedTask;
        });

        var delegationProperty = delegator.Features.Get<IServerDelegationFeature>();
        using (destination = delegationProperty.CreateDelegationRule(queueName, deploymentResult.ApplicationBaseUri))
        {
            // Send a request to the delegator that gets transfered to the delegatee in the other process.
            using var client = new HttpClient();
            responseString = await client.GetStringAsync(delegatorAddress).DefaultTimeout();
            Assert.Equal("Hello from delegatee", responseString);
        }
    }
}
