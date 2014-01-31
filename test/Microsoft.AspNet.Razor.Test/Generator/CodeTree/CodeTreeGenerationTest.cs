// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.IO;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class CodeTreeGenerationTest : CSharpRazorCodeGeneratorTest
    {        
        [Fact]
        public void CodeTreeComparisonTest()
        {
            RunTest("CodeTree", onResults: (results, codDOMOutput) =>
            {
                CodeTreeOutputValidator.ValidateResults(results.GeneratedCode, codDOMOutput, results.DesignTimeLineMappings, results.OLDDesignTimeLineMappings);
                File.WriteAllText("./testfile_ct.cs", results.GeneratedCode);
                File.WriteAllText("./testfile_cd.cs", codDOMOutput);
            }, designTimeMode: true);
        }   
    }
}
