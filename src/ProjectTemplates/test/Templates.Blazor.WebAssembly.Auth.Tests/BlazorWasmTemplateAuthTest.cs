// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Templates.Test.Helpers;
using Xunit.Abstractions;

namespace Templates.Blazor.Test;

#pragma warning disable xUnit1041 // Fixture arguments to test classes must have fixture sources

public class BlazorWasmTemplateAuthTest : LoggedTest
{
    public BlazorWasmTemplateAuthTest(ProjectFactoryFixture projectFactory)
    {
        ProjectFactory = projectFactory;
    }

    public ProjectFactoryFixture ProjectFactory { get; set; }

    private ITestOutputHelper _output;
    public ITestOutputHelper Output
    {
        get
        {
            if (_output == null)
            {
                _output = new TestOutputLogger(Logger);
            }
            return _output;
        }
    }

    public string ProjectType { get; } = "blazorwasm";

    protected async Task<Project> CreateBuildPublishAsync(string auth = null, string[] args = null, string targetFramework = null)
    {
        // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
        Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

        var project = await ProjectFactory.CreateProject(Output);
        if (targetFramework != null)
        {
            project.TargetFramework = targetFramework;
        }

        await project.RunDotNetNewAsync(ProjectType, auth: auth, args: args, errorOnRestoreError: false);

        await project.RunDotNetPublishAsync(noRestore: false);

        return project;
    }

    [Fact]
    public async Task BlazorWasmStandaloneTemplate_IndividualAuth_CreateBuildPublish()
    {
        var project = await CreateBuildPublishAsync("Individual", args: new[] {
                "--authority",
                "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
                ArgConstants.ClientId,
                "sample-client-id"
            });
    }

    [Fact]
    public async Task BlazorWasmStandaloneTemplate_NoHttps_IndividualAuth_CreateBuildPublish()
    {
        var project = await CreateBuildPublishAsync("Individual", args: new[] {
                "--authority",
                "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
                ArgConstants.ClientId,
                "sample-client-id",
                ArgConstants.NoHttps
            });
    }

    public static TheoryData<TemplateInstance> TemplateDataIndividualB2C => new TheoryData<TemplateInstance>
        {
            new TemplateInstance("blazorwasmstandaloneaadb2c", "IndividualB2C",
                ArgConstants.AadB2cInstance, "example.b2clogin.com",
                "-ssp", "b2c_1_siupin",
                ArgConstants.ClientId, "clientId",
                ArgConstants.Domain, "my-domain"),
            new TemplateInstance("blazorwasmstandaloneaadb2c_program_main", "IndividualB2C",
                ArgConstants.AadB2cInstance, "example.b2clogin.com",
                "-ssp", "b2c_1_siupin",
                ArgConstants.ClientId, "clientId",
                ArgConstants.Domain, "my-domain",
                ArgConstants.UseProgramMain),
        };

    public static TheoryData<TemplateInstance> TemplateDataSingleOrg => new TheoryData<TemplateInstance>
        {
            new TemplateInstance("blazorwasmstandaloneaad", "SingleOrg",
                ArgConstants.Domain, "my-domain",
                ArgConstants.TenantId, "tenantId",
                ArgConstants.ClientId, "clientId"),
        };

    public static TheoryData<TemplateInstance> TemplateDataSingleOrgProgramMain => new TheoryData<TemplateInstance>
        {
            new TemplateInstance("blazorwasmstandaloneaad_program_main", "SingleOrg",
                ArgConstants.Domain, "my-domain",
                ArgConstants.TenantId, "tenantId",
                ArgConstants.ClientId, "clientId",
                ArgConstants.UseProgramMain),
        };

    public class TemplateInstance
    {
        public TemplateInstance(string name, string auth, params string[] arguments)
        {
            Name = name;
            Auth = auth;
            Arguments = arguments;
        }

        public string Name { get; }
        public string Auth { get; }
        public string[] Arguments { get; }
    }

    [ConditionalTheory]
    [MemberData(nameof(TemplateDataIndividualB2C))]
    public Task BlazorWasmStandaloneTemplate_AzureActiveDirectoryTemplate_IndividualB2C_Works(TemplateInstance instance)
        => CreateBuildPublishAsync(auth: instance.Auth, args: instance.Arguments, targetFramework: "netstandard2.1");

    [ConditionalTheory]
    [MemberData(nameof(TemplateDataIndividualB2C))]
    public Task BlazorWasmStandaloneTemplate_AzureActiveDirectoryTemplate_IndividualB2C_NoHttps_Works(TemplateInstance instance)
        => CreateBuildPublishAsync(auth: instance.Auth, args: instance.Arguments.Union(new[] { ArgConstants.NoHttps }).ToArray(), targetFramework: "netstandard2.1");

    [ConditionalTheory]
    [MemberData(nameof(TemplateDataSingleOrg))]
    public Task BlazorWasmStandaloneTemplate_AzureActiveDirectoryTemplate_SingleOrg_Works(TemplateInstance instance)
        => CreateBuildPublishAsync(auth: instance.Auth, args: instance.Arguments, targetFramework: "netstandard2.1");

    [ConditionalTheory]
    [MemberData(nameof(TemplateDataSingleOrg))]
    public Task BlazorWasmStandaloneTemplate_AzureActiveDirectoryTemplate_SingleOrg_NoHttps_Works(TemplateInstance instance)
        => CreateBuildPublishAsync(auth: instance.Auth, args: instance.Arguments.Union(new[] { ArgConstants.NoHttps }).ToArray(), targetFramework: "netstandard2.1");

    [ConditionalTheory]
    [MemberData(nameof(TemplateDataSingleOrgProgramMain))]
    public Task BlazorWasmStandaloneTemplate_AzureActiveDirectoryTemplate_SingleOrg_ProgramMain_Works(TemplateInstance instance)
        => CreateBuildPublishAsync(auth: instance.Auth, args: instance.Arguments, targetFramework: "netstandard2.1");

    [ConditionalTheory]
    [MemberData(nameof(TemplateDataSingleOrgProgramMain))]
    public Task BlazorWasmStandaloneTemplate_AzureActiveDirectoryTemplate_SingleOrg_NoHttps_ProgramMain_Works(TemplateInstance instance)
        => CreateBuildPublishAsync(auth: instance.Auth, args: instance.Arguments.Union(new[] { ArgConstants.NoHttps }).ToArray(), targetFramework: "netstandard2.1");
}

internal sealed class TestOutputLogger : ITestOutputHelper
{
    private readonly ILogger _logger;

    public TestOutputLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void WriteLine(string message)
    {
        _logger.LogInformation(message);
    }

    public void WriteLine(string format, params object[] args)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args));
        }
    }
}
