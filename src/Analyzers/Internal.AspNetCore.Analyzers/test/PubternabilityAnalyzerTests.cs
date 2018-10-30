// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Analyzer.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Internal.AspNetCore.Analyzers.Tests
{
    public class PubternabilityAnalyzerTests : DiagnosticVerifier
    {

        private const string InternalDefinitions = @"
namespace A.Internal.Namespace
{
   public class C {}
   public delegate C CD ();
   public class CAAttribute: System.Attribute {}

   public class Program
   {
       public static void Main() {}
   }
}";
        public PubternabilityAnalyzerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory]
        [MemberData(nameof(PublicMemberDefinitions))]
        public async Task PublicExposureOfPubternalTypeProducesPUB0001(string member)
        {
            var code = GetSourceFromNamespaceDeclaration($@"
namespace A
{{
    public class T
    {{
        {member}
    }}
}}");
            var diagnostic = Assert.Single(await GetDiagnostics(code.Source));
            Assert.Equal("PUB0001", diagnostic.Id);
            AnalyzerAssert.DiagnosticLocation(code.DefaultMarkerLocation, diagnostic.Location);
        }

        [Theory]
        [MemberData(nameof(PublicMemberWithAllowedDefinitions))]
        public async Task PublicExposureOfPubternalMembersSometimesAllowed(string member)
        {
            var code = GetSourceFromNamespaceDeclaration($@"
namespace A
{{
    public class T
    {{
        {member}
    }}
}}");
            Assert.Empty(await GetDiagnostics(code.Source));
        }


        [Theory]
        [MemberData(nameof(PublicTypeDefinitions))]
        public async Task PublicExposureOfPubternalTypeProducesInTypeDefinitionPUB0001(string member)
        {
            var code = GetSourceFromNamespaceDeclaration($@"
namespace A
{{
    {member}
}}");
            var diagnostic = Assert.Single(await GetDiagnostics(code.Source));
            Assert.Equal("PUB0001", diagnostic.Id);
            AnalyzerAssert.DiagnosticLocation(code.DefaultMarkerLocation, diagnostic.Location);
        }

        [Theory]
        [MemberData(nameof(PublicMemberDefinitions))]
        public async Task PrivateUsageOfPubternalTypeDoesNotProduce(string member)
        {
            var code = GetSourceFromNamespaceDeclaration($@"
namespace A
{{
    internal class T
    {{
        {member}
    }}
}}");
            var diagnostics = await GetDiagnostics(code.Source);
            Assert.Empty(diagnostics);
        }

        [Theory]
        [MemberData(nameof(PrivateMemberDefinitions))]
        public async Task PrivateUsageOfPubternalTypeDoesNotProduceInPublicClasses(string member)
        {
            var code = GetSourceFromNamespaceDeclaration($@"
namespace A
{{
    public class T
    {{
        {member}
    }}
}}");
            var diagnostics = await GetDiagnostics(code.Source);
            Assert.Empty(diagnostics);
        }


        [Theory]
        [MemberData(nameof(PublicTypeWithAllowedDefinitions))]
        public async Task PublicExposureOfPubternalTypeSometimesAllowed(string member)
        {
            var code = GetSourceFromNamespaceDeclaration($@"
namespace A
{{
    {member}
}}");
            var diagnostics = await GetDiagnostics(code.Source);
            Assert.Empty(diagnostics);
        }

        [Theory]
        [MemberData(nameof(PrivateMemberDefinitions))]
        [MemberData(nameof(PublicMemberDefinitions))]
        public async Task DefinitionOfPubternalCrossAssemblyProducesPUB0002(string member)
        {
            var code = TestSource.Read($@"
using A.Internal.Namespace;
namespace A
{{
    internal class T
    {{
        {member}
    }}
}}");

            var diagnostic = Assert.Single(await GetDiagnosticWithProjectReference(code.Source));
            Assert.Equal("PUB0002", diagnostic.Id);
            AnalyzerAssert.DiagnosticLocation(code.DefaultMarkerLocation, diagnostic.Location);
        }

        [Theory]
        [MemberData(nameof(TypeUsages))]
        public async Task UsageOfPubternalCrossAssemblyProducesPUB0002(string usage)
        {
            var code = TestSource.Read($@"
using A.Internal.Namespace;
namespace A
{{
    public class T
    {{
        private void M()
        {{
            {usage}
        }}
    }}
}}");
            var diagnostic = Assert.Single(await GetDiagnosticWithProjectReference(code.Source));
            Assert.Equal("PUB0002", diagnostic.Id);
            AnalyzerAssert.DiagnosticLocation(code.DefaultMarkerLocation, diagnostic.Location);
        }

        public static IEnumerable<object[]> PublicMemberDefinitions =>
            ApplyModifiers(MemberDefinitions, "public", "protected");

        public static IEnumerable<object[]> PublicMemberWithAllowedDefinitions =>
            ApplyModifiers(AllowedMemberDefinitions, "public");

        public static IEnumerable<object[]> PublicTypeDefinitions =>
            ApplyModifiers(TypeDefinitions, "public");

        public static IEnumerable<object[]> PublicTypeWithAllowedDefinitions =>
            ApplyModifiers(AllowedDefinitions, "public");

        public static IEnumerable<object[]> PrivateMemberDefinitions =>
            ApplyModifiers(MemberDefinitions, "private", "internal");

        public static IEnumerable<object[]> TypeUsages =>
            ApplyModifiers(TypeUsageStrings, string.Empty);

        public static string[] MemberDefinitions => new []
        {
            "/*MM*/C c;",
            "T(/*MM*/C c) {}",
            "/*MM*/CD c { get; }",
            "event /*MM*/CD c;",
            "delegate /*MM*/C WOW();"
        };

        public static string[] TypeDefinitions => new []
        {
            "delegate /*MM*/C WOW();",
            "class /*MM*/T: P<C> { } public class P<T> {}",
            "class /*MM*/T: C {}",
            "class T { public class /*MM*/T1: C { } }"
        };

        public static string[] AllowedMemberDefinitions => new []
        {
            "T([CA]int c) {}",
            "[CA] MOD int f;",
            "[CA] MOD int f { get; set; }",
            "[CA] MOD class CC { }"
        };

        public static string[] AllowedDefinitions => new []
        {
            "class T: I<C> { } interface I<T> {}"
        };

        public static string[] TypeUsageStrings => new []
        {
            "/*MM*/var c = new C();",
            "/*MM*/CD d = () => null;",
            "var t = typeof(/*MM*/CAAttribute);"
        };

        private static IEnumerable<object[]> ApplyModifiers(string[] code, params string[] mods)
        {
            foreach (var mod in mods)
            {
                foreach (var s in code)
                {
                    if (s.Contains("MOD"))
                    {
                        yield return new object[] { s.Replace("MOD", mod) };
                    }
                    else
                    {
                        yield return new object[] { mod + " " + s };
                    }
                }
            }
        }

        private TestSource GetSourceFromNamespaceDeclaration(string namespaceDefinition)
        {
            return TestSource.Read("using A.Internal.Namespace;" + InternalDefinitions + namespaceDefinition);
        }

        private Task<Diagnostic[]> GetDiagnosticWithProjectReference(string code)
        {
            var libraray = CreateProject(InternalDefinitions);

            var mainProject = CreateProject(code).AddProjectReference(new ProjectReference(libraray.Id));

            return GetDiagnosticsAsync(mainProject.Documents.ToArray(), new PubternalityAnalyzer(), new [] { "PUB0002" });
        }

        private Task<Diagnostic[]> GetDiagnostics(string code)
        {
            return GetDiagnosticsAsync(new[] { code }, new PubternalityAnalyzer(), new [] { "PUB0002" });
        }
    }
}
