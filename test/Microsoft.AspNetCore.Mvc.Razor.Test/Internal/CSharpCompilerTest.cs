// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class CSharpCompilerTest
    {
        [Fact]
        public void Compile_UsesApplicationsCompilationSettings_ForParsingAndCompilation()
        {
            // Arrange
            var content = "public class Test {}";
            var define = "MY_CUSTOM_DEFINE";
            var options = new TestOptionsManager<RazorViewEngineOptions>();
            options.Value.ParseOptions = options.Value.ParseOptions.WithPreprocessorSymbols(define);
            var razorReferenceManager = new DefaultRazorReferenceManager(GetApplicationPartManager(), options);
            var compiler = new CSharpCompiler(razorReferenceManager, options);

            // Act
            var syntaxTree = compiler.CreateSyntaxTree(SourceText.From(content));

            // Assert
            Assert.Contains(define, syntaxTree.Options.PreprocessorSymbolNames);
        }

        private static ApplicationPartManager GetApplicationPartManager()
        {
            var applicationPartManager = new ApplicationPartManager();
            var assembly = typeof(CSharpCompilerTest).GetTypeInfo().Assembly;
            applicationPartManager.ApplicationParts.Add(new AssemblyPart(assembly));
            applicationPartManager.FeatureProviders.Add(new MetadataReferenceFeatureProvider());

            return applicationPartManager;
        }
    }
}
