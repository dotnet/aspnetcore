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
    <httpPlatform processPath=""..\test.exe"" stdoutLogEnabled=""false"" startupTimeLimit=""3600""/>
  </system.webServer>
</configuration>");

        [Fact]
        public void WebConfigTransform_creates_new_config_if_one_does_not_exist()
        {
            Assert.True(XNode.DeepEquals(WebConfigTemplate, WebConfigTransform.Transform(null, "test.exe")));
        }

        [Fact]
        public void WebConfigTransform_creates_new_config_if_one_has_unexpected_format()
        {
            Assert.True(XNode.DeepEquals(WebConfigTemplate, WebConfigTransform.Transform(XDocument.Parse("<unexpected />"), "test.exe")));
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

            Assert.True(XNode.DeepEquals(WebConfigTemplate, WebConfigTransform.Transform(input, "test.exe")));
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

            var output = WebConfigTransform.Transform(input, "test.exe");
            Assert.Equal(attributeValue, (string)output.Descendants(elementName).Single().Attribute(attributeName));
        }

        [Fact]
        public void WebConfigTransform_overwrites_processPath()
        {
            var newProcessPath =
                (string)WebConfigTransform.Transform(WebConfigTemplate, "app.exe")
                    .Descendants("httpPlatform").Single().Attribute("processPath");

            Assert.Equal(@"..\app.exe", newProcessPath);
        }

        [Fact]
        public void WebConfigTransform_fixes_httpPlatformHandler_casing()
        {
            var input = new XDocument(WebConfigTemplate);
            input.Descendants("add").Single().SetAttributeValue("name", "httpplatformhandler");

            Assert.True(XNode.DeepEquals(WebConfigTemplate, WebConfigTransform.Transform(input, "test.exe")));
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
                WebConfigTransform.Transform(input, "app.exe").Descendants("httpPlatform").Elements().Single()));
        }

        private bool VerifyMissingElementCreated(params string[] elementNames)
        {
            var input = new XDocument(WebConfigTemplate);
            foreach (var elementName in elementNames)
            {
                input.Descendants(elementName).Remove();
            }

            return XNode.DeepEquals(WebConfigTemplate, WebConfigTransform.Transform(input, "test.exe"));
        }
    }
}