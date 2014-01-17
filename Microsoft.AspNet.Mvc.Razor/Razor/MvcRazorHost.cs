using System.Web.Razor;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcRazorHost : RazorEngineHost
    {
        private static readonly string[] _namespaces = new[] 
        { 
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "Microsoft.AspNet.Mvc",
            "Microsoft.AspNet.Mvc.Razor"
        };

        public MvcRazorHost()
            : base(new CSharpRazorCodeLanguage())
        {
            DefaultBaseClass = typeof(RazorView).FullName;
            GeneratedClassContext = new GeneratedClassContext(
                executeMethodName: "Execute",
                writeMethodName: "Write",
                writeLiteralMethodName: "WriteLiteral",
                writeToMethodName: "WriteTo",
                writeLiteralToMethodName: "WriteLiteralTo",
                templateTypeName: "Template",
                defineSectionMethodName: "DefineSection")
            {
                ResolveUrlMethodName = "Href"
            };

            foreach (var ns in _namespaces)
            {
                NamespaceImports.Add(ns);
            }
        }

        public override RazorCodeGenerator DecorateCodeGenerator(RazorCodeGenerator incomingCodeGenerator)
        {
            return new MvcCSharpRazorCodeGenerator(incomingCodeGenerator.ClassName,
                                                       incomingCodeGenerator.RootNamespaceName,
                                                       incomingCodeGenerator.SourceFileName,
                                                       incomingCodeGenerator.Host);
        }

        public override ParserBase DecorateCodeParser(ParserBase incomingCodeParser)
        {
            return new MvcCSharpRazorCodeParser();
        }
    }
}
