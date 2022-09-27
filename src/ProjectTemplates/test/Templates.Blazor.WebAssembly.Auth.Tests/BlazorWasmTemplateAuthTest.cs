// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Newtonsoft.Json.Linq;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Templates.Blazor.Test;

public class BlazorWasmTemplateAuthTest : BlazorTemplateTest
{
    public BlazorWasmTemplateAuthTest(ProjectFactoryFixture projectFactory)
        : base(projectFactory) { }

    public override string ProjectType { get; } = "blazorwasm";

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/34554", Queues = "Windows.10.Arm64v8.Open")]
    // LocalDB doesn't work on non Windows platforms
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public Task BlazorWasmHostedTemplate_IndividualAuth_Works_WithLocalDB()
        => BlazorWasmHostedTemplate_IndividualAuth_Works(true, false, false);

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/34554", Queues = "Windows.10.Arm64v8.Open")]
    // LocalDB doesn't work on non Windows platforms
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public Task BlazorWasmHostedTemplate_IndividualAuth_NoHttps_Works_WithLocalDB()
        => BlazorWasmHostedTemplate_IndividualAuth_Works(true, false, true);

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/34554", Queues = "Windows.10.Arm64v8.Open")]
    public Task BlazorWasmHostedTemplate_IndividualAuth_Works_WithOutLocalDB()
        => BlazorWasmHostedTemplate_IndividualAuth_Works(false, false, false);

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/34554", Queues = "Windows.10.Arm64v8.Open")]
    public Task BlazorWasmHostedTemplate_IndividualAuth_NoHttps_Works_WithOutLocalDB()
        => BlazorWasmHostedTemplate_IndividualAuth_Works(false, false, false);

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/34554", Queues = "Windows.10.Arm64v8.Open")]
    // LocalDB doesn't work on non Windows platforms
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public Task BlazorWasmHostedTemplate_IndividualAuth_Works_WithLocalDB_ProgramMain()
        => BlazorWasmHostedTemplate_IndividualAuth_Works(true, true, false);

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/34554", Queues = "Windows.10.Arm64v8.Open")]
    // LocalDB doesn't work on non Windows platforms
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public Task BlazorWasmHostedTemplate_IndividualAuth_NoHttps_Works_WithLocalDB_ProgramMain()
        => BlazorWasmHostedTemplate_IndividualAuth_Works(true, true, true);

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/34554", Queues = "Windows.10.Arm64v8.Open")]
    public Task BlazorWasmHostedTemplate_IndividualAuth_Works_WithOutLocalDB_ProgramMain()
        => BlazorWasmHostedTemplate_IndividualAuth_Works(false, true, false);

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/34554", Queues = "Windows.10.Arm64v8.Open")]
    public Task BlazorWasmHostedTemplate_IndividualAuth_NoHttps_Works_WithOutLocalDB_ProgramMain()
        => BlazorWasmHostedTemplate_IndividualAuth_Works(false, true, true);

    private async Task<Project> CreateBuildPublishIndividualAuthProject(bool useLocalDb, bool useProgramMain = false, bool noHttps = false)
    {
        // Additional arguments are needed. See: https://github.com/dotnet/aspnetcore/issues/24278
        Environment.SetEnvironmentVariable("EnableDefaultScopedCssItems", "true");

        var args = new[] { ArgConstants.Hosted, useLocalDb ? "-uld" : "", useProgramMain ? ArgConstants.UseProgramMain : "", noHttps ? ArgConstants.NoHttps : "" };
        var project = await CreateBuildPublishAsync("Individual", args: args);

        var serverProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");

        var serverProjectFileContents = ReadFile(serverProject.TemplateOutputDir, $"{serverProject.ProjectName}.csproj");
        if (!useLocalDb)
        {
            Assert.Contains(".db", serverProjectFileContents);
        }

        var appSettings = ReadFile(serverProject.TemplateOutputDir, "appsettings.json");
        var element = JsonSerializer.Deserialize<JsonElement>(appSettings);
        var clientsProperty = element.GetProperty("IdentityServer").EnumerateObject().Single().Value.EnumerateObject().Single();
        var replacedSection = element.GetRawText().Replace(clientsProperty.Name, serverProject.ProjectName.Replace(".Server", ".Client"));
        var appSettingsPath = Path.Combine(serverProject.TemplateOutputDir, "appsettings.json");
        File.WriteAllText(appSettingsPath, replacedSection);

        await serverProject.RunDotNetPublishAsync();

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.

        await serverProject.RunDotNetBuildAsync();

        await serverProject.RunDotNetEfCreateMigrationAsync("blazorwasm");
        serverProject.AssertEmptyMigration("blazorwasm");

        return project;
    }

    private async Task BlazorWasmHostedTemplate_IndividualAuth_Works(bool useLocalDb, bool useProgramMain, bool noHttps)
    {
        var project = await CreateBuildPublishIndividualAuthProject(useLocalDb: useLocalDb, useProgramMain: useProgramMain, noHttps: noHttps);

        var serverProject = GetSubProject(project, "Server", $"{project.ProjectName}.Server");
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
            new TemplateInstance("blazorwasmhostedaadb2c", "IndividualB2C",
                ArgConstants.Hosted,
                ArgConstants.AadB2cInstance, "example.b2clogin.com",
                "-ssp", "b2c_1_siupin",
                ArgConstants.ClientId, "clientId",
                ArgConstants.Domain, "my-domain",
                ArgConstants.DefaultScope, "full",
                ArgConstants.AppIdUri, "ApiUri",
                ArgConstants.AppIdClientId, "1234123413241324"),
            new TemplateInstance("blazorwasmhostedaadb2c_program_main", "IndividualB2C",
                ArgConstants.Hosted,
                ArgConstants.AadB2cInstance, "example.b2clogin.com",
                "-ssp", "b2c_1_siupin",
                ArgConstants.ClientId, "clientId",
                ArgConstants.Domain, "my-domain",
                ArgConstants.DefaultScope, "full",
                ArgConstants.AppIdUri, "ApiUri",
                ArgConstants.AppIdClientId, "1234123413241324",
                ArgConstants.UseProgramMain),
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
            new TemplateInstance("blazorwasmhostedaad", "SingleOrg",
                ArgConstants.Hosted,
                ArgConstants.Domain, "my-domain",
                ArgConstants.TenantId, "tenantId",
                ArgConstants.ClientId, "clientId",
                ArgConstants.DefaultScope, "full",
                ArgConstants.AppIdUri, "ApiUri",
                ArgConstants.AppIdClientId, "1234123413241324"),
            new TemplateInstance("blazorwasmhostedaadgraph", "SingleOrg",
                ArgConstants.Hosted,
                ArgConstants.CallsGraph,
                ArgConstants.Domain, "my-domain",
                ArgConstants.TenantId, "tenantId",
                ArgConstants.ClientId, "clientId",
                ArgConstants.DefaultScope, "full",
                ArgConstants.AppIdUri, "ApiUri",
                ArgConstants.AppIdClientId, "1234123413241324"),
            new TemplateInstance("blazorwasmhostedaadapi", "SingleOrg",
                ArgConstants.Hosted,
                ArgConstants.CalledApiUrl, "\"https://graph.microsoft.com\"",
                ArgConstants.CalledApiScopes, "user.readwrite",
                ArgConstants.Domain, "my-domain",
                ArgConstants.TenantId, "tenantId",
                ArgConstants.ClientId, "clientId",
                ArgConstants.DefaultScope, "full",
                ArgConstants.AppIdUri, "ApiUri",
                ArgConstants.AppIdClientId, "1234123413241324"),
            new TemplateInstance("blazorwasmstandaloneaad", "SingleOrg",
                ArgConstants.Domain, "my-domain",
                ArgConstants.TenantId, "tenantId",
                ArgConstants.ClientId, "clientId"),
        };

    public static TheoryData<TemplateInstance> TemplateDataSingleOrgProgramMain => new TheoryData<TemplateInstance>
        {
            new TemplateInstance("blazorwasmhostedaad_program_main", "SingleOrg",
                ArgConstants.Hosted,
                ArgConstants.Domain, "my-domain",
                ArgConstants.TenantId, "tenantId",
                ArgConstants.ClientId, "clientId",
                ArgConstants.DefaultScope, "full",
                ArgConstants.AppIdUri, "ApiUri",
                ArgConstants.AppIdClientId, "1234123413241324",
                ArgConstants.UseProgramMain),
            new TemplateInstance("blazorwasmhostedaadgraph_program_main", "SingleOrg",
                ArgConstants.Hosted,
                ArgConstants.CallsGraph,
                ArgConstants.Domain, "my-domain",
                ArgConstants.TenantId, "tenantId",
                ArgConstants.ClientId, "clientId",
                ArgConstants.DefaultScope, "full",
                ArgConstants.AppIdUri, "ApiUri",
                ArgConstants.AppIdClientId, "1234123413241324",
                ArgConstants.UseProgramMain),
            new TemplateInstance("blazorwasmhostedaadapi_program_main", "SingleOrg",
                ArgConstants.Hosted,
                ArgConstants.CalledApiUrl, "\"https://graph.microsoft.com\"",
                ArgConstants.CalledApiScopes, "user.readwrite",
                ArgConstants.Domain, "my-domain",
                ArgConstants.TenantId, "tenantId",
                ArgConstants.ClientId, "clientId",
                ArgConstants.DefaultScope, "full",
                ArgConstants.AppIdUri, "ApiUri",
                ArgConstants.AppIdClientId, "1234123413241324",
                ArgConstants.UseProgramMain),
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
    public Task BlazorWasmHostedTemplate_AzureActiveDirectoryTemplate_IndividualB2C_Works(TemplateInstance instance)
        => CreateBuildPublishAsync(auth: instance.Auth, args: instance.Arguments, targetFramework: "netstandard2.1");

    [ConditionalTheory]
    [MemberData(nameof(TemplateDataIndividualB2C))]
    public Task BlazorWasmHostedTemplate_AzureActiveDirectoryTemplate_IndividualB2C_NoHttps_Works(TemplateInstance instance)
        => CreateBuildPublishAsync(auth: instance.Auth, args: instance.Arguments.Union(new[] { ArgConstants.NoHttps }).ToArray(), targetFramework: "netstandard2.1");

    [ConditionalTheory]
    [MemberData(nameof(TemplateDataSingleOrg))]
    public Task BlazorWasmHostedTemplate_AzureActiveDirectoryTemplate_SingleOrg_Works(TemplateInstance instance)
        => CreateBuildPublishAsync(auth: instance.Auth, args: instance.Arguments, targetFramework: "netstandard2.1");

    [ConditionalTheory]
    [MemberData(nameof(TemplateDataSingleOrg))]
    public Task BlazorWasmHostedTemplate_AzureActiveDirectoryTemplate_SingleOrg_NoHttps_Works(TemplateInstance instance)
        => CreateBuildPublishAsync(auth: instance.Auth, args: instance.Arguments.Union(new[] { ArgConstants.NoHttps }).ToArray(), targetFramework: "netstandard2.1");

    [ConditionalTheory]
    [MemberData(nameof(TemplateDataSingleOrgProgramMain))]
    public Task BlazorWasmHostedTemplate_AzureActiveDirectoryTemplate_SingleOrg_ProgramMain_Works(TemplateInstance instance)
        => CreateBuildPublishAsync(auth: instance.Auth, args: instance.Arguments, targetFramework: "netstandard2.1");

    [ConditionalTheory]
    [MemberData(nameof(TemplateDataSingleOrgProgramMain))]
    public Task BlazorWasmHostedTemplate_AzureActiveDirectoryTemplate_SingleOrg_NoHttps_ProgramMain_Works(TemplateInstance instance)
        => CreateBuildPublishAsync(auth: instance.Auth, args: instance.Arguments.Union(new[] { ArgConstants.NoHttps }).ToArray(), targetFramework: "netstandard2.1");

    private static void UpdatePublishedSettings(Project serverProject)
    {
        // Hijack here the config file to use the development key during publish.
        var appSettings = JObject.Parse(File.ReadAllText(Path.Combine(serverProject.TemplateOutputDir, "appsettings.json")));
        var appSettingsDevelopment = JObject.Parse(File.ReadAllText(Path.Combine(serverProject.TemplateOutputDir, "appsettings.Development.json")));
        ((JObject)appSettings["IdentityServer"]).Merge(appSettingsDevelopment["IdentityServer"]);
        ((JObject)appSettings["IdentityServer"]).Merge(new
        {
            IdentityServer = new
            {
                Key = new
                {
                    FilePath = "./tempkey.json"
                }
            }
        });
        var testAppSettings = appSettings.ToString();
        File.WriteAllText(Path.Combine(serverProject.TemplatePublishDir, "appsettings.json"), testAppSettings);
    }
}
