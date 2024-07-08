// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.InternalTesting;
using Templates.Test.Helpers;

namespace BlazorTemplates.Tests;

public class BlazorWebTemplateTest(ProjectFactoryFixture projectFactory) : BlazorTemplateTest(projectFactory)
{
    public override string ProjectType => "blazor";

    [ConditionalTheory]
    [SkipNonHelix]
    [InlineData(BrowserKind.Chromium, "None")]
    [InlineData(BrowserKind.Chromium, "Server")]
    [InlineData(BrowserKind.Chromium, "WebAssembly")]
    [InlineData(BrowserKind.Chromium, "Auto")]
    public async Task BlazorWebTemplate_Works(BrowserKind browserKind, string interactivityOption)
    {
        var project = await CreateBuildPublishAsync(
            args: ["-int", interactivityOption],
            getTargetProject: GetTargetProject);

        // There won't be a counter page when the 'None' interactivity option is used
        var pagesToExclude = interactivityOption is "None"
            ? BlazorTemplatePages.Counter
            : BlazorTemplatePages.None;

        var appName = project.ProjectName;

        // Test the built project
        using (var aspNetProcess = project.StartBuiltProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");
            await TestBasicInteractionInNewPageAsync(browserKind, aspNetProcess.ListeningUri.AbsoluteUri, appName, pagesToExclude);
        }

        // Test the published project
        using (var aspNetProcess = project.StartPublishedProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));

            await aspNetProcess.AssertStatusCode("/", HttpStatusCode.OK, "text/html");

            if (HasClientProject())
            {
                await AssertWebAssemblyCompressionFormatAsync(aspNetProcess, "br");
            }

            await TestBasicInteractionInNewPageAsync(browserKind, aspNetProcess.ListeningUri.AbsoluteUri, appName, pagesToExclude);
        }

        bool HasClientProject()
            => interactivityOption is "WebAssembly" or "Auto";

        Project GetTargetProject(Project rootProject)
        {
            if (HasClientProject())
            {
                // Multiple projects were created, so we need to specifically select the server
                // project to be used
                return GetSubProject(rootProject, rootProject.ProjectName, rootProject.ProjectName);
            }

            // In other cases, just use the root project
            return rootProject;
        }
    }

    private static async Task AssertWebAssemblyCompressionFormatAsync(AspNetProcess aspNetProcess, string expectedEncoding)
    {
        var response = await aspNetProcess.SendRequest(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(aspNetProcess.ListeningUri, "/_framework/blazor.boot.json"));
            // These are the same as chrome
            request.Headers.AcceptEncoding.Clear();
            request.Headers.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("gzip"));
            request.Headers.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("deflate"));
            request.Headers.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("br"));
            return request;
        });
        Assert.Equal(expectedEncoding, response.Content.Headers.ContentEncoding.Single());
    }
}
