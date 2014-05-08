// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcRazorHost : RazorEngineHost, IMvcRazorHost
    {
        private const string ViewNamespace = "ASP";

        private static readonly string[] _defaultNamespaces = new[] 
        { 
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "Microsoft.AspNet.Mvc",
            "Microsoft.AspNet.Mvc.Razor",
            "Microsoft.AspNet.Mvc.Rendering"
        };

        // CodeGenerationContext.DefaultBaseClass is set to MyBaseType<dynamic>. 
        // This field holds the type name without the generic decoration (MyBaseType)
        private readonly string _baseType;

        public MvcRazorHost(Type baseType)
            : this(baseType.FullName)
        {
        }

        public MvcRazorHost(string baseType)
            : base(new CSharpRazorCodeLanguage())
        {
            _baseType = baseType;
            DefaultBaseClass = baseType + "<dynamic>";
            GeneratedClassContext = new GeneratedClassContext(
                executeMethodName: "ExecuteAsync",
                writeMethodName: "Write",
                writeLiteralMethodName: "WriteLiteral",
                writeToMethodName: "WriteTo",
                writeLiteralToMethodName: "WriteLiteralTo",
                templateTypeName: "HelperResult",
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
            var className = ParserHelpers.SanitizeClassName(rootRelativePath);
            using (var reader = new StreamReader(inputStream))
            {
                var engine = new RazorTemplateEngine(this);
                return engine.GenerateCode(reader, className, ViewNamespace, rootRelativePath);
            }
        }

        public override ParserBase DecorateCodeParser(ParserBase incomingCodeParser)
        {
            return new MvcRazorCodeParser(_baseType);
        }

        public override CodeBuilder DecorateCodeBuilder(CodeBuilder incomingBuilder, CodeGeneratorContext context)
        {
            return new MvcCSharpCodeBuilder(context);
        }
    }
}
