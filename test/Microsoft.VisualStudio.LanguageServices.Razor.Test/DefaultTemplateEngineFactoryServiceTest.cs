// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Mvc1_X = Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X;
using MvcLatest = Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor
{
    public class DefaultTemplateEngineFactoryServiceTest
    {
        [Fact]
        public void Create_CreatesDesignTimeTemplateEngine_ForLatest()
        {
            // Arrange
            var mvcReference = GetAssemblyMetadataReference("Microsoft.AspNetCore.Mvc.Razor", "2.0.0");
            var services = GetServices(mvcReference);
            var factoryService = new DefaultTemplateEngineFactoryService(services);

            // Act
            var engine = factoryService.Create("/TestPath/SomePath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
                Assert.True(b.DesignTime);
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.MvcViewDocumentClassifierPass>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperPass>());
        }

        [Fact]
        public void Create_CreatesDesignTimeTemplateEngine_ForVersion1_1()
        {
            // Arrange
            var mvcReference = GetAssemblyMetadataReference("Microsoft.AspNetCore.Mvc.Razor", "1.1.3");
            var services = GetServices(mvcReference);
            var factoryService = new DefaultTemplateEngineFactoryService(services);

            // Act
            var engine = factoryService.Create("/TestPath/SomePath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
                Assert.True(b.DesignTime);
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<Mvc1_X.MvcViewDocumentClassifierPass>());
            Assert.Single(engine.Engine.Features.OfType<Mvc1_X.ViewComponentTagHelperPass>());
        }

        [Fact]
        public void Create_DoesNotSupportViewComponentTagHelpers_ForVersion1_0()
        {
            // Arrange
            var mvcReference = GetAssemblyMetadataReference("Microsoft.AspNetCore.Mvc.Razor", "1.0.0");
            var services = GetServices(mvcReference);
            var factoryService = new DefaultTemplateEngineFactoryService(services);

            // Act
            var engine = factoryService.Create("/TestPath/SomePath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<Mvc1_X.MvcViewDocumentClassifierPass>());
            Assert.Empty(engine.Engine.Features.OfType<Mvc1_X.ViewComponentTagHelperPass>());
        }

        [Fact]
        public void Create_UnknownMvcVersion_UsesLatest()
        {
            // Arrange
            var mvcReference = GetAssemblyMetadataReference("Microsoft.AspNetCore.Mvc.Razor", "3.0.0");
            var services = GetServices(mvcReference);
            var factoryService = new DefaultTemplateEngineFactoryService(services);

            // Act
            var engine = factoryService.Create("/TestPath/SomePath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
                Assert.True(b.DesignTime);
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.MvcViewDocumentClassifierPass>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperPass>());
        }

        [Fact]
        public void Create_UnknownProjectPath_UsesLatest()
        {
            // Arrange
            var mvcReference = GetAssemblyMetadataReference("Microsoft.AspNetCore.Mvc.Razor", "1.1.0");
            var services = GetServices(mvcReference);
            var factoryService = new DefaultTemplateEngineFactoryService(services);

            // Act
            var engine = factoryService.Create("/TestPath/DifferentPath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
                Assert.True(b.DesignTime);
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.MvcViewDocumentClassifierPass>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperPass>());
        }

        [Fact]
        public void Create_MvcReferenceNotFound_UsesLatest()
        {
            // Arrange
            var mvcReference = GetAssemblyMetadataReference("Microsoft.Something.Else", "1.0.0");
            var services = GetServices(mvcReference);
            var factoryService = new DefaultTemplateEngineFactoryService(services);

            // Act
            var engine = factoryService.Create("/TestPath/DifferentPath/", b =>
            {
                b.Features.Add(new MyCoolNewFeature());
                Assert.True(b.DesignTime);
            });

            // Assert
            Assert.Single(engine.Engine.Features.OfType<MyCoolNewFeature>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.MvcViewDocumentClassifierPass>());
            Assert.Single(engine.Engine.Features.OfType<MvcLatest.ViewComponentTagHelperPass>());
        }

        private HostLanguageServices GetServices(MetadataReference mvcReference)
        {
            var project = ProjectInfo
                .Create(ProjectId.CreateNewId(), VersionStamp.Default, "TestProject", "TestAssembly", LanguageNames.CSharp)
                .WithFilePath("/TestPath/SomePath/MyProject.csproj")
                .WithMetadataReferences(new[] { mvcReference });

            var workspace = new AdhocWorkspace();
            workspace.AddProject(project);

            return workspace.Services.GetLanguageServices(LanguageNames.CSharp);
        }

        private MetadataReference GetAssemblyMetadataReference(string assemblyName, string version)
        {
            var code = $@"
using System.Reflection;
[assembly: AssemblyVersion(""{version}"")]
";

            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

            return compilation.ToMetadataReference();
        }

        private class MyCoolNewFeature : IRazorEngineFeature
        {
            public RazorEngine Engine { get; set; }
        }
    }
}
