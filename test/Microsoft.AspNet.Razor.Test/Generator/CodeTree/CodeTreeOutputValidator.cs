using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class CodeTreeOutputValidator
    {
        public static void ValidateResults(string codeTreeCode, string codeDOMCode, IList<LineMapping> codeTreeMappings, IDictionary<int, GeneratedCodeMapping> codeDOMMappings)
        {
            Assert.Equal(codeTreeMappings.Count, codeDOMMappings.Values.Count);

            ValidateNamespace(codeTreeCode, codeDOMCode);
            ValidateClass(codeTreeCode, codeDOMCode);
            ValidateUsings(codeTreeCode, codeDOMCode);
            ValidateNewObjects(codeTreeCode, codeDOMCode);
            ValidateBraces(codeTreeCode, codeDOMCode);
            ValidateLambdas(codeTreeCode, codeDOMCode);
            ValidateMembers(codeTreeCode, codeDOMCode);
            ValidateReturns(codeTreeCode, codeDOMCode);
            ValidateBaseTypes(codeTreeCode, codeDOMCode);
        }

        private static void ValidateNamespace(string codeTreeCode, string codeDOMCode)
        {
            ValidateCodeTreeContains(codeTreeCode, codeDOMCode, @"namespace [^\s{]+");

        }

        private static void ValidateClass(string codeTreeCode, string codeDOMCode)
        {
            ValidateCodeTreeContains(codeTreeCode, codeDOMCode, @"class [^\s{]+");
        }

        private static void ValidateUsings(string codeTreeCode, string codeDOMCode)
        {
            ValidateCodeTreeContainsAll(codeTreeCode, codeDOMCode, @"using [^\s{;]+");
        }

        private static void ValidateNewObjects(string codeTreeCode, string codeDOMCode)
        {
            ValidateCodeTreeContainsAll(codeTreeCode, codeDOMCode, @"new [^\(<\s]+");
        }

        private static void ValidateBraces(string codeTreeCode, string codeDOMCode)
        {
            var codeDOMMatches = Regex.Matches(codeDOMCode, "{");
            var codeTreeMatches = Regex.Matches(codeTreeCode, "{");

            Assert.NotEmpty(codeDOMMatches);
            Assert.NotEmpty(codeTreeMatches);
            if (codeDOMMatches.Count != codeTreeMatches.Count)
            {
                // 1 leniency for the design time helpers
                Assert.Equal(codeDOMMatches.Count, codeTreeMatches.Count - 1);
            }
            else
            {
                Assert.Equal(codeDOMMatches.Count, codeTreeMatches.Count);
            }

            codeDOMMatches = Regex.Matches(codeDOMCode, "}");
            codeTreeMatches = Regex.Matches(codeTreeCode, "}");

            Assert.NotEmpty(codeDOMMatches);
            Assert.NotEmpty(codeTreeMatches);
            if (codeDOMMatches.Count != codeTreeMatches.Count)
            {
                // 1 leniency for the design time helpers
                Assert.Equal(codeDOMMatches.Count, codeTreeMatches.Count - 1);
            }
            else
            {
                Assert.Equal(codeDOMMatches.Count, codeTreeMatches.Count);
            }
        }

        private static void ValidateLambdas(string codeTreeCode, string codeDOMCode)
        {
            ValidateCount(codeTreeCode, codeDOMCode, " => ");
        }

        private static void ValidateMembers(string codeTreeCode, string codeDOMCode)
        {
            ValidateCodeTreeContainsAll(codeTreeCode, codeDOMCode, @"(public|private) [^\s\(]+ [^\s\(]+");
        }

        private static void ValidateReturns(string codeTreeCode, string codeDOMCode)
        {
            ValidateCodeTreeContainsAll(codeTreeCode, codeDOMCode, @"return [^\s\(;]+");
        }

        private static void ValidateBaseTypes(string codeTreeCode, string codeDOMCode)
        {
            ValidateCodeTreeContainsAll(codeTreeCode, codeDOMCode, @"class [^\s]+ : [^\s{]+");
        }

        private static void ValidateKeywords(string codeTreeCode, string codeDOMCode)
        {
            ValidateCount(codeTreeCode, codeDOMCode, "Execute");
            ValidateCount(codeTreeCode, codeDOMCode, CSharpDesignTimeHelpersVisitor.InheritsHelper);
        }

        private static void ValidateCodeTreeContains(string codeTreeCode, string codeDOMCode, string regex)
        {
            var match = Regex.Match(codeDOMCode, regex);
            Assert.NotEmpty(match.Groups);
            Assert.True(codeTreeCode.IndexOf(match.Groups[0].Value) >= 0);
        }

        private static void ValidateCodeTreeContainsAll(string codeTreeCode, string codeDOMCode, string regex)
        {
            var matches = Regex.Matches(codeDOMCode, regex);
            Assert.NotEmpty(matches);

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];

                for (int j = 0; j < match.Groups.Count; j++)
                {
                    Assert.True(codeTreeCode.IndexOf(match.Groups[j].Value) >= 0);
                }
            }
        }

        private static void ValidateCount(string codeTreeCode, string codeDOMCode, string regex)
        {
            var codeDOMMatches = Regex.Matches(codeDOMCode, regex);
            var codeTreeMatches = Regex.Matches(codeTreeCode, regex);

            Assert.Equal(codeDOMMatches.Count, codeTreeMatches.Count);
        }
    }
}
