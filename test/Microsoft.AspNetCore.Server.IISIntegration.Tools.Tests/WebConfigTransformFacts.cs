using Xunit;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Server.IISIntegration.Tools.Tests
{
    public class WebConfigTransformFacts
    {
        private XDocument WebConfigTemplate => XDocument.Parse(
@"<configuration>
  <system.webServer>
    <handlers>
      <add name=""aspNetCore"" path=""*"" verb=""*"" modules=""AspNetCoreModule"" resourceType=""Unspecified""/>
    </handlers>
    <aspNetCore processPath="".\test.exe"" stdoutLogEnabled=""false"" stdoutLogFile="".\logs\stdout"" startupTimeLimit=""3600""/>
  </system.webServer>
</configuration>");

        [Fact]
        public void WebConfigTransform_creates_new_config_if_one_does_not_exist()
        {
            Assert.True(XNode.DeepEquals(WebConfigTemplate,
                    WebConfigTransform.Transform(null, "test.exe", configureForAzure: false)));
        }

        [Fact]
        public void WebConfigTransform_creates_new_config_if_one_has_unexpected_format()
        {
            Assert.True(XNode.DeepEquals(WebConfigTemplate,
                WebConfigTransform.Transform(XDocument.Parse("<unexpected />"), "test.exe", configureForAzure: false)));
        }

        [Theory]
        [InlineData(new object[] { new[] { "system.webServer" } })]
        [InlineData(new object[] { new[] { "add" } })]
        [InlineData(new object[] { new[] { "handlers" } })]
        [InlineData(new object[] { new[] { "aspNetCore" } })]
        [InlineData(new object[] { new[] { "environmentVariables" } })]
        [InlineData(new object[] { new[] { "environmentVariable" } })]
        [InlineData(new object[] { new[] { "handlers", "aspNetCore", "environmentVariables" } })]
        public void WebConfigTransform_adds_missing_elements(string[] elementNames)
        {
            var input = WebConfigTemplate;
            foreach (var elementName in elementNames)
            {
                input.Descendants(elementName).Remove();
            }

            Assert.True(XNode.DeepEquals(WebConfigTemplate,
                WebConfigTransform.Transform(input, "test.exe", configureForAzure: false)));
        }

        [Theory]
        [InlineData("add", "path", "test")]
        [InlineData("add", "verb", "test")]
        [InlineData("add", "modules", "mods")]
        [InlineData("add", "resourceType", "Either")]
        [InlineData("aspNetCore", "stdoutLogEnabled", "true")]
        [InlineData("aspNetCore", "startupTimeLimit", "1200")]
        [InlineData("aspNetCore", "arguments", "arg1")]
        [InlineData("aspNetCore", "stdoutLogFile", "logfile")]
        public void WebConfigTransform_wont_override_custom_values(string elementName, string attributeName, string attributeValue)
        {
            var input = WebConfigTemplate;
            input.Descendants(elementName).Single().SetAttributeValue(attributeName, attributeValue);

            var output = WebConfigTransform.Transform(input, "test.exe", configureForAzure: false);
            Assert.Equal(attributeValue, (string)output.Descendants(elementName).Single().Attribute(attributeName));
        }

        [Fact]
        public void WebConfigTransform_overwrites_processPath()
        {
            var newProcessPath =
                (string)WebConfigTransform.Transform(WebConfigTemplate, "app.exe", configureForAzure: false)
                    .Descendants("aspNetCore").Single().Attribute("processPath");

            Assert.Equal(@".\app.exe", newProcessPath);
        }

        [Fact]
        public void WebConfigTransform_fixes_aspnetcore_casing()
        {
            var input = WebConfigTemplate;
            input.Descendants("add").Single().SetAttributeValue("name", "aspnetcore");

            Assert.True(XNode.DeepEquals(WebConfigTemplate,
                WebConfigTransform.Transform(input, "test.exe", configureForAzure: false)));
        }

        [Fact]
        public void WebConfigTransform_does_not_remove_children_of_aspNetCore_element()
        {
            var envVarElement =
                new XElement("environmentVariable", new XAttribute("name", "ENVVAR"), new XAttribute("value", "123"));

            var input = WebConfigTemplate;
            input.Descendants("aspNetCore").Single().Add(envVarElement);

            Assert.True(XNode.DeepEquals(envVarElement,
                WebConfigTransform.Transform(input, "app.exe", configureForAzure: false)
                    .Descendants("environmentVariable").SingleOrDefault(e => (string)e.Attribute("name") == "ENVVAR")));
        }

        [Fact]
        public void WebConfigTransform_adds_stdoutLogEnabled_if_attribute_is_missing()
        {
            var input = WebConfigTemplate;
            input.Descendants("aspNetCore").Attributes("stdoutLogEnabled").Remove();

            Assert.Equal(
                "false",
                (string)WebConfigTransform.Transform(input, "test.exe", configureForAzure: false)
                    .Descendants().Attributes("stdoutLogEnabled").Single());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("false")]
        [InlineData("true")]
        public void WebConfigTransform_adds_stdoutLogFile_if_attribute_is_missing(string stdoutLogFile)
        {
            var input = WebConfigTemplate;

            var aspNetCoreElement = input.Descendants("aspNetCore").Single();
            aspNetCoreElement.Attribute("stdoutLogEnabled").Remove();
            if (stdoutLogFile != null)
            {
                aspNetCoreElement.SetAttributeValue("stdoutLogEnabled", stdoutLogFile);
            }

            Assert.Equal(
                @".\logs\stdout",
                (string)WebConfigTransform.Transform(input, "test.exe", configureForAzure: false)
                    .Descendants().Attributes("stdoutLogFile").Single());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("true")]
        [InlineData("false")]
        public void WebConfigTransform_does_not_change_existing_stdoutLogEnabled(string stdoutLogEnabledValue)
        {
            var input = WebConfigTemplate;
            var aspNetCoreElement = input.Descendants("aspNetCore").Single();

            aspNetCoreElement.SetAttributeValue("stdoutLogFile", "mylog.txt");
            aspNetCoreElement.Attributes("stdoutLogEnabled").Remove();
            if (stdoutLogEnabledValue != null)
            {
                input.Descendants("aspNetCore").Single().SetAttributeValue("stdoutLogEnabled", stdoutLogEnabledValue);
            }

            Assert.Equal(
                "mylog.txt",
                (string)WebConfigTransform.Transform(input, "test.exe", configureForAzure: false)
                    .Descendants().Attributes("stdoutLogFile").Single());
        }

        [Fact]
        public void WebConfigTransform_correctly_configures_for_Azure()
        {
            var input = WebConfigTemplate;
            input.Descendants("aspNetCore").Attributes().Remove();

            var aspNetCoreElement = WebConfigTransform.Transform(input, "test.exe", configureForAzure: true)
                .Descendants("aspNetCore").Single();
            aspNetCoreElement.Elements().Remove();

            Assert.True(XNode.DeepEquals(
                XDocument.Parse(@"<aspNetCore processPath=""%home%\site\test.exe"" stdoutLogEnabled=""false""
                    stdoutLogFile=""\\?\%home%\LogFiles\stdout"" startupTimeLimit=""3600""/>").Root,
                aspNetCoreElement));
        }

        private bool VerifyMissingElementCreated(params string[] elementNames)
        {
            var input = WebConfigTemplate;
            foreach (var elementName in elementNames)
            {
                input.Descendants(elementName).Remove();
            }

            return XNode.DeepEquals(WebConfigTemplate,
                WebConfigTransform.Transform(input, "test.exe", configureForAzure: false));
        }
    }
}