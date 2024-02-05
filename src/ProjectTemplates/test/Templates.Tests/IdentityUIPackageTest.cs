// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test;

public class IdentityUIPackageTest : LoggedTest
{
    public IdentityUIPackageTest(ProjectFactoryFixture projectFactory)
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

    public static string[] Bootstrap5ContentFiles { get; } = new []
    {
            "Identity/favicon.ico",
            "Identity/css/site.css",
            "Identity/js/site.js",
            "Identity/lib/bootstrap/dist/css/bootstrap.css",
            "Identity/lib/bootstrap/dist/css/bootstrap.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap.min.css",
            "Identity/lib/bootstrap/dist/css/bootstrap.min.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap.rtl.css",
            "Identity/lib/bootstrap/dist/css/bootstrap.rtl.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap.rtl.min.css",
            "Identity/lib/bootstrap/dist/css/bootstrap.rtl.min.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap-grid.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-grid.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap-grid.min.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-grid.min.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap-grid.rtl.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-grid.rtl.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap-grid.rtl.min.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-grid.rtl.min.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap-reboot.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-reboot.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap-reboot.min.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-reboot.min.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap-reboot.rtl.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-reboot.rtl.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap-reboot.rtl.min.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-reboot.rtl.min.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap-utilities.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-utilities.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap-utilities.min.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-utilities.min.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap-utilities.rtl.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-utilities.rtl.css.map",
            "Identity/lib/bootstrap/dist/css/bootstrap-utilities.rtl.min.css",
            "Identity/lib/bootstrap/dist/css/bootstrap-utilities.rtl.min.css.map",
            "Identity/lib/bootstrap/dist/js/bootstrap.js",
            "Identity/lib/bootstrap/dist/js/bootstrap.js.map",
            "Identity/lib/bootstrap/dist/js/bootstrap.min.js",
            "Identity/lib/bootstrap/dist/js/bootstrap.min.js.map",
            "Identity/lib/bootstrap/dist/js/bootstrap.bundle.js",
            "Identity/lib/bootstrap/dist/js/bootstrap.bundle.js.map",
            "Identity/lib/bootstrap/dist/js/bootstrap.bundle.min.js",
            "Identity/lib/bootstrap/dist/js/bootstrap.bundle.min.js.map",
            "Identity/lib/bootstrap/dist/js/bootstrap.esm.js",
            "Identity/lib/bootstrap/dist/js/bootstrap.esm.js.map",
            "Identity/lib/bootstrap/dist/js/bootstrap.esm.min.js",
            "Identity/lib/bootstrap/dist/js/bootstrap.esm.min.js.map",
            "Identity/lib/jquery/LICENSE.txt",
            "Identity/lib/jquery/dist/jquery.js",
            "Identity/lib/jquery/dist/jquery.min.js",
            "Identity/lib/jquery/dist/jquery.min.map",
            "Identity/lib/jquery-validation/LICENSE.md",
            "Identity/lib/jquery-validation/dist/additional-methods.js",
            "Identity/lib/jquery-validation/dist/additional-methods.min.js",
            "Identity/lib/jquery-validation/dist/jquery.validate.js",
            "Identity/lib/jquery-validation/dist/jquery.validate.min.js",
            "Identity/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js",
            "Identity/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js",
            "Identity/lib/jquery-validation-unobtrusive/LICENSE.txt",
    };

    [ConditionalFact]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public async Task IdentityUIPackage_WorksWithDifferentOptions()
    {
        var packageOptions = new Dictionary<string, string>();
        var project = await ProjectFactory.CreateProject(Output);
        var useLocalDB = false;

        await project.RunDotNetNewAsync("razor", auth: "Individual", useLocalDB: useLocalDB, environmentVariables: packageOptions);

        var projectFileContents = ReadFile(project.TemplateOutputDir, $"{project.ProjectName}.csproj");
        Assert.Contains(".db", projectFileContents);

        await project.RunDotNetPublishAsync(packageOptions: packageOptions);

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.

        await project.RunDotNetBuildAsync(packageOptions: packageOptions);

        await project.RunDotNetEfCreateMigrationAsync("razorpages");
        project.AssertEmptyMigration("razorpages");

        var versionValidator = "Bootstrap v5.1.0";
        using (var aspNetProcess = project.StartBuiltProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            var response = await aspNetProcess.SendRequest("/Identity/lib/bootstrap/dist/css/bootstrap.css");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(versionValidator, await response.Content.ReadAsStringAsync());
            await ValidatePublishedFiles(aspNetProcess, Bootstrap5ContentFiles);
        }

        using (var aspNetProcess = project.StartPublishedProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            var response = await aspNetProcess.SendRequest("/Identity/lib/bootstrap/dist/css/bootstrap.css");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(versionValidator, await response.Content.ReadAsStringAsync());
            await ValidatePublishedFiles(aspNetProcess, Bootstrap5ContentFiles);
        }
    }

    private async Task ValidatePublishedFiles(AspNetProcess aspNetProcess, string[] expectedContentFiles)
    {
        foreach (var file in expectedContentFiles)
        {
            var response = await aspNetProcess.SendRequest(file);
            Assert.True(response?.StatusCode == HttpStatusCode.OK, $"Couldn't find file '{file}'");
        }
    }

    private string ReadFile(string basePath, string path)
    {
        var fullPath = Path.Combine(basePath, path);
        var doesExist = File.Exists(fullPath);

        Assert.True(doesExist, $"Expected file to exist, but it doesn't: {path}");
        return File.ReadAllText(Path.Combine(basePath, path));
    }
}
