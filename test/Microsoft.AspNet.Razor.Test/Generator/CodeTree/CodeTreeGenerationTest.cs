// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using Microsoft.CSharp;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class CodeTreeGenerationTest : CSharpRazorCodeGeneratorTest
    {
        [Fact]
        public void CodeTreeComparisonTest()
        {
            RunTest("CodeTree", onResults: (results) =>
            {
                CodeDomProvider codeProvider = (CodeDomProvider)Activator.CreateInstance(typeof(CSharpCodeProvider));

                CodeGeneratorOptions options = new CodeGeneratorOptions();
                var output = new StringBuilder();
                using (var writer = new StringWriter(output))
                {
                    codeProvider.GenerateCodeFromCompileUnit(results.CCU, writer, options);
                }
                string codeDOMOutput = output.ToString();

                CodeTreeOutputValidator.ValidateResults(results.GeneratedCode, codeDOMOutput, results.DesignTimeLineMappings, results.OLDDesignTimeLineMappings);
            }, designTimeMode: true);
        }
    }
}
