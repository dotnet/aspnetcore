using Xunit;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System;

namespace Microsoft.AspNetCore.Server.IISIntegration.Tools.Tests
{
    public class PublishIISCommandFacts
    {
        private class Folders
        {
            public string TestRoot;
            public string PublishOutput;
            public string ProjectPath;
        }

        [Fact]
        public void PublishIIS_uses_default_values_if_options_not_specified()
        {
            var folders = CreateTestDir("{}");

            new PublishIISCommand(folders.PublishOutput, folders.ProjectPath).Run();

            var processPath = (string)GetPublishedWebConfig(folders.PublishOutput)
                .Descendants("aspNetCore").Attributes("processPath").Single();

            Assert.Equal($@".\projectDir.exe", processPath);

            Directory.Delete(folders.TestRoot, recursive: true);
        }

        [Theory]
        [InlineData("awesomeApp")]
        [InlineData("awesome.App")]
        public void PublishIIS_reads_application_name_from_project_json_if_exists(string projectName)
        {
            var folders = CreateTestDir($@"{{ ""name"": ""{projectName}"" }}");

            new PublishIISCommand(folders.PublishOutput, folders.ProjectPath).Run();

            var processPath = (string)GetPublishedWebConfig(folders.PublishOutput)
                .Descendants("aspNetCore").Attributes("processPath").Single();

            Assert.Equal($@".\{projectName}.exe", processPath);

            Directory.Delete(folders.TestRoot, recursive: true);
        }

        [Theory]
        [InlineData("projectDir")]
        [InlineData("project.Dir")]
        public void PublishIIS_accepts_path_to_project_json_as_project_path(string projectDir)
        {
            var folders = CreateTestDir("{}", projectDir);

            new PublishIISCommand(folders.PublishOutput, Path.Combine(folders.ProjectPath, "project.json")).Run();

            var processPath = (string)GetPublishedWebConfig(folders.PublishOutput)
                .Descendants("aspNetCore").Attributes("processPath").Single();

            Assert.Equal($@".\{projectDir}.exe", processPath);

            Directory.Delete(folders.TestRoot, recursive: true);
        }

        [Fact]
        public void PublishIIS_modifies_existing_web_config()
        {
            var folders = CreateTestDir("{}");

            File.WriteAllText(Path.Combine(folders.PublishOutput, "web.config"),
@"<configuration>
  <system.webServer>
    <handlers>
      <add name=""aspNetCore"" path=""*"" verb=""*"" modules=""AspNetCoreModule"" resourceType=""Unspecified""/>
    </handlers>
    <aspNetCore processPath=""%________%"" stdoutLogEnabled=""false"" startupTimeLimit=""1234""/>
  </system.webServer>
</configuration>");

            new PublishIISCommand(folders.PublishOutput, Path.Combine(folders.ProjectPath, "project.json")).Run();

            var aspNetCoreElement = GetPublishedWebConfig(folders.PublishOutput)
                .Descendants("aspNetCore").Single();

            Assert.Equal(@".\projectDir.exe", (string)aspNetCoreElement.Attribute("processPath"));
            Assert.Equal(@"1234", (string)aspNetCoreElement.Attribute("startupTimeLimit"));

            Directory.Delete(folders.TestRoot, recursive: true);
        }

        private XDocument GetPublishedWebConfig(string publishOut)
        {
            return XDocument.Load(Path.Combine(publishOut, "web.config"));
        }

        private Folders CreateTestDir(string projectJson, string projectDir = "projectDir")
        {
            var testRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(testRoot);

            var projectPath = Path.Combine(testRoot, projectDir);
            Directory.CreateDirectory(projectPath);
            File.WriteAllText(Path.Combine(projectPath, "project.json"), projectJson);

            var publishOut = Path.Combine(testRoot, "publishOut");
            Directory.CreateDirectory(publishOut);

            return new Folders { TestRoot = testRoot, ProjectPath = projectPath, PublishOutput = publishOut };
        }
    }
}
