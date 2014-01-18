// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;

namespace Microsoft.AspNet.Razor.Test
{
    internal static class CodeCompileUnitExtensions
    {
        public static string GenerateCode<T>(this CodeCompileUnit ccu) where T : CodeDomProvider, new()
        {
            StringBuilder output = new StringBuilder();
            using (StringWriter writer = new StringWriter(output))
            {
                T provider = new T();
                provider.GenerateCodeFromCompileUnit(ccu, writer, new CodeGeneratorOptions() { IndentString = "    " });
            }

            return output.ToString();
        }
    }
}
