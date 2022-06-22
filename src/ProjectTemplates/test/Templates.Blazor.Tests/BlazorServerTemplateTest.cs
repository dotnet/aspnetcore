// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Templates.Blazor.Test;

public class BlazorServerTemplateTest : BlazorTemplateTest
{
    public BlazorServerTemplateTest(ProjectFactoryFixture projectFactory)
        : base(projectFactory)
    {
    }

    public override string ProjectType { get; } = "blazorserver";

    [Fact]
    public Task BlazorServerTemplateWorks_NoAuth() => CreateBuildPublishAsync();

    [Fact]
    public Task BlazorServerTemplate_NoHttps_Works_NoAuth() => CreateBuildPublishAsync(args: new[] { ArgConstants.NoHttps });

    [Fact]
    public Task BlazorServerTemplateWorks_ProgamMainNoAuth() => CreateBuildPublishAsync(args: new[] { ArgConstants.UseProgramMain });

    [Fact]
    public Task BlazorServerTemplate_NoHttps_Works_ProgamMainNoAuth() => CreateBuildPublishAsync(args: new[] { ArgConstants.UseProgramMain, ArgConstants.NoHttps});

    [ConditionalTheory]
    [InlineData("Individual", null)]
    [InlineData("Individual", new [] { ArgConstants.UseProgramMain })]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/30825", Queues = "All.OSX")]
    public Task BlazorServerTemplateWorks_IndividualAuth(string auth, string[] args) => CreateBuildPublishAsync(auth, args: args);

    [ConditionalTheory]
    [InlineData("Individual", new[] { ArgConstants.NoHttps })]
    [InlineData("Individual", new [] { ArgConstants.UseProgramMain, ArgConstants.NoHttps })]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/30825", Queues = "All.OSX")]
    public Task BlazorServerTemplateWorks_IndividualAuth_NoHttps(string auth, string[] args) => CreateBuildPublishAsync(auth, args: args);

    [ConditionalTheory]
    [InlineData("Individual", new [] { ArgConstants.UseLocalDb })]
    [InlineData("Individual", new [] { ArgConstants.UseProgramMain, ArgConstants.UseLocalDb })]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "No LocalDb on non-Windows")]
    public Task BlazorServerTemplateWorks_IndividualAuth_LocalDb(string auth, string[] args) => CreateBuildPublishAsync(auth, args: args);

    [ConditionalTheory]
    [InlineData("Individual", new[] { ArgConstants.UseLocalDb, ArgConstants.NoHttps })]
    [InlineData("Individual", new[] { ArgConstants.UseProgramMain, ArgConstants.UseLocalDb, ArgConstants.NoHttps })]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "No LocalDb on non-Windows")]
    public Task BlazorServerTemplateWorks_IndividualAuth_NoHttps_LocalDb(string auth, string[] args) => CreateBuildPublishAsync(auth, args: args);

    [Theory]
    [InlineData("IndividualB2C", null)]
    [InlineData("IndividualB2C", new[] { ArgConstants.UseProgramMain })]
    [InlineData("IndividualB2C", new[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
    [InlineData("IndividualB2C", new[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
    public Task BlazorServerTemplate_IdentityWeb_BuildAndPublish_IndividualB2C(string auth, string[] args) => CreateBuildPublishAsync(auth, args);

    [Theory]
    [InlineData("IndividualB2C", null)]
    [InlineData("IndividualB2C", new[] { ArgConstants.UseProgramMain, ArgConstants.NoHttps })]
    [InlineData("IndividualB2C", new[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite, ArgConstants.NoHttps })]
    [InlineData("IndividualB2C", new[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite, ArgConstants.NoHttps })]
    public Task BlazorServerTemplate_IdentityWeb_BuildAndPublish_IndividualB2C_NoHttps(string auth, string[] args) => CreateBuildPublishAsync(auth, args);

    [Theory]
    [InlineData("SingleOrg", null)]
    [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain })]
    [InlineData("SingleOrg", new[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
    [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
    [InlineData("SingleOrg", new[] { ArgConstants.CallsGraph })]
    [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.CallsGraph })]
    public Task BlazorServerTemplate_IdentityWeb_BuildAndPublish_SingleOrg(string auth, string[] args) => CreateBuildPublishAsync(auth, args);

    [Theory]
    [InlineData("SingleOrg", null)]
    [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite, ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite, ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new[] { ArgConstants.CallsGraph, ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.CallsGraph, ArgConstants.NoHttps })]
    public Task BlazorServerTemplate_IdentityWeb_BuildAndPublish_SingleOrg_NoHttps(string auth, string[] args) => CreateBuildPublishAsync(auth, args);
}
