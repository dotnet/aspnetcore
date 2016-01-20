using Xunit;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Tools.PublishIIS.Tests
{
    public class WebConfigTransformFacts
    {
        private  XDocument WebConfigTemplate => XDocument.Parse(
@"<configuration>
  <system.webServer>
    <handlers>
      <add name=""httpPlatformHandler"" path=""*"" verb=""*"" modules=""httpPlatformHandler"" resourceType=""Unspecified""/>
    </handlers>
    <httpPlatform processPath=""..\test.exe"" stdoutLogEnabled=""false"" stdoutLogFile=""..\logs\stdout.log"" startupTimeLimit=""3600""/>
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
        [InlineData(new object[] { new[] {"system.webServer"}})]
        [InlineData(new object[] { new[] {"add"}})]
        [InlineData(new object[] { new[] {"handlers"}})]
        [InlineData(new object[] { new[] {"httpPlatform"}})]
        [InlineData(new object[] { new[] {"handlers", "httpPlatform"}})]
        public void WebConfigTransform_adds_missing_elements(string[] elementNames)
        {
            var input = new XDocument(WebConfigTemplate);
            foreach (var elementName in elementNames)
            {
                input.Descendants(elementName).Remove();
            }

            Assert.True(XNode.DeepEquals(WebConfigTemplate,
                WebConfigTransform.Transform(input, "test.exe", configureForAzure: false)));
        }

        [Theory]
        [InlineData("add", "path", "test")]
        [InlineData("add", "verb","test")]
        [InlineData("add", "modules", "mods")]
        [InlineData("add", "resourceType", "Either")]
        [InlineData("httpPlatform", "stdoutLogEnabled", "true")]
        [InlineData("httpPlatform", "startupTimeLimit", "1200")]
        [InlineData("httpPlatform", "arguments", "arg1")]
        [InlineData("httpPlatform", "stdoutLogFile", "logfile.log")]
        public void WebConfigTransform_wont_override_custom_values(string elementName, string attributeName, string attributeValue)
        {
            var input = new XDocument(WebConfigTemplate);
            input.Descendants(elementName).Single().SetAttributeValue(attributeName, attributeValue);

            var output = WebConfigTransform.Transform(input, "test.exe", configureForAzure: false);
            Assert.Equal(attributeValue, (string)output.Descendants(elementName).Single().Attribute(attributeName));
        }

        [Fact]
        public void WebConfigTransform_overwrites_processPath()
        {
            var newProcessPath =
                (string)WebConfigTransform.Transform(WebConfigTemplate, "app.exe", configureForAzure: false)
                    .Descendants("httpPlatform").Single().Attribute("processPath");

            Assert.Equal(@"..\app.exe", newProcessPath);
        }

        [Fact]
        public void WebConfigTransform_fixes_httpPlatformHandler_casing()
        {
            var input = new XDocument(WebConfigTemplate);
            input.Descendants("add").Single().SetAttributeValue("name", "httpplatformhandler");

            Assert.True(XNode.DeepEquals(WebConfigTemplate,
                WebConfigTransform.Transform(input, "test.exe", configureForAzure: false)));
        }

        [Fact]
        public void WebConfigTransform_does_not_remove_children_of_httpPlatform_element()
        {
            var envVarsElement =
                new XElement("environmentVariables",
                    new XElement("environmentVariable", new XAttribute("name", "ENVVAR"), new XAttribute("value", "123")));

            var input = new XDocument(WebConfigTemplate);
            input.Descendants("httpPlatform").Single().Add(envVarsElement);

            Assert.True(XNode.DeepEquals(envVarsElement,
                WebConfigTransform.Transform(input, "app.exe", configureForAzure: false)
                    .Descendants("httpPlatform").Elements().Single()));
        }

        [Fact]
        public void WebConfigTransform_adds_stdoutLogEnabled_if_attribute_is_missing()
        {
            var input = new XDocument(WebConfigTemplate);
            input.Descendants("httpPlatform").Attributes("stdoutLogEnabled").Remove();

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
            var input = new XDocument(WebConfigTemplate);

            var httpPlatformElement = input.Descendants("httpPlatform").Single();
            httpPlatformElement.Attribute("stdoutLogEnabled").Remove();
            if (stdoutLogFile != null)
            {
                httpPlatformElement.SetAttributeValue("stdoutLogEnabled", stdoutLogFile);
            }

            Assert.Equal(
                @"..\logs\stdout.log",
                (string)WebConfigTransform.Transform(input, "test.exe", configureForAzure: false)
                    .Descendants().Attributes("stdoutLogFile").Single());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("true")]
        [InlineData("false")]
        public void WebConfigTransform_does_not_change_existing_stdoutLogEnabled(string stdoutLogEnabledValue)
        {
            var input = new XDocument(WebConfigTemplate);
            var httpPlatformElement = input.Descendants("httpPlatform").Single();

            httpPlatformElement.SetAttributeValue("stdoutLogFile", "mylog.txt");
            httpPlatformElement.Attributes("stdoutLogEnabled").Remove();
            if (stdoutLogEnabledValue != null)
            {
                input.Descendants("httpPlatform").Single().SetAttributeValue("stdoutLogEnabled", stdoutLogEnabledValue);
            }

            Assert.Equal(
                "mylog.txt",
                (string)WebConfigTransform.Transform(input, "test.exe", configureForAzure: false)
                    .Descendants().Attributes("stdoutLogFile").Single());
        }

        [Fact]
        public void WebConfigTransform_correctly_configures_for_Azure()
        {
            var input = new XDocument(WebConfigTemplate);
            input.Descendants("httpPlatform").Attributes().Remove();

            Assert.True(XNode.DeepEquals(
                XDocument.Parse(@"<httpPlatform processPath=""%home%\site\test.exe"" stdoutLogEnabled=""false""
                    stdoutLogFile=""\\?\%home%\LogFiles\stdout.log"" startupTimeLimit=""3600""/>").Root,
                WebConfigTransform.Transform(input, "test.exe", configureForAzure: true).Descendants("httpPlatform").Single()));
        }

        private bool VerifyMissingElementCreated(params string[] elementNames)
        {
            var input = new XDocument(WebConfigTemplate);
            foreach (var elementName in elementNames)
            {
                input.Descendants(elementName).Remove();
            }

            return XNode.DeepEquals(WebConfigTemplate,
                WebConfigTransform.Transform(input, "test.exe", configureForAzure: false));
        }
    }
}