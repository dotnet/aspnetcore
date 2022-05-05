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

namespace Templates.Test
{
    public class BlazorServerTemplateTest : BlazorTemplateTest
    {
        public BlazorServerTemplateTest(ProjectFactoryFixture projectFactory)
            : base(projectFactory)
        {
        }

        public override string ProjectType { get; } = "blazorserver";

        [Fact]
        public Task BlazorServerTemplateWorks_NoAuth() => CreateBuildPublishAsync("blazorservernoauth");

        [Fact]
        public Task BlazorServerTemplateWorks_ProgamMainNoAuth() => CreateBuildPublishAsync("blazorservernoauth", args: new [] { ArgConstants.UseProgramMain });

        [Theory]
        [InlineData(true, null)]
        [InlineData(true, new string[] { ArgConstants.UseProgramMain })]
        [InlineData(false, null)]
        [InlineData(false, new string[] { ArgConstants.UseProgramMain })]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/30825", Queues = "All.OSX")]
        public Task BlazorServerTemplateWorks_IndividualAuth(bool useLocalDB, string[] args) => CreateBuildPublishAsync("blazorserverindividual" + (useLocalDB ? "uld" : "", args: args));

        [Theory]
        [InlineData("IndividualB2C", null)]
        [InlineData("IndividualB2C", new[] { ArgConstants.UseProgramMain })]
        [InlineData("IndividualB2C", new[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        [InlineData("IndividualB2C", new[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        [InlineData("SingleOrg", null)]
        [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain })]
        [InlineData("SingleOrg", new[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
        [InlineData("SingleOrg", new[] { ArgConstants.CallsGraph })]
        [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.CallsGraph })]
        public Task BlazorServerTemplate_IdentityWeb_BuildAndPublish(string auth, string[] args)
            => CreateBuildPublishAsync("blazorserveridweb" + Guid.NewGuid().ToString().Substring(0, 10).ToLowerInvariant(), auth, args);

    }
}
