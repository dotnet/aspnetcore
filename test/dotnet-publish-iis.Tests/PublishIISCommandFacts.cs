using Xunit;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System;

namespace Microsoft.AspNetCore.Tools.PublishIIS.Tests
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
            var webRoot = "wwwroot";
            var folders = CreateTestDir("{}", webRoot);

            new PublishIISCommand(folders.PublishOutput, folders.ProjectPath, null).Run();

            var processPath = (string)GetPublishedWebConfig(folders.PublishOutput, webRoot)
                .Descendants("httpPlatform").Attributes("processPath").Single();

            Assert.Equal($@"..\projectDir.exe", processPath);

            Directory.Delete(folders.TestRoot, recursive: true);
        }

        [Fact]
        public void PublishIIS_reads_application_name_from_project_json_if_exists()
        {
            var webRoot = "wwwroot";
            var folders = CreateTestDir(@"{ ""name"": ""awesomeApp""}", webRoot);

            new PublishIISCommand(folders.PublishOutput, folders.ProjectPath, null).Run();

            var processPath = (string)GetPublishedWebConfig(folders.PublishOutput, webRoot)
                .Descendants("httpPlatform").Attributes("processPath").Single();

            Assert.Equal($@"..\awesomeApp.exe", processPath);

            Directory.Delete(folders.TestRoot, recursive: true);
        }

        [Fact]
        public void PublishIIS_uses_webroot_from_hosting_json()
        {
            var webRoot = "mywebroot";
            var folders = CreateTestDir("{}", webRoot);
            File.WriteAllText(Path.Combine(folders.ProjectPath, "hosting.json"), $"{{ \"webroot\": \"{webRoot}\"}}");

            new PublishIISCommand(folders.PublishOutput, folders.ProjectPath, null).Run();

            var processPath = (string)GetPublishedWebConfig(folders.PublishOutput, webRoot)
                .Descendants("httpPlatform").Attributes("processPath").Single();

            Assert.Equal($@"..\projectDir.exe", processPath);

            Directory.Delete(folders.TestRoot, recursive: true);
        }

        [Fact]
        public void PublishIIS_webroot_switch_takes_precedence_over_hosting_json()
        {
            var webRoot = "mywebroot";
            var folders = CreateTestDir("{}", webRoot);
            File.WriteAllText(Path.Combine(folders.ProjectPath, "hosting.json"), $"{{ \"webroot\": \"wwwroot\"}}");

            new PublishIISCommand(folders.PublishOutput, folders.ProjectPath, webRoot).Run();

            var processPath = (string)GetPublishedWebConfig(folders.PublishOutput, webRoot)
                .Descendants("httpPlatform").Attributes("processPath").Single();

            Assert.Equal($@"..\projectDir.exe", processPath);

            Directory.Delete(folders.TestRoot, recursive: true);
        }

        [Fact]
        public void PublishIIS_accepts_path_to_project_json_as_project_path()
        {
            var webRoot = "wwwroot";
            var folders = CreateTestDir("{}", webRoot);

            new PublishIISCommand(folders.PublishOutput, Path.Combine(folders.ProjectPath, "project.json"), null).Run();

            var processPath = (string)GetPublishedWebConfig(folders.PublishOutput, webRoot)
                .Descendants("httpPlatform").Attributes("processPath").Single();

            Assert.Equal($@"..\projectDir.exe", processPath);

            Directory.Delete(folders.TestRoot, recursive: true);
        }

        [Fact]
        public void PublishIIS_modifies_existing_web_config()
        {
            var webRoot = "wwwroot";
            var folders = CreateTestDir("{}", webRoot);

            File.WriteAllText(Path.Combine(folders.PublishOutput, webRoot, "web.config"),
@"<configuration>
  <system.webServer>
    <handlers>
      <add name=""httpPlatformHandler"" path=""*"" verb=""*"" modules=""httpPlatformHandler"" resourceType=""Unspecified""/>
    </handlers>
    <httpPlatform processPath=""%________%"" stdoutLogEnabled=""false"" startupTimeLimit=""1234""/>
  </system.webServer>
</configuration>");

            new PublishIISCommand(folders.PublishOutput, Path.Combine(folders.ProjectPath, "project.json"), null).Run();

            var httpPlatformElement = GetPublishedWebConfig(folders.PublishOutput, webRoot)
                .Descendants("httpPlatform").Single();

            Assert.Equal($@"..\projectDir.exe", (string)httpPlatformElement.Attribute("processPath"));
            Assert.Equal($@"1234", (string)httpPlatformElement.Attribute("startupTimeLimit"));

            Directory.Delete(folders.TestRoot, recursive: true);
        }

        private XDocument GetPublishedWebConfig(string publishOut, string webRoot)
        {
            return XDocument.Load(Path.Combine(publishOut, webRoot, "web.config"));
        }

        private Folders CreateTestDir(string projectJson, string webRoot)
        {
            var testRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(testRoot);

            var projectPath = Path.Combine(testRoot, "projectDir");
            Directory.CreateDirectory(projectPath);
            Directory.CreateDirectory(Path.Combine(projectPath, webRoot));
            File.WriteAllText(Path.Combine(projectPath, "project.json"), projectJson);

            var publishOut = Path.Combine(testRoot, "publishOut");
            Directory.CreateDirectory(publishOut);
            Directory.CreateDirectory(Path.Combine(publishOut, webRoot));

            return new Folders { TestRoot = testRoot, ProjectPath = projectPath, PublishOutput = publishOut };
        }
    }
}
