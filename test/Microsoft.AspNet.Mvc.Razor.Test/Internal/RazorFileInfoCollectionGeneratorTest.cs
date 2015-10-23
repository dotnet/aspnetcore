// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Razor.Compilation;
using Microsoft.AspNet.Mvc.Razor.Precompilation;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Internal
{
    public class RazorFileInfoCollectionGeneratorTest
    {
        public static TheoryData GenerateCollection_ProducesExpectedCodeData
        {
            get
            {
                var expected1 =
@"namespace __ASP_ASSEMBLY
{
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
    public class __PreGeneratedViewCollection : Microsoft.AspNet.Mvc.Razor.Precompilation.RazorFileInfoCollection
    {
        public __PreGeneratedViewCollection()
        {
            AssemblyResourceName = @""EmptyAssembly"";
            SymbolsResourceName = @"""";
            FileInfos = new System.Collections.Generic.List<Microsoft.AspNet.Mvc.Razor.Precompilation.RazorFileInfo>
            {
            };
        }

        private static System.Reflection.Assembly _loadedAssembly;

        public override System.Reflection.Assembly LoadAssembly(
            Microsoft.Extensions.PlatformAbstractions.IAssemblyLoadContext loadContext)
        {
             if (_loadedAssembly == null)
             {
                _loadedAssembly = base.LoadAssembly(loadContext);
             }
             return _loadedAssembly;   
        }
    }
}";
                var expected2 =
@"namespace __ASP_ASSEMBLY
{
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
    public class __PreGeneratedViewCollection : Microsoft.AspNet.Mvc.Razor.Precompilation.RazorFileInfoCollection
    {
        public __PreGeneratedViewCollection()
        {
            AssemblyResourceName = @""TestAssembly"";
            SymbolsResourceName = @""SymbolsResource"";
            FileInfos = new System.Collections.Generic.List<Microsoft.AspNet.Mvc.Razor.Precompilation.RazorFileInfo>
            {             
                new Microsoft.AspNet.Mvc.Razor.Precompilation.RazorFileInfo
                {
                    FullTypeName = @""SomeType.Name"",
                    RelativePath = @""Views/Home/Index.cshtml""
                },             
                new Microsoft.AspNet.Mvc.Razor.Precompilation.RazorFileInfo
                {
                    FullTypeName = @""Different.Name"",
                    RelativePath = @""Views/Home/Different.cshtml""
                },
            };
        }

        private static System.Reflection.Assembly _loadedAssembly;

        public override System.Reflection.Assembly LoadAssembly(
            Microsoft.Extensions.PlatformAbstractions.IAssemblyLoadContext loadContext)
        {
             if (_loadedAssembly == null)
             {
                _loadedAssembly = base.LoadAssembly(loadContext);
             }
             return _loadedAssembly;   
        }
    }
}";

                return new TheoryData<RazorFileInfoCollection, string>
                {
                    {  new EmptyCollection(), expected1 },
                    {  new TestCollection(), expected2 },
                };
            }
        }

        [Theory]
        [MemberData(nameof(GenerateCollection_ProducesExpectedCodeData))]
        public void GenerateCollection_ProducesExpectedCode(RazorFileInfoCollection collection, string expected)
        {
            // Act
            var actual = RazorFileInfoCollectionGenerator.GenerateCode(collection);

            // Assert
            Assert.Equal(expected, actual);
        }

        private class EmptyCollection : RazorFileInfoCollection
        {
            public EmptyCollection()
            {
                AssemblyResourceName = "EmptyAssembly";
                FileInfos = new List<RazorFileInfo>();
            }
        }

        private class TestCollection : RazorFileInfoCollection
        {
            public TestCollection()
            {
                AssemblyResourceName = "TestAssembly";
                SymbolsResourceName = "SymbolsResource";
                FileInfos = new List<RazorFileInfo>
                {
                    new RazorFileInfo
                    {
                        FullTypeName = "SomeType.Name",
                        RelativePath = @"Views/Home/Index.cshtml"
                    },
                    new RazorFileInfo
                    {
                        FullTypeName = "Different.Name",
                        RelativePath = @"Views/Home/Different.cshtml"
                    },
                };
            }
        }
    }
}
