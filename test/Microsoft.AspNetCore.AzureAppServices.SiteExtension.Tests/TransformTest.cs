// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Xml;
using Microsoft.Web.XmlTransform;
using Xunit;

namespace Microsoft.AspNetCore.AzureAppServices.SiteExtension
{
    public class TransformTest
    {
        [Theory]
        [InlineData("config_empty.xml")]
        [InlineData("config_existingline.xml")]
        [InlineData("config_existingEmptyValue.xml")]
        public void Transform_EmptyConfig_Added(string configFile)
        {
            var doc = LoadDocAndRunTransform(configFile);

            Assert.Equal(2, doc.ChildNodes.Count);
            var envNode = doc["configuration"]?["system.webServer"]?["runtime"]?["environmentVariables"];

            Assert.NotNull(envNode);

            Assert.Equal(2, envNode.ChildNodes.Count);

            var firstChild = envNode.FirstChild;
            Assert.Equal("add", firstChild.Name);
            Assert.Equal("DOTNET_ADDITIONAL_DEPS", firstChild.Attributes["name"].Value);
            Assert.Equal(@"%ProgramFiles%\dotnet\additionalDeps\Microsoft.AspNetCore.AzureAppServices.HostingStartup\",
                firstChild.Attributes["value"].Value);

            var secondChild = firstChild.NextSibling;
            Assert.Equal("add", secondChild.Name);
            Assert.Equal("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", secondChild.Attributes["name"].Value);
            Assert.Equal("Microsoft.AspNetCore.AzureAppServices.HostingStartup", secondChild.Attributes["value"].Value);
        }

        [Fact]
        public void Transform_ExistingValue_AppendsValue()
        {
            var doc = LoadDocAndRunTransform("config_existingvalue.xml");

            Assert.Equal(2, doc.ChildNodes.Count);
            var envNode = doc["configuration"]?["system.webServer"]?["runtime"]?["environmentVariables"];

            Assert.NotNull(envNode);

            Assert.Equal(2, envNode.ChildNodes.Count);

            var firstChild = envNode.FirstChild;
            Assert.Equal("add", firstChild.Name);
            Assert.Equal("DOTNET_ADDITIONAL_DEPS", firstChild.Attributes["name"].Value);
            Assert.Equal(@"ExistingValue1;%ProgramFiles%\dotnet\additionalDeps\Microsoft.AspNetCore.AzureAppServices.HostingStartup\",
                firstChild.Attributes["value"].Value);

            var secondChild = firstChild.NextSibling;
            Assert.Equal("add", secondChild.Name);
            Assert.Equal("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", secondChild.Attributes["name"].Value);
            Assert.Equal("ExistingValue2;Microsoft.AspNetCore.AzureAppServices.HostingStartup", secondChild.Attributes["value"].Value);
        }

        private static XmlDocument LoadDocAndRunTransform(string docName)
        {
            // Microsoft.Web.Hosting.Transformers.ApplicationHost.SiteExtensionDefinition.Transform
            // (See Microsoft.Web.Hosting, Version=7.1.0.0) replaces variables for you in Azure.
            var transformFile = File.ReadAllText("applicationHost.xdt");
            transformFile = transformFile.Replace("%XDT_EXTENSIONPATH%", AppDomain.CurrentDomain.BaseDirectory);
            var transform = new XmlTransformation(transformFile, isTransformAFile: false, logger: null);
            var doc = new XmlDocument();
            doc.Load(docName);
            Assert.True(transform.Apply(doc));
            return doc;
        }
    }
}
