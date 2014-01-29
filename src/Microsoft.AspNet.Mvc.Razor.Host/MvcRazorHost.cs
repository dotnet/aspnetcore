using System;
using System.IO;
using System.Text;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Generator;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcRazorHost : RazorEngineHost, IMvcRazorHost
    {
        private static readonly string[] _defaultNamespaces = new[] 
        { 
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "Microsoft.AspNet.Mvc",
            "Microsoft.AspNet.Mvc.Razor"
        };

        public MvcRazorHost(Type baseType)
            : this(baseType.FullName)
        {

        }

        public MvcRazorHost(string baseType)
            : base(new CSharpRazorCodeLanguage())
        {
            DefaultBaseClass = baseType;
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

            foreach (var ns in _defaultNamespaces)
            {
                NamespaceImports.Add(ns);
            }
        }

        public GeneratorResults GenerateCode(string rootRelativePath, Stream inputStream)
        {
            string className = Path.GetFileNameWithoutExtension(rootRelativePath);
            if (rootRelativePath.StartsWith("~/", StringComparison.Ordinal))
            {
                rootRelativePath = rootRelativePath.Substring(2);
            }
            string classNamespace = GenerateNamespace(rootRelativePath);
            
            using (var reader = new StreamReader(inputStream))
            {
                var engine = new RazorTemplateEngine(this);
                return engine.GenerateCode(reader, className, classNamespace, rootRelativePath);
            }
        }

        private static string GenerateNamespace(string rootRelativePath)
        {
            var namespaceBuilder = new StringBuilder(rootRelativePath.Length);
            rootRelativePath = Path.GetDirectoryName(rootRelativePath);
            for (int i = 0; i < rootRelativePath.Length; i++)
            {
                char c = rootRelativePath[i];
                if (c == Path.DirectorySeparatorChar)
                {
                    namespaceBuilder.Append('.');
                }
                else if (!Char.IsLetterOrDigit(c))
                {
                    namespaceBuilder.Append('_');
                }
                else
                {
                    namespaceBuilder.Append(c);
                }
            }
            return namespaceBuilder.ToString();
        }
    }
}
