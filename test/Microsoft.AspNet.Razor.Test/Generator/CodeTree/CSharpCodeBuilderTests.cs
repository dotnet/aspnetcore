using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.TestCommon;
using Moq;

namespace Microsoft.AspNet.Razor.Test.Generator.CodeTree
{
    public class CSharpCodeBuilderTests
    {
        [Fact]
        public void CodeTreeWithUsings()
        {
            var syntaxTreeNode = Mock.Of<SyntaxTreeNode>();
            var language = new CSharpRazorCodeLanguage();
            RazorEngineHost host = new RazorEngineHost(language);
            var context = CodeGeneratorContext.Create(host, "TestClass", "TestNamespace", "Foo.cs", shouldGenerateLinePragmas: false);
            context.CodeTreeBuilder.AddUsingChunk("FakeNamespace1", syntaxTreeNode, context);
            context.CodeTreeBuilder.AddUsingChunk("FakeNamespace2.SubNamespace", syntaxTreeNode, context);
            CodeBuilder codeBuilder = language.CreateBuilder(context);

            // Act
            CodeBuilderResult result = codeBuilder.Build();

            // Assert
            Assert.Equal(@"namespace TestNamespace
{
#line 1 """"
using FakeNamespace1;
#line default
#line hidden
#line 1 """"
using FakeNamespace2.SubNamespace;
#line default
#line hidden

    public class TestClass
    {
        #line hidden
        public TestClass()
        {
        }

        public override void Execute()
        {
        }
    }
}", result.Code.TrimEnd());
        }
    }
}
