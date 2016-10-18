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

        [Theory]
        [InlineData("netcoreapp1.0")]
        [InlineData("netstandard1.5")]
        public void PublishIIS_uses_default_values_if_options_not_specified(string targetFramework)
        {
            var folders = CreateTestDir($@"{{ ""frameworks"": {{ ""{targetFramework}"": {{ }} }} }}");

            new PublishIISCommand(folders.PublishOutput, targetFramework, null, folders.ProjectPath).Run();

            var processPath = (string)GetPublishedWebConfig(folders.PublishOutput)
                .Descendants("aspNetCore").Attributes("processPath").Single();

            Assert.Equal(@".\projectDir.exe", processPath);

            Directory.Delete(folders.TestRoot, recursive: true);
        }

        [Fact]
        public void PublishIIS_can_publish_for_portable_app()
        {
            var folders = CreateTestDir(
@"
  {
    ""frameworks"": {
      ""netcoreapp1.0"": {
        ""dependencies"": {
          ""Microsoft.NETCore.App"": {
            ""version"": ""1.0.0-*"",
            ""type"": ""platform""
          }
        }
      }
    }
  }");

            new PublishIISCommand(folders.PublishOutput, "netcoreapp1.0", null, folders.ProjectPath).Run();

            var aspNetCoreElement = GetPublishedWebConfig(folders.PublishOutput)
                .Descendants("aspNetCore").Single();

            Assert.Equal(@"dotnet", (string)aspNetCoreElement.Attribute("processPath"));
            Assert.Equal(@".\projectDir.dll", (string)aspNetCoreElement.Attribute("arguments"));

            Directory.Delete(folders.TestRoot, recursive: true);
        }

        [Theory]
        [InlineData("awesomeApp")]
        [InlineData("awesome.App")]
        public void PublishIIS_reads_application_name_from_project_json_if_exists(string projectName)
        {
            var folders = CreateTestDir($@"{{ ""name"": ""{projectName}"", ""frameworks"": {{ ""netcoreapp1.0"": {{}} }} }}");

            new PublishIISCommand(folders.PublishOutput, "netcoreapp1.0", null, folders.ProjectPath).Run();

            var processPath = (string)GetPublishedWebConfig(folders.PublishOutput)
                .Descendants("aspNetCore").Attributes("processPath").Single();

            Assert.Equal($@".\{projectName}.exe", processPath);

            Directory.Delete(folders.TestRoot, recursive: true);
        }

        [Fact]
        public void PublishIIS_reads_application_name_from_outputName_if_specified()
        {
            var folders = CreateTestDir(
@"{
    ""name"": ""awesomeApp"",
    ""buildOptions"": { ""outputName"": ""myApp"" },
    ""frameworks"": { ""netcoreapp1.0"": { } }
}");

            new PublishIISCommand(folders.PublishOutput, "netcoreapp1.0", null, folders.ProjectPath).Run();

            var processPath = (string)GetPublishedWebConfig(folders.PublishOutput)
                .Descendants("aspNetCore").Attributes("processPath").Single();

            Assert.Equal(@".\myApp.exe", processPath);

            Directory.Delete(folders.TestRoot, recursive: true);
        }

        [Theory]
        [InlineData("Debug", "myApp")]
        [InlineData("Release", "awesomeApp")]
        public void PublishIIS_reads_application_name_from_configuration_specific_outputName_if_specified(string configuration, string expectedName)
        {
            var folders = CreateTestDir(
@"{
    ""name"": ""awesomeApp"",
    ""configurations"": { ""Debug"": { ""buildOptions"": { ""outputName"": ""myApp"" } } },
    ""frameworks"": { ""netcoreapp1.0"": { } }
}");

            new PublishIISCommand(folders.PublishOutput, "netcoreapp1.0", configuration, folders.ProjectPath).Run();

            var processPath = (string)GetPublishedWebConfig(folders.PublishOutput)
                .Descendants("aspNetCore").Attributes("processPath").Single();

            Assert.Equal($@".\{expectedName}.exe", processPath);

            Directory.Delete(folders.TestRoot, recursive: true);
        }

        [Theory]
        [InlineData("projectDir")]
        [InlineData("project.Dir")]
        public void PublishIIS_accepts_path_to_project_json_as_project_path(string projectDir)
        {
            var folders = CreateTestDir(@"{ ""frameworks"": { ""netcoreapp1.0"": { } } }", projectDir);

            new PublishIISCommand(folders.PublishOutput, "netcoreapp1.0", null,
                    Path.Combine(folders.ProjectPath, "project.json")).Run();

            var processPath = (string)GetPublishedWebConfig(folders.PublishOutput)
                .Descendants("aspNetCore").Attributes("processPath").Single();

            Assert.Equal($@".\{projectDir}.exe", processPath);

            Directory.Delete(folders.TestRoot, recursive: true);
        }

        [Fact]
        public void PublishIIS_modifies_existing_web_config()
        {
            var folders = CreateTestDir(@"{ ""frameworks"": { ""netcoreapp1.0"": { } } }");

            File.WriteAllText(Path.Combine(folders.PublishOutput, "web.config"),
@"<configuration>
  <system.webServer>
    <handlers>
      <add name=""aspNetCore"" path=""*"" verb=""*"" modules=""AspNetCoreModule"" resourceType=""Unspecified""/>
    </handlers>
    <aspNetCore processPath=""%________%"" stdoutLogEnabled=""false"" startupTimeLimit=""1234""/>
  </system.webServer>
</configuration>");

            new PublishIISCommand(folders.PublishOutput, "netcoreapp1.0", null,
                    Path.Combine(folders.ProjectPath, "project.json")).Run();

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
