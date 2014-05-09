// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
                Console.WriteLine(results.GeneratedCode);
            }, designTimeMode: true);
        }
    }
}
