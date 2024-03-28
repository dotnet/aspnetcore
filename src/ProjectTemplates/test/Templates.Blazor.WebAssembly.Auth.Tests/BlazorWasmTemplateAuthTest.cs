// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Templates.Test.Helpers;

namespace Templates.Blazor.Test;

public class BlazorWasmTemplateAuthTest : BlazorTemplateTest
{
    public BlazorWasmTemplateAuthTest(ProjectFactoryFixture projectFactory)
        : base(projectFactory) { }

    public override string ProjectType { get; } = "blazorwasm";

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
