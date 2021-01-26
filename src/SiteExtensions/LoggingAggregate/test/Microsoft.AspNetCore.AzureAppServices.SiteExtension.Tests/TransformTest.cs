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
        private static readonly string XdtExtensionPath = AppDomain.CurrentDomain.BaseDirectory;

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

            Assert.Equal(3, envNode.ChildNodes.Count);

            var depsElement = envNode.FirstChild;
            Assert.Equal("add", depsElement.Name);
            Assert.Equal("DOTNET_ADDITIONAL_DEPS", depsElement.Attributes["name"].Value);
            Assert.Equal($@"{XdtExtensionPath}\additionalDeps\;{XdtExtensionPath}\additionalDeps\Microsoft.AspNetCore.AzureAppServices.HostingStartup\;" +
                         @"%ProgramFiles%\dotnet\additionalDeps\Microsoft.AspNetCore.AzureAppServices.HostingStartup\",
                depsElement.Attributes["value"].Value);

            var sharedStoreElement = depsElement.NextSibling;
            Assert.Equal("add", sharedStoreElement.Name);
            Assert.Equal("DOTNET_SHARED_STORE", sharedStoreElement.Attributes["name"].Value);
            Assert.Equal($@"{XdtExtensionPath}\store", sharedStoreElement.Attributes["value"].Value);

            var startupAssembliesElement = sharedStoreElement.NextSibling;
            Assert.Equal("add", startupAssembliesElement.Name);
            Assert.Equal("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", startupAssembliesElement.Attributes["name"].Value);
            Assert.Equal("Microsoft.AspNetCore.AzureAppServices.HostingStartup", startupAssembliesElement.Attributes["value"].Value);
        }

        [Fact]
        public void Transform_ExistingValue_AppendsValue()
        {
            var doc = LoadDocAndRunTransform("config_existingvalue.xml");

            Assert.Equal(2, doc.ChildNodes.Count);
            var envNode = doc["configuration"]?["system.webServer"]?["runtime"]?["environmentVariables"];

            Assert.NotNull(envNode);

            Assert.Equal(3, envNode.ChildNodes.Count);

            var depsElement = envNode.FirstChild;
            Assert.Equal("add", depsElement.Name);
            Assert.Equal("DOTNET_ADDITIONAL_DEPS", depsElement.Attributes["name"].Value);
            Assert.Equal(@"ExistingValue1;"+
                         $@"{XdtExtensionPath}\additionalDeps\;{XdtExtensionPath}\additionalDeps\Microsoft.AspNetCore.AzureAppServices.HostingStartup\;" +
                         @"%ProgramFiles%\dotnet\additionalDeps\Microsoft.AspNetCore.AzureAppServices.HostingStartup\",
                depsElement.Attributes["value"].Value);

            var sharedStoreElement = depsElement.NextSibling;
            Assert.Equal("add", sharedStoreElement.Name);
            Assert.Equal("DOTNET_SHARED_STORE", sharedStoreElement.Attributes["name"].Value);
            Assert.Equal($@"ExistingValue3;{XdtExtensionPath}\store", sharedStoreElement.Attributes["value"].Value);

            var startupAssembliesElement = sharedStoreElement.NextSibling;
            Assert.Equal("add", startupAssembliesElement.Name);
            Assert.Equal("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", startupAssembliesElement.Attributes["name"].Value);
            Assert.Equal("ExistingValue2;Microsoft.AspNetCore.AzureAppServices.HostingStartup", startupAssembliesElement.Attributes["value"].Value);
        }

        private static XmlDocument LoadDocAndRunTransform(string docName)
        {
            // Microsoft.Web.Hosting.Transformers.ApplicationHost.SiteExtensionDefinition.Transform
            // (See Microsoft.Web.Hosting, Version=7.1.0.0) replaces variables for you in Azure.
            var transformFile = File.ReadAllText("applicationHost.xdt");
            transformFile = transformFile.Replace("%XDT_EXTENSIONPATH%", XdtExtensionPath);
            var transform = new XmlTransformation(transformFile, isTransformAFile: false, logger: null);
            var doc = new XmlDocument();
            doc.Load(docName);
            Assert.True(transform.Apply(doc));
            return doc;
        }
    }
}
